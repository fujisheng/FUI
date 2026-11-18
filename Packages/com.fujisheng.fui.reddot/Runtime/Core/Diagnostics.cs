using System;

namespace FUI.RedDot
{
    /// <summary>运行时隔离故障所处的阶段。</summary>
    public enum RedDotDiagnosticKind
    {
        /// <summary>Rule 计算或停止失败。</summary>
        RuleFailure,
        /// <summary>节点聚合失败。</summary>
        AggregatorFailure,
        /// <summary>订阅回调失败。</summary>
        SubscriberFailure,
        /// <summary>Source 停止失败。</summary>
        SourceFailure,
    }

    /// <summary>一条不可变的运行时故障记录。</summary>
    public readonly struct RedDotDiagnostic
    {
        /// <summary>故障阶段。</summary>
        public RedDotDiagnosticKind Kind { get; }
        /// <summary>关联节点；Source 全局故障时可能为空键。</summary>
        public RedDotKey Key { get; }
        /// <summary>Rule、Source、Aggregator 或回调名称。</summary>
        public string Owner { get; }
        /// <summary>原始异常。</summary>
        public Exception Exception { get; }
        /// <summary>故障发生时所在的提交批次。</summary>
        public ulong BatchVersion { get; }

        /// <summary>创建包含阶段、所有者、节点和批次版本的诊断。</summary>
        public RedDotDiagnostic(RedDotDiagnosticKind kind, RedDotKey key, string owner, Exception exception, ulong batchVersion = 0)
        {
            Kind = kind;
            Key = key;
            Owner = owner ?? string.Empty;
            Exception = exception;
            BatchVersion = batchVersion;
        }
    }

    /// <summary>Debugger 依赖的只读诊断端口。</summary>
    public interface IRedDotDiagnostics
    {
        /// <summary>诊断可观察数据的版本；版本未变化时可复用上一次快照。</summary>
        ulong DiagnosticsVersion { get; }

        /// <summary>在框架隔离一条异常后触发。</summary>
        event Action<RedDotDiagnostic> DiagnosticReported;

        /// <summary>复制当前图和运行态数据。</summary>
        RedDotNodeSnapshot[] GetDiagnosticsSnapshot();
    }

    /// <summary>单条直接贡献的只读快照。</summary>
    public readonly struct RedDotContributionSnapshot
    {
        /// <summary>贡献所有者名称。</summary>
        public string Owner { get; }
        /// <summary>贡献类别。</summary>
        public RedDotInputKind Kind { get; }
        /// <summary>快照数量。</summary>
        public int Count { get; }

        /// <summary>创建一条贡献快照。</summary>
        public RedDotContributionSnapshot(string owner, RedDotInputKind kind, int count)
        {
            Owner = owner;
            Kind = kind;
            Count = count;
        }
    }

    /// <summary>节点在获取时刻的不可变诊断快照。</summary>
    public sealed class RedDotNodeSnapshot
    {
        /// <summary>节点键。</summary>
        public RedDotKey Key { get; }
        /// <summary>Catalog 调试名称。</summary>
        public string DebugName { get; }
        /// <summary>最近提交状态。</summary>
        public RedDotState State { get; }
        /// <summary>聚合器类型名。</summary>
        public string Aggregator { get; }
        /// <summary>直接父节点。</summary>
        public RedDotKey[] Parents { get; }
        /// <summary>直接子节点。</summary>
        public RedDotKey[] Children { get; }
        /// <summary>直接贡献。</summary>
        public RedDotContributionSnapshot[] Contributions { get; }
        /// <summary>Rule 数量。</summary>
        public int RuleCount { get; }
        /// <summary>订阅者数量。</summary>
        public int SubscriberCount { get; }
        /// <summary>是否为按需实例化的动态节点。</summary>
        public bool IsDynamic { get; }
        /// <summary>是否存在任一阶段的待处理变化。</summary>
        public bool IsDirty { get; }
        /// <summary>最近一次聚合耗时。</summary>
        public double LastCalculationMilliseconds { get; }
        /// <summary>最近一条失败诊断；成功计算不会清除它。</summary>
        public RedDotDiagnostic? LastDiagnostic { get; }

        /// <summary>创建只读节点快照。</summary>
        public RedDotNodeSnapshot(
            RedDotKey key,
            string debugName,
            RedDotState state,
            string aggregator,
            RedDotKey[] parents,
            RedDotKey[] children,
            RedDotContributionSnapshot[] contributions,
            int ruleCount,
            int subscriberCount,
            bool isDynamic,
            bool isDirty,
            double lastCalculationMilliseconds,
            RedDotDiagnostic? lastDiagnostic)
        {
            Key = key;
            DebugName = debugName;
            State = state;
            Aggregator = aggregator;
            Parents = parents ?? Array.Empty<RedDotKey>();
            Children = children ?? Array.Empty<RedDotKey>();
            Contributions = contributions ?? Array.Empty<RedDotContributionSnapshot>();
            RuleCount = ruleCount;
            SubscriberCount = subscriberCount;
            IsDynamic = isDynamic;
            IsDirty = isDirty;
            LastCalculationMilliseconds = lastCalculationMilliseconds;
            LastDiagnostic = lastDiagnostic;
        }
    }
}
