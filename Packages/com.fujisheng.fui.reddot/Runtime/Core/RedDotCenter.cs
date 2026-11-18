using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FUI.RedDot
{
    /// <summary>
    /// 统一管理节点图、独立贡献、帧末合批计算和精确订阅。
    /// 实例绑定创建线程；除订阅回调外，所有公开操作都必须在该线程调用。
    /// </summary>
    public sealed class RedDotCenter : IRedDotReader, IRedDotDiagnostics, IDisposable
    {
        sealed class Node
        {
            public readonly RedDotKey Key;
            public readonly string DebugName;
            public readonly IRedDotAggregator Aggregator;
            public readonly bool IsDynamic;
            public readonly List<Node> Parents = new List<Node>();
            public readonly List<Node> Children = new List<Node>();
            public readonly List<RuleEntry> Rules = new List<RuleEntry>();
            public readonly Dictionary<long, RedDotValue> SourceValues = new Dictionary<long, RedDotValue>();
            public readonly Dictionary<long, Action<RedDotState>> Subscribers = new Dictionary<long, Action<RedDotState>>();
            public RedDotAggregationInput[] InputBuffer = Array.Empty<RedDotAggregationInput>();
            public Action<RedDotState>[] CallbackBuffer = Array.Empty<Action<RedDotState>>();
            public RedDotState State;
            public int PendingCount;
            public bool HasPendingState;
            public double LastCalculationMilliseconds;
            public RedDotDiagnostic? LastDiagnostic;
            public int TopologicalIndex;

            public Node(RedDotNodeDefinition definition, bool isDynamic)
            {
                Key = definition.Key;
                DebugName = definition.DebugName;
                Aggregator = definition.Aggregator;
                IsDynamic = isDynamic;
                State = new RedDotState(Key, 0);
            }
        }

        sealed class RuleEntry : IRedDotInvalidator
        {
            readonly RedDotCenter center;
            public readonly long Id;
            public readonly Node Node;
            public readonly IRedDotRule Rule;
            public RedDotValue Value;
            public bool Disposed;

            public RuleEntry(RedDotCenter center, long id, Node node, IRedDotRule rule)
            {
                this.center = center;
                Id = id;
                Node = node;
                Rule = rule;
            }

            public void Invalidate()
            {
                if (!Disposed)
                {
                    center.Invalidate(this);
                }
            }
        }

        sealed class SourceEntry : IRedDotPublisher
        {
            readonly RedDotCenter center;
            public readonly long Id;
            public readonly IRedDotSource Source;
            public readonly HashSet<RedDotKey> PublishedKeys = new HashSet<RedDotKey>();
            public bool Disposed;

            public SourceEntry(RedDotCenter center, long id, IRedDotSource source)
            {
                this.center = center;
                Id = id;
                Source = source;
            }

            public void Publish(RedDotKey key, RedDotValue value)
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(IRedDotPublisher));
                }
                center.Publish(this, key, value);
            }

            public void Remove(RedDotKey key)
            {
                Publish(key, default);
            }
        }

        readonly struct SourceUpdateKey : IEquatable<SourceUpdateKey>
        {
            public readonly long SourceId;
            public readonly RedDotKey Key;

            public SourceUpdateKey(long sourceId, RedDotKey key)
            {
                SourceId = sourceId;
                Key = key;
            }

            public bool Equals(SourceUpdateKey other) => SourceId == other.SourceId && Key == other.Key;
            public override bool Equals(object obj) => obj is SourceUpdateKey other && Equals(other);
            public override int GetHashCode() => (SourceId.GetHashCode() * 397) ^ Key.GetHashCode();
        }

        sealed class Registration : IDisposable
        {
            readonly RedDotCenter center;
            Action dispose;

            public Registration(RedDotCenter center, Action dispose)
            {
                this.center = center;
                this.dispose = dispose;
            }

            public void Dispose()
            {
                // 注销也属于 Center 的公开操作。先校验线程，再消费回调，确保误用失败后仍能在主线程重试。
                center.EnsureThread();
                var action = Interlocked.Exchange(ref dispose, null);
                action?.Invoke();
            }
        }

        sealed class ReverseTopologicalComparer : IComparer<Node>
        {
            public static readonly ReverseTopologicalComparer Instance = new ReverseTopologicalComparer();
            public int Compare(Node left, Node right) => right.TopologicalIndex.CompareTo(left.TopologicalIndex);
        }

        readonly int ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        readonly Dictionary<RedDotKey, Node> nodes = new Dictionary<RedDotKey, Node>();
        readonly List<Node> topologicalNodes = new List<Node>();
        readonly List<RedDotDynamicTemplate> dynamicTemplates = new List<RedDotDynamicTemplate>();
        readonly Dictionary<long, RuleEntry> rules = new Dictionary<long, RuleEntry>();
        readonly Dictionary<long, SourceEntry> sources = new Dictionary<long, SourceEntry>();
        readonly HashSet<RuleEntry> dirtyRules = new HashSet<RuleEntry>();
        readonly HashSet<Node> dirtyNodes = new HashSet<Node>();
        readonly Dictionary<SourceUpdateKey, RedDotValue> pendingSourceUpdates = new Dictionary<SourceUpdateKey, RedDotValue>();
        readonly List<RuleEntry> ruleBatch = new List<RuleEntry>();
        readonly List<KeyValuePair<SourceUpdateKey, RedDotValue>> sourceBatch = new List<KeyValuePair<SourceUpdateKey, RedDotValue>>();
        readonly List<Node> nodeBatch = new List<Node>();
        readonly List<Node> changedNodes = new List<Node>();
        readonly HashSet<Node> pruneCandidates = new HashSet<Node>();
        readonly List<Node> pruneBatch = new List<Node>();
        readonly List<RedDotDiagnostic> diagnosticBatch = new List<RedDotDiagnostic>();
        readonly Stack<Node> dirtyTraversal = new Stack<Node>();
        long nextIdentity;
        ulong version;
        ulong diagnosticsVersion;
        ulong activeBatchVersion;
        Action<RedDotDiagnostic> diagnosticReported;
        bool flushing;
        bool disposing;
        bool disposed;

        /// <summary>当前已实例化的静态与动态节点总数。</summary>
        public int NodeCount
        {
            get
            {
                EnsureUsable();
                return nodes.Count;
            }
        }

        /// <summary>是否存在等待下一次 Tick 或 Flush 处理的变化。</summary>
        public bool HasPendingChanges
        {
            get
            {
                EnsureUsable();
                return dirtyRules.Count > 0 || dirtyNodes.Count > 0 || pendingSourceUpdates.Count > 0;
            }
        }

        /// <summary>诊断可观察数据发生变化时递增，供只读工具跳过重复快照。</summary>
        public ulong DiagnosticsVersion
        {
            get
            {
                EnsureUsable();
                return diagnosticsVersion;
            }
        }

        /// <summary>最近一次动态清理实际检查的候选数，供性能验收使用。</summary>
        internal int LastPruneInspectionCount { get; private set; }

        /// <summary>
        /// 上报已隔离的 Rule、Source、Aggregator 或订阅者异常；处理器异常同样会被框架隔离。
        /// </summary>
        public event Action<RedDotDiagnostic> DiagnosticReported
        {
            add
            {
                EnsureUsable();
                diagnosticReported += value;
            }
            remove
            {
                EnsureUsable();
                diagnosticReported -= value;
            }
        }

        /// <summary>
        /// 注册一份生成的图定义。整个定义会先完成重复 RedDotKey、父边与环校验，再原子加入中心。
        /// </summary>
        public void RegisterCatalog(RedDotCatalogDefinition catalog)
        {
            EnsureUsable();
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            ValidateCatalog(catalog);
            for (var index = 0; index < catalog.Nodes.Length; index++)
            {
                var definition = catalog.Nodes[index];
                nodes.Add(definition.Key, new Node(definition, false));
            }
            for (var index = 0; index < catalog.Edges.Length; index++)
            {
                AddEdge(catalog.Edges[index].Parent, catalog.Edges[index].Child);
            }
            for (var index = 0; index < catalog.DynamicTemplates.Length; index++)
            {
                dynamicTemplates.Add(catalog.DynamicTemplates[index]);
            }
            RebuildTopologicalOrder();
            TouchDiagnostics();
        }

        /// <summary>
        /// 为节点注册一项独立拉取贡献。返回句柄只移除该 Rule 自己的贡献。
        /// </summary>
        public IDisposable RegisterRule(RedDotKey key, IRedDotRule rule)
        {
            EnsureUsable();
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var node = EnsureNode(key);
            var entry = new RuleEntry(this, NextIdentity(), node, rule);
            rules.Add(entry.Id, entry);
            node.Rules.Add(entry);
            try
            {
                rule.Start(entry);
                dirtyRules.Add(entry);
            }
            catch
            {
                dirtyRules.Remove(entry);
                rules.Remove(entry.Id);
                node.Rules.Remove(entry);
                entry.Disposed = true;
                try
                {
                    rule.Stop();
                }
                catch (Exception cleanupException)
                {
                    Report(new RedDotDiagnostic(RedDotDiagnosticKind.RuleFailure, key, rule.GetType().Name, cleanupException));
                }
                throw;
            }
            TouchDiagnostics();
            return new Registration(this, () => RemoveRule(entry));
        }

        /// <summary>
        /// 注册一个推送来源并分配独立身份。注销时会移除该来源发布的全部贡献。
        /// </summary>
        public IDisposable RegisterSource(IRedDotSource source)
        {
            EnsureUsable();
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entry = new SourceEntry(this, NextIdentity(), source);
            sources.Add(entry.Id, entry);
            try
            {
                source.Start(entry);
            }
            catch
            {
                RemoveSource(entry);
                throw;
            }
            TouchDiagnostics();
            return new Registration(this, () => RemoveSource(entry));
        }

        /// <summary>
        /// 获取最近一次成功提交的状态；尚未实例化但能匹配动态模板的 RedDotKey 返回零状态。
        /// </summary>
        public RedDotState Get(RedDotKey key)
        {
            EnsureUsable();
            if (nodes.TryGetValue(key, out var node))
            {
                return node.State;
            }
            if (TryFindDynamicTemplate(key, out _))
            {
                return new RedDotState(key, 0);
            }
            throw new KeyNotFoundException($"Red dot key '{key}' is not registered by a catalog.");
        }

        /// <summary>
        /// 精确订阅单个 RedDotKey。订阅时立即回放当前状态，回调异常不会阻断其他订阅者。
        /// </summary>
        public IDisposable Subscribe(RedDotKey key, Action<RedDotState> callback)
        {
            EnsureUsable();
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var node = EnsureNode(key);
            var id = NextIdentity();
            node.Subscribers.Add(id, callback);
            try
            {
                callback(node.State);
            }
            catch (Exception exception)
            {
                Report(new RedDotDiagnostic(RedDotDiagnosticKind.SubscriberFailure, key, callback.Method.Name, exception));
            }
            TouchDiagnostics();
            return new Registration(this, () => RemoveSubscriber(node, id));
        }

        /// <summary>宿主每帧调用一次；当前实现等价于 <see cref="Flush"/>。</summary>
        public void Tick()
        {
            Flush();
        }

        /// <summary>
        /// 提交一批变化：计算脏 Rule、合并 Source、按拓扑重算、原子替换状态并逐 RedDotKey 通知。
        /// Flush 中产生的新失效保留到下一批，递归 Flush 会抛出异常。
        /// </summary>
        public void Flush()
        {
            EnsureUsable();
            if (flushing)
            {
                throw new InvalidOperationException("RedDotCenter cannot flush recursively.");
            }
            if (!HasPendingChanges)
            {
                return;
            }

            flushing = true;
            try
            {
                SnapshotRuleBatch();
                SnapshotSourceBatch();
                EvaluateRules();
                ApplySourceUpdates();
                SnapshotNodeBatch();
                CalculateNodes();
                CommitNodes();
                PublishDiagnosticBatch();
                NotifyChangedNodes();
                PruneDynamicNodes();
            }
            finally
            {
                ruleBatch.Clear();
                sourceBatch.Clear();
                nodeBatch.Clear();
                changedNodes.Clear();
                diagnosticBatch.Clear();
                activeBatchVersion = 0;
                flushing = false;
                TouchDiagnostics();
            }
        }

        /// <summary>复制当前节点图和运行态诊断数据，不暴露内部可变集合。</summary>
        public RedDotNodeSnapshot[] GetDiagnosticsSnapshot()
        {
            EnsureUsable();
            var dirtySnapshot = new HashSet<Node>(dirtyNodes);
            foreach (var rule in dirtyRules) dirtySnapshot.Add(rule.Node);
            foreach (var update in pendingSourceUpdates)
            {
                if (nodes.TryGetValue(update.Key.Key, out var dirtyNode)) dirtySnapshot.Add(dirtyNode);
            }
            var result = new RedDotNodeSnapshot[nodes.Count];
            var resultIndex = 0;
            foreach (var pair in nodes)
            {
                var node = pair.Value;
                var parents = new RedDotKey[node.Parents.Count];
                for (var index = 0; index < parents.Length; index++) parents[index] = node.Parents[index].Key;
                var children = new RedDotKey[node.Children.Count];
                for (var index = 0; index < children.Length; index++) children[index] = node.Children[index].Key;

                var contributions = new RedDotContributionSnapshot[node.Rules.Count + node.SourceValues.Count];
                var contributionIndex = 0;
                for (var index = 0; index < node.Rules.Count; index++)
                {
                    var entry = node.Rules[index];
                    contributions[contributionIndex++] = new RedDotContributionSnapshot(entry.Rule.GetType().Name, RedDotInputKind.Direct, entry.Value.Count);
                }
                foreach (var sourceValue in node.SourceValues)
                {
                    var owner = sources.TryGetValue(sourceValue.Key, out var source) ? source.Source.GetType().Name : sourceValue.Key.ToString();
                    contributions[contributionIndex++] = new RedDotContributionSnapshot(owner, RedDotInputKind.Direct, sourceValue.Value.Count);
                }

                result[resultIndex++] = new RedDotNodeSnapshot(
                    node.Key,
                    node.DebugName,
                    node.State,
                    node.Aggregator.GetType().Name,
                    parents,
                    children,
                    contributions,
                    node.Rules.Count,
                    node.Subscribers.Count,
                    node.IsDynamic,
                    dirtySnapshot.Contains(node),
                    node.LastCalculationMilliseconds,
                    node.LastDiagnostic);
            }
            Array.Sort(result, (left, right) => left.Key.CompareTo(right.Key));
            return result;
        }

        /// <summary>
        /// 幂等停止全部 Rule 与 Source，移除贡献和订阅，并释放中心持有的图状态。
        /// </summary>
        public void Dispose()
        {
            if (disposed || disposing)
            {
                return;
            }
            EnsureThread();
            disposing = true;

            try
            {
                var ruleEntries = new RuleEntry[rules.Count];
                rules.Values.CopyTo(ruleEntries, 0);
                for (var index = 0; index < ruleEntries.Length; index++) RemoveRule(ruleEntries[index]);
                var sourceEntries = new SourceEntry[sources.Count];
                sources.Values.CopyTo(sourceEntries, 0);
                for (var index = 0; index < sourceEntries.Length; index++) RemoveSource(sourceEntries[index]);

                nodes.Clear();
                dynamicTemplates.Clear();
                topologicalNodes.Clear();
                dirtyRules.Clear();
                dirtyNodes.Clear();
                pendingSourceUpdates.Clear();
                pruneCandidates.Clear();
                pruneBatch.Clear();
                diagnosticBatch.Clear();
                diagnosticReported = null;
                disposed = true;
            }
            finally
            {
                disposing = false;
            }
        }

        void ValidateCatalog(RedDotCatalogDefinition catalog)
        {
            var newKeys = new HashSet<RedDotKey>();
            for (var index = 0; index < catalog.Nodes.Length; index++)
            {
                var definition = catalog.Nodes[index];
                if (!definition.Key.IsValid)
                {
                    throw new ArgumentException("A catalog contains an empty red dot key.", nameof(catalog));
                }
                if (definition.Aggregator == null)
                {
                    throw new ArgumentException($"Red dot node '{definition.Key}' has no aggregator.", nameof(catalog));
                }
                if (nodes.ContainsKey(definition.Key) || !newKeys.Add(definition.Key))
                {
                    throw new InvalidOperationException($"Red dot key '{definition.Key}' is already registered.");
                }
            }

            for (var index = 0; index < catalog.Edges.Length; index++)
            {
                var edge = catalog.Edges[index];
                if (!ContainsKnownKey(edge.Parent, newKeys) || !ContainsKnownKey(edge.Child, newKeys))
                {
                    throw new InvalidOperationException($"Red dot edge '{edge.Parent}' -> '{edge.Child}' references a missing node.");
                }
            }
            ValidateCombinedGraphIsAcyclic(newKeys, catalog.Edges);

            var prefixes = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < dynamicTemplates.Count; index++)
            {
                prefixes.Add(RedDotKey.DynamicPrefix(dynamicTemplates[index].Parent, dynamicTemplates[index].Segment));
            }
            for (var index = 0; index < catalog.DynamicTemplates.Length; index++)
            {
                var template = catalog.DynamicTemplates[index];
                for (var parentIndex = 0; parentIndex < template.Parents.Length; parentIndex++)
                {
                    if (!ContainsKnownKey(template.Parents[parentIndex], newKeys))
                    {
                        throw new InvalidOperationException($"Dynamic red dot template '{template.DebugName}' references missing parent '{template.Parents[parentIndex]}'.");
                    }
                }
                var prefix = RedDotKey.DynamicPrefix(template.Parent, template.Segment);
                if (!prefixes.Add(prefix))
                {
                    throw new InvalidOperationException($"Dynamic red dot template prefix '{prefix}' is already registered.");
                }
            }
        }

        bool ContainsKnownKey(RedDotKey key, HashSet<RedDotKey> newKeys) => nodes.ContainsKey(key) || newKeys.Contains(key);

        void ValidateCombinedGraphIsAcyclic(HashSet<RedDotKey> newKeys, RedDotEdgeDefinition[] pendingEdges)
        {
            // 在修改现有图之前用 Kahn 算法验证“已有节点 + 本次 Catalog”的整体图。
            var adjacency = new Dictionary<RedDotKey, HashSet<RedDotKey>>(nodes.Count + newKeys.Count);
            var indegrees = new Dictionary<RedDotKey, int>(nodes.Count + newKeys.Count);
            foreach (var pair in nodes)
            {
                adjacency.Add(pair.Key, new HashSet<RedDotKey>());
                indegrees.Add(pair.Key, 0);
            }
            foreach (var key in newKeys)
            {
                adjacency.Add(key, new HashSet<RedDotKey>());
                indegrees.Add(key, 0);
            }
            foreach (var pair in nodes)
            {
                var parent = pair.Value;
                for (var index = 0; index < parent.Children.Count; index++)
                {
                    var child = parent.Children[index].Key;
                    if (adjacency[parent.Key].Add(child)) indegrees[child] = indegrees[child] + 1;
                }
            }
            for (var index = 0; index < pendingEdges.Length; index++)
            {
                var edge = pendingEdges[index];
                if (adjacency[edge.Parent].Add(edge.Child))
                {
                    indegrees[edge.Child] = indegrees[edge.Child] + 1;
                }
            }

            var ready = new Queue<RedDotKey>();
            foreach (var pair in indegrees)
            {
                if (pair.Value == 0) ready.Enqueue(pair.Key);
            }
            var visited = 0;
            while (ready.Count > 0)
            {
                var current = ready.Dequeue();
                visited++;
                foreach (var child in adjacency[current])
                {
                    var degree = indegrees[child] - 1;
                    indegrees[child] = degree;
                    if (degree == 0) ready.Enqueue(child);
                }
            }
            if (visited != indegrees.Count)
            {
                throw new InvalidOperationException("The red dot catalog creates a cycle.");
            }
        }

        Node EnsureNode(RedDotKey key)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("A red dot key cannot be empty.", nameof(key));
            }
            if (nodes.TryGetValue(key, out var node))
            {
                return node;
            }

            if (TryFindDynamicTemplate(key, out var template))
            {
                var prefix = RedDotKey.DynamicPrefix(template.Parent, template.Segment);
                node = new Node(new RedDotNodeDefinition(key, template.DebugName + " [" + key.Value.Substring(prefix.Length) + "]", template.Aggregator), true);
                nodes.Add(key, node);
                pruneCandidates.Add(node);
                for (var parentIndex = 0; parentIndex < template.Parents.Length; parentIndex++)
                {
                    AddEdge(template.Parents[parentIndex], key);
                }
                RebuildTopologicalOrder();
                TouchDiagnostics();
                return node;
            }
            throw new KeyNotFoundException($"Red dot key '{key}' is not registered by a catalog.");
        }

        bool TryFindDynamicTemplate(RedDotKey key, out RedDotDynamicTemplate template)
        {
            for (var index = 0; index < dynamicTemplates.Count; index++)
            {
                var candidate = dynamicTemplates[index];
                var prefix = RedDotKey.DynamicPrefix(candidate.Parent, candidate.Segment);
                if (key.Value.StartsWith(prefix, StringComparison.Ordinal) && key.Value.Length > prefix.Length)
                {
                    template = candidate;
                    return true;
                }
            }
            template = default;
            return false;
        }

        void AddEdge(RedDotKey parentKey, RedDotKey childKey)
        {
            var parent = nodes[parentKey];
            var child = nodes[childKey];
            if (!parent.Children.Contains(child)) parent.Children.Add(child);
            if (!child.Parents.Contains(parent)) child.Parents.Add(parent);
        }

        void RebuildTopologicalOrder()
        {
            topologicalNodes.Clear();
            var indegrees = new Dictionary<Node, int>(nodes.Count);
            var ready = new Queue<Node>();
            foreach (var pair in nodes)
            {
                var node = pair.Value;
                indegrees.Add(node, node.Parents.Count);
                if (node.Parents.Count == 0) ready.Enqueue(node);
            }
            while (ready.Count > 0)
            {
                var node = ready.Dequeue();
                topologicalNodes.Add(node);
                for (var index = 0; index < node.Children.Count; index++)
                {
                    var child = node.Children[index];
                    var degree = indegrees[child] - 1;
                    indegrees[child] = degree;
                    if (degree == 0) ready.Enqueue(child);
                }
            }
            if (topologicalNodes.Count != nodes.Count)
            {
                throw new InvalidOperationException("The red dot graph contains a cycle.");
            }
            for (var index = 0; index < topologicalNodes.Count; index++)
            {
                topologicalNodes[index].TopologicalIndex = index;
            }
        }

        void Invalidate(RuleEntry entry)
        {
            EnsureUsable();
            if (dirtyRules.Add(entry)) TouchDiagnostics();
        }

        void Publish(SourceEntry source, RedDotKey key, RedDotValue value)
        {
            EnsureUsable();
            EnsureNode(key);
            if (pendingSourceUpdates.TryGetValue(new SourceUpdateKey(source.Id, key), out var current) && current == value) return;
            pendingSourceUpdates[new SourceUpdateKey(source.Id, key)] = value;
            TouchDiagnostics();
        }

        void SnapshotRuleBatch()
        {
            // 先关闭当前批次；Evaluate 或回调中的再次失效会进入下一批。
            ruleBatch.Clear();
            foreach (var rule in dirtyRules) ruleBatch.Add(rule);
            dirtyRules.Clear();
        }

        void SnapshotSourceBatch()
        {
            // 同一 Source + RedDotKey 在一批中只保留最后一次发布值。
            sourceBatch.Clear();
            foreach (var update in pendingSourceUpdates) sourceBatch.Add(update);
            pendingSourceUpdates.Clear();
        }

        void EvaluateRules()
        {
            for (var index = 0; index < ruleBatch.Count; index++)
            {
                var entry = ruleBatch[index];
                if (entry.Disposed) continue;
                try
                {
                    var value = entry.Rule.Evaluate();
                    if (entry.Value != value)
                    {
                        entry.Value = value;
                        MarkDirty(entry.Node);
                    }
                }
                catch (Exception exception)
                {
                    Report(new RedDotDiagnostic(RedDotDiagnosticKind.RuleFailure, entry.Node.Key, entry.Rule.GetType().Name, exception));
                }
            }
        }

        void ApplySourceUpdates()
        {
            for (var index = 0; index < sourceBatch.Count; index++)
            {
                var update = sourceBatch[index];
                if (!sources.TryGetValue(update.Key.SourceId, out var source) || source.Disposed) continue;
                var node = EnsureNode(update.Key.Key);
                var changed = false;
                if (update.Value.Count == 0)
                {
                    changed = node.SourceValues.Remove(source.Id);
                    source.PublishedKeys.Remove(node.Key);
                }
                else if (!node.SourceValues.TryGetValue(source.Id, out var current) || current != update.Value)
                {
                    node.SourceValues[source.Id] = update.Value;
                    source.PublishedKeys.Add(node.Key);
                    changed = true;
                }
                if (changed) MarkDirty(node);
            }
        }

        void SnapshotNodeBatch()
        {
            nodeBatch.Clear();
            foreach (var node in dirtyNodes) nodeBatch.Add(node);
            dirtyNodes.Clear();
            // 子节点先于父节点计算，使父节点可以读取子节点本批次的 PendingCount。
            nodeBatch.Sort(ReverseTopologicalComparer.Instance);
        }

        void CalculateNodes()
        {
            for (var index = 0; index < nodeBatch.Count; index++)
            {
                var node = nodeBatch[index];
                var started = Stopwatch.GetTimestamp();
                try
                {
                    var inputCount = node.Rules.Count + node.SourceValues.Count + node.Children.Count;
                    EnsureInputCapacity(node, inputCount);
                    var inputIndex = 0;
                    for (var ruleIndex = 0; ruleIndex < node.Rules.Count; ruleIndex++)
                    {
                        var rule = node.Rules[ruleIndex];
                        node.InputBuffer[inputIndex++] = new RedDotAggregationInput(node.Key, rule.Value, RedDotInputKind.Direct);
                    }
                    foreach (var sourceValue in node.SourceValues)
                    {
                        node.InputBuffer[inputIndex++] = new RedDotAggregationInput(node.Key, sourceValue.Value, RedDotInputKind.Direct);
                    }
                    for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
                    {
                        var child = node.Children[childIndex];
                        var childCount = child.HasPendingState ? child.PendingCount : child.State.Count;
                        node.InputBuffer[inputIndex++] = new RedDotAggregationInput(child.Key, new RedDotValue(childCount), RedDotInputKind.Child);
                    }
                    node.PendingCount = node.Aggregator.Aggregate(new ReadOnlySpan<RedDotAggregationInput>(node.InputBuffer, 0, inputCount)).Count;
                    node.HasPendingState = true;
                }
                catch (Exception exception)
                {
                    // 聚合失败只回滚当前节点，其他节点仍可完成本批计算与提交。
                    node.PendingCount = node.State.Count;
                    node.HasPendingState = true;
                    Report(new RedDotDiagnostic(RedDotDiagnosticKind.AggregatorFailure, node.Key, node.Aggregator.GetType().Name, exception));
                }
                finally
                {
                    node.LastCalculationMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
                }
            }
        }

        static void EnsureInputCapacity(Node node, int count)
        {
            if (node.InputBuffer.Length >= count) return;
            var capacity = node.InputBuffer.Length == 0 ? 4 : node.InputBuffer.Length;
            while (capacity < count) capacity *= 2;
            node.InputBuffer = new RedDotAggregationInput[capacity];
        }

        void CommitNodes()
        {
            // 计算阶段只写 Pending；全部计算结束后才替换可见 RedDotState，避免订阅者看到半批状态。
            var hasChanges = false;
            for (var index = nodeBatch.Count - 1; index >= 0; index--)
            {
                var node = nodeBatch[index];
                if (!node.HasPendingState) continue;
                node.HasPendingState = false;
                if (node.State.Count == node.PendingCount) continue;
                hasChanges = true;
                changedNodes.Add(node);
            }
            if (!hasChanges)
            {
                activeBatchVersion = version;
                return;
            }

            var batchVersion = ++version;
            activeBatchVersion = batchVersion;
            for (var index = 0; index < changedNodes.Count; index++)
            {
                var node = changedNodes[index];
                node.State = new RedDotState(node.Key, node.PendingCount, batchVersion);
                if (node.IsDynamic && node.PendingCount == 0) pruneCandidates.Add(node);
            }
        }

        void NotifyChangedNodes()
        {
            for (var nodeIndex = 0; nodeIndex < changedNodes.Count; nodeIndex++)
            {
                var node = changedNodes[nodeIndex];
                if (node.Subscribers.Count == 0) continue;
                // 复制回调快照，允许回调安全地取消自身或其他订阅。
                EnsureCallbackCapacity(node, node.Subscribers.Count);
                node.Subscribers.Values.CopyTo(node.CallbackBuffer, 0);
                var callbackCount = node.Subscribers.Count;
                for (var callbackIndex = 0; callbackIndex < callbackCount; callbackIndex++)
                {
                    try
                    {
                        node.CallbackBuffer[callbackIndex](node.State);
                    }
                    catch (Exception exception)
                    {
                        Report(new RedDotDiagnostic(RedDotDiagnosticKind.SubscriberFailure, node.Key, node.CallbackBuffer[callbackIndex].Method.Name, exception));
                    }
                    node.CallbackBuffer[callbackIndex] = null;
                }
            }
        }

        static void EnsureCallbackCapacity(Node node, int count)
        {
            if (node.CallbackBuffer.Length >= count) return;
            var capacity = node.CallbackBuffer.Length == 0 ? 4 : node.CallbackBuffer.Length;
            while (capacity < count) capacity *= 2;
            node.CallbackBuffer = new Action<RedDotState>[capacity];
        }

        void MarkDirty(Node node)
        {
            // 聚合方向从子到父传播，只标记受影响祖先，不遍历无关子图。
            dirtyTraversal.Push(node);
            while (dirtyTraversal.Count > 0)
            {
                var current = dirtyTraversal.Pop();
                if (!dirtyNodes.Add(current)) continue;
                for (var index = 0; index < current.Parents.Count; index++) dirtyTraversal.Push(current.Parents[index]);
            }
        }

        void PruneDynamicNodes()
        {
            // 只检查发生过“可能变为可回收”状态变化的动态节点，避免每批扫描完整图。
            LastPruneInspectionCount = 0;
            pruneBatch.Clear();
            foreach (var candidate in pruneCandidates) pruneBatch.Add(candidate);
            pruneCandidates.Clear();
            var removedAny = false;
            for (var index = 0; index < pruneBatch.Count; index++)
            {
                var node = pruneBatch[index];
                LastPruneInspectionCount++;
                if (!nodes.ContainsKey(node.Key) || !CanPrune(node)) continue;
                for (var parentIndex = 0; parentIndex < node.Parents.Count; parentIndex++)
                {
                    var parent = node.Parents[parentIndex];
                    parent.Children.Remove(node);
                    if (parent.IsDynamic && pruneCandidates.Add(parent)) pruneBatch.Add(parent);
                }
                nodes.Remove(node.Key);
                removedAny = true;
            }
            pruneCandidates.Clear();
            pruneBatch.Clear();
            if (removedAny) RebuildTopologicalOrder();
        }

        static bool CanPrune(Node node) => node.IsDynamic && node.State.Count == 0 && node.Rules.Count == 0 &&
                                           node.SourceValues.Count == 0 && node.Subscribers.Count == 0 && node.Children.Count == 0;

        void RemoveRule(RuleEntry entry)
        {
            EnsureThread();
            if (entry == null || entry.Disposed) return;
            entry.Disposed = true;
            dirtyRules.Remove(entry);
            rules.Remove(entry.Id);
            entry.Node.Rules.Remove(entry);
            try
            {
                entry.Rule.Stop();
            }
            catch (Exception exception)
            {
                Report(new RedDotDiagnostic(RedDotDiagnosticKind.RuleFailure, entry.Node.Key, entry.Rule.GetType().Name, exception));
            }
            MarkDirty(entry.Node);
            if (entry.Node.IsDynamic) pruneCandidates.Add(entry.Node);
            TouchDiagnostics();
        }

        void RemoveSource(SourceEntry entry)
        {
            EnsureThread();
            if (entry == null || entry.Disposed) return;
            // 先使 Publisher 失效，阻止 Source.Stop 中的重入发布重新污染待提交区。
            entry.Disposed = true;
            try
            {
                entry.Source.Stop();
            }
            catch (Exception exception)
            {
                Report(new RedDotDiagnostic(RedDotDiagnosticKind.SourceFailure, default, entry.Source.GetType().Name, exception));
            }
            sources.Remove(entry.Id);

            var pendingKeys = new List<SourceUpdateKey>();
            foreach (var update in pendingSourceUpdates)
            {
                if (update.Key.SourceId == entry.Id) pendingKeys.Add(update.Key);
            }
            for (var index = 0; index < pendingKeys.Count; index++) pendingSourceUpdates.Remove(pendingKeys[index]);
            for (var index = 0; index < pendingKeys.Count; index++)
            {
                if (nodes.TryGetValue(pendingKeys[index].Key, out var pendingNode) && pendingNode.IsDynamic)
                {
                    MarkDirty(pendingNode);
                    pruneCandidates.Add(pendingNode);
                }
            }
            foreach (var key in entry.PublishedKeys)
            {
                if (nodes.TryGetValue(key, out var node) && node.SourceValues.Remove(entry.Id))
                {
                    MarkDirty(node);
                    if (node.IsDynamic) pruneCandidates.Add(node);
                }
            }
            entry.PublishedKeys.Clear();
            TouchDiagnostics();
        }

        void RemoveSubscriber(Node node, long id)
        {
            EnsureThread();
            if (node.Subscribers.Remove(id) && node.IsDynamic)
            {
                MarkDirty(node);
                pruneCandidates.Add(node);
            }
            TouchDiagnostics();
        }

        long NextIdentity() => ++nextIdentity;

        void TouchDiagnostics()
        {
            diagnosticsVersion++;
        }

        void EnsureUsable()
        {
            EnsureThread();
            if (disposed || disposing)
            {
                throw new ObjectDisposedException(nameof(RedDotCenter));
            }
        }

        void EnsureThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException("RedDotCenter may only be used from its creating thread.");
            }
        }

        void Report(RedDotDiagnostic diagnostic)
        {
            // 计算阶段尚不知道本批是否真的产生状态变化，先暂存到原子提交完成后再固化版本。
            if (flushing && activeBatchVersion == 0)
            {
                diagnosticBatch.Add(diagnostic);
                return;
            }
            PublishDiagnostic(diagnostic, flushing ? activeBatchVersion : version);
        }

        void PublishDiagnosticBatch()
        {
            for (var index = 0; index < diagnosticBatch.Count; index++)
            {
                PublishDiagnostic(diagnosticBatch[index], activeBatchVersion);
            }
            diagnosticBatch.Clear();
        }

        void PublishDiagnostic(RedDotDiagnostic diagnostic, ulong batchVersion)
        {
            diagnostic = new RedDotDiagnostic(diagnostic.Kind, diagnostic.Key, diagnostic.Owner, diagnostic.Exception, batchVersion);
            if (diagnostic.Key.IsValid && nodes.TryGetValue(diagnostic.Key, out var node)) node.LastDiagnostic = diagnostic;
            TouchDiagnostics();
            try
            {
                diagnosticReported?.Invoke(diagnostic);
            }
            catch
            {
                // 诊断观察者不能破坏红点提交。
            }
        }
    }
}
