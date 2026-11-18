using System;

namespace FUI.RedDot
{
    /// <summary>
    /// 标记由 Source Generator 处理的静态节点目录。目标类型必须是顶层 static partial 类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RedDotCatalogAttribute : Attribute
    {
    }

    /// <summary>
    /// 标记静态 <see cref="RedDotKey"/> 字段，并声明父节点字段名和可选聚合器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class RedDotNodeAttribute : Attribute
    {
        /// <summary>同一 Catalog 内的父节点字段名；空数组表示根节点。</summary>
        public string[] Parents { get; }

        /// <summary>节点聚合器类型；未设置时使用 <see cref="SumAggregator"/>。</summary>
        public Type Aggregator { get; set; }

        /// <summary>声明当前静态节点的父字段名。</summary>
        public RedDotNodeAttribute(params string[] parents)
        {
            Parents = parents ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// 标记动态 RedDotKey 工厂。生成器据此注册动态模板，而不会在运行时使用反射扫描。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class RedDotFactoryAttribute : Attribute
    {
        /// <summary>同一 Catalog 内的父节点字段名，至少需要一个父节点。</summary>
        public string[] Parents { get; }

        /// <summary>动态节点实例使用的聚合器类型；未设置时使用 <see cref="SumAggregator"/>。</summary>
        public Type Aggregator { get; set; }

        /// <summary>声明动态模板实例连接的父字段名。</summary>
        public RedDotFactoryAttribute(params string[] parents)
        {
            Parents = parents ?? Array.Empty<string>();
        }
    }
}
