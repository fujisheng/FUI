using System;
using System.ComponentModel;

namespace FUI.RedDot
{
    /// <summary>Source Generator 与运行时之间的节点注册协议。</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct RedDotNodeDefinition
    {
        internal RedDotKey Key { get; }
        internal string DebugName { get; }
        internal IRedDotAggregator Aggregator { get; }

        /// <summary>创建节点定义；项目代码通常不应直接调用。</summary>
        public RedDotNodeDefinition(RedDotKey key, string debugName, IRedDotAggregator aggregator = null)
        {
            Key = key;
            DebugName = debugName ?? key.Value;
            Aggregator = aggregator ?? new SumAggregator();
        }
    }

    /// <summary>Source Generator 与运行时之间的父子边注册协议。</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct RedDotEdgeDefinition
    {
        internal RedDotKey Parent { get; }
        internal RedDotKey Child { get; }

        /// <summary>创建一条父节点到直接子节点的边。</summary>
        public RedDotEdgeDefinition(RedDotKey parent, RedDotKey child)
        {
            Parent = parent;
            Child = child;
        }
    }

    /// <summary>Source Generator 与运行时之间的动态节点模板协议。</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct RedDotDynamicTemplate
    {
        readonly RedDotKey[] parents;

        internal RedDotKey[] Parents => parents;
        internal RedDotKey Parent => parents[0];
        internal string Segment { get; }
        internal string DebugName { get; }
        internal IRedDotAggregator Aggregator { get; }

        /// <summary>创建单父级动态模板。</summary>
        public RedDotDynamicTemplate(RedDotKey parent, string segment, string debugName, IRedDotAggregator aggregator = null)
            : this(new[] { parent }, segment, debugName, aggregator) { }

        /// <summary>创建多父级动态模板；传入数组会被复制。</summary>
        public RedDotDynamicTemplate(RedDotKey[] parents, string segment, string debugName, IRedDotAggregator aggregator = null)
        {
            if (parents == null || parents.Length == 0)
            {
                throw new ArgumentException("A dynamic red dot template requires at least one parent.", nameof(parents));
            }
            this.parents = (RedDotKey[])parents.Clone();
            Segment = string.IsNullOrWhiteSpace(segment)
                ? throw new ArgumentException("A dynamic red dot segment cannot be empty.", nameof(segment))
                : segment;
            DebugName = debugName ?? segment;
            Aggregator = aggregator ?? new SumAggregator();
        }
    }

    /// <summary>Source Generator 输出的不可变图注册协议。</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RedDotCatalogDefinition
    {
        readonly RedDotNodeDefinition[] nodes;
        readonly RedDotEdgeDefinition[] edges;
        readonly RedDotDynamicTemplate[] dynamicTemplates;

        internal RedDotNodeDefinition[] Nodes => nodes;
        internal RedDotEdgeDefinition[] Edges => edges;
        internal RedDotDynamicTemplate[] DynamicTemplates => dynamicTemplates;

        /// <summary>创建图定义；所有输入数组都会被复制。</summary>
        public RedDotCatalogDefinition(
            RedDotNodeDefinition[] nodes,
            RedDotEdgeDefinition[] edges = null,
            RedDotDynamicTemplate[] dynamicTemplates = null)
        {
            this.nodes = nodes == null ? Array.Empty<RedDotNodeDefinition>() : (RedDotNodeDefinition[])nodes.Clone();
            this.edges = edges == null ? Array.Empty<RedDotEdgeDefinition>() : (RedDotEdgeDefinition[])edges.Clone();
            this.dynamicTemplates = dynamicTemplates == null
                ? Array.Empty<RedDotDynamicTemplate>()
                : (RedDotDynamicTemplate[])dynamicTemplates.Clone();
        }
    }
}
