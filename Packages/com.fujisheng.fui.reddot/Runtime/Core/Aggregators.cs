using System;

namespace FUI.RedDot
{
    /// <summary>
    /// 标识聚合输入来自节点自身贡献还是直接子节点。
    /// </summary>
    public enum RedDotInputKind
    {
        /// <summary>规则或来源直接提供给当前节点的贡献。</summary>
        Direct,
        /// <summary>直接子节点最近提交或本批待提交的状态。</summary>
        Child,
    }

    /// <summary>
    /// 提供给聚合器的只读输入项。框架按“自身贡献在前、直接子节点在后”的顺序填充。
    /// </summary>
    public readonly struct RedDotAggregationInput
    {
        /// <summary>贡献或子节点所属的键。</summary>
        public RedDotKey Key { get; }
        /// <summary>非负输入值。</summary>
        public RedDotValue Value { get; }
        /// <summary>输入来源类别。</summary>
        public RedDotInputKind Kind { get; }

        /// <summary>创建一项聚合输入。</summary>
        public RedDotAggregationInput(RedDotKey key, RedDotValue value, RedDotInputKind kind)
        {
            Key = key;
            Value = value;
            Kind = kind;
        }
    }

    /// <summary>
    /// 将节点自身贡献和直接子节点状态合并成单个非负值。
    /// 实现必须无状态，且不得保留传入的 <see cref="ReadOnlySpan{T}"/>。
    /// </summary>
    public interface IRedDotAggregator
    {
        /// <summary>计算本节点的新值。</summary>
        RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs);
    }

    /// <summary>
    /// 将全部输入相加；发生整数溢出时饱和为 <see cref="int.MaxValue"/>。
    /// 共享后代通过多条直接边汇入时会按边重复计数。
    /// </summary>
    public sealed class SumAggregator : IRedDotAggregator
    {
        /// <inheritdoc />
        public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
        {
            var count = 0;
            for (var index = 0; index < inputs.Length; index++)
            {
                var value = inputs[index].Value.Count;
                if (int.MaxValue - count < value)
                {
                    return new RedDotValue(int.MaxValue);
                }
                count += value;
            }
            return new RedDotValue(count);
        }
    }

    /// <summary>任一输入大于零时返回 1，否则返回 0。</summary>
    public sealed class AnyAggregator : IRedDotAggregator
    {
        /// <inheritdoc />
        public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
        {
            for (var index = 0; index < inputs.Length; index++)
            {
                if (inputs[index].Value.Count > 0)
                {
                    return new RedDotValue(1);
                }
            }
            return default;
        }
    }

    /// <summary>返回全部输入中的最大值，没有输入时返回 0。</summary>
    public sealed class MaxAggregator : IRedDotAggregator
    {
        /// <inheritdoc />
        public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
        {
            var maximum = 0;
            for (var index = 0; index < inputs.Length; index++)
            {
                if (inputs[index].Value.Count > maximum)
                {
                    maximum = inputs[index].Value.Count;
                }
            }
            return new RedDotValue(maximum);
        }
    }
}
