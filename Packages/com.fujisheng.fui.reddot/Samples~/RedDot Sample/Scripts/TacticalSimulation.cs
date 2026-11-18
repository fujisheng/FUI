using System;
using System.Collections.Generic;
using System.Diagnostics;
using FUI.RedDot;

namespace FUI.Samples.RedDot
{
    /// <summary>独立推演世界。UI读取测量结果，永远不在测量区间创建快照或格式化文字。</summary>
    public sealed class TacticalSimulation : IDisposable
    {
        readonly List<IDisposable> registrations = new List<IDisposable>();
        readonly CountFact[] facts = new CountFact[1000];
        readonly FactRule[] rules = new FactRule[1000];
        readonly RedDotKey[] leaves = new RedDotKey[1000];
        readonly RedDotKey root = RedDotKey.Root("Simulation");
        int notifications;
        bool nextWave;
        bool recursive;
        bool disposed;
        /// <summary>仅由推演所有者驱动，不交给默认Runtime。</summary>
        public RedDotCenter Center { get; } = new RedDotCenter();
        /// <summary>终端可绑定的推演根节点。</summary>
        public RedDotKey Root => root;
        /// <summary>实际计算次数差值。</summary>
        public int Evaluations { get; private set; }
        /// <summary>本批实际变化通知数。</summary>
        public int Notifications { get; private set; }
        /// <summary>当前线程Flush区间的实际托管分配。</summary>
        public long AllocatedBytes { get; private set; }
        /// <summary>实际Flush耗时。</summary>
        public double Milliseconds { get; private set; }
        /// <summary>是否自动连续推进。</summary>
        public bool Continuous { get; set; }
        /// <summary>最近提交是否满足观察者可见的原子性。</summary>
        public bool AtomicRead { get; private set; } = true;
        /// <summary>最近一次递归防护是否确实被触发。</summary>
        public bool RecursiveRejected { get; private set; }

        /// <summary>构造10000个静态节点，协议数组仅用于规模化测试，不替代游戏Catalog。</summary>
        public TacticalSimulation()
        {
            var nodes = new RedDotNodeDefinition[10000];
            var edges = new RedDotEdgeDefinition[9999];
            nodes[0] = new RedDotNodeDefinition(root, "Simulation");
            for (var i = 1; i < nodes.Length; i++)
            {
                var key = RedDotKey.Child(root, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                nodes[i] = new RedDotNodeDefinition(key, key.Value);
                edges[i - 1] = new RedDotEdgeDefinition(root, key);
                if (i <= facts.Length) { leaves[i - 1] = key; }
            }
            Center.RegisterCatalog(new RedDotCatalogDefinition(nodes, edges));
            for (var i = 0; i < facts.Length; i++)
            {
                facts[i] = new CountFact(); rules[i] = new FactRule(facts[i]);
                registrations.Add(Center.RegisterRule(leaves[i], rules[i]));
                registrations.Add(Center.Subscribe(leaves[i], CountNotification));
            }
            registrations.Add(Center.Subscribe(root, OnRoot));
            // 预热所有工作缓冲区和观察者；正式测量不包含注册或JIT首次执行。
            QueueDistinct(); Center.Flush();
            QueueDistinct(); Center.Flush();
            Step();
        }
        /// <summary>对同一个规则重复失效1000次。</summary>
        public void QueueRepeated()
        {
            facts[0].Set(facts[0].Count == 1 ? 2 : 1);
            for (var i = 1; i < 1000; i++) { facts[0].Resample(); }
        }
        /// <summary>向1000个不同规则下达命令。</summary>
        public void QueueDistinct()
        {
            for (var i = 0; i < facts.Length; i++) { facts[i].Set(facts[i].Count == 1 ? 2 : 1); }
        }
        /// <summary>相同值重新计算，不应产生通知。</summary>
        public void QueueUnchanged() { foreach (var fact in facts) { fact.Resample(); } }
        /// <summary>根节点回调中触发第二波事实变化。</summary>
        public void QueueNextWave() { nextWave = true; QueueRepeated(); }
        /// <summary>下一次回调尝试递归刷新，由框架拒绝。</summary>
        public void QueueRecursive() { recursive = true; RecursiveRejected = false; QueueRepeated(); }
        /// <summary>两个有效大值演示饱和，而不是整数溢出。</summary>
        public void QueueSaturation() { facts[0].Set(int.MaxValue); facts[1].Set(int.MaxValue); }
        /// <summary>只测量Flush；计算次数统计和结果文字都在计量区间之外。</summary>
        public void Step()
        {
            var beforeEvaluations = TotalEvaluations(); notifications = 0;
            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            Center.Flush();
            var end = Stopwatch.GetTimestamp();
            AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
            Milliseconds = (end - start) * 1000.0 / Stopwatch.Frequency;
            Evaluations = TotalEvaluations() - beforeEvaluations; Notifications = notifications;
        }
        void CountNotification(RedDotState state) { notifications++; }
        void OnRoot(RedDotState state)
        {
            notifications++;
            var sum = 0;
            for (var i = 0; i < leaves.Length; i++)
            {
                var value = Center.Get(leaves[i]).Count;
                sum = int.MaxValue - sum < value ? int.MaxValue : sum + value;
            }
            AtomicRead = state.Count == sum;
            if (nextWave) { nextWave = false; facts[0].Set(facts[0].Count + 1); }
            if (recursive)
            {
                recursive = false;
                try { Center.Flush(); }
                catch (InvalidOperationException) { RecursiveRejected = true; }
            }
        }
        int TotalEvaluations() { var total = 0; foreach (var rule in rules) { total += rule.Evaluations; } return total; }
        /// <summary>释放推演，不影响默认账号。</summary>
        public void Dispose()
        {
            if (disposed) { return; }
            disposed = true;
            for (var i = registrations.Count - 1; i >= 0; i--) { registrations[i].Dispose(); }
            Center.Dispose();
        }
    }
}
