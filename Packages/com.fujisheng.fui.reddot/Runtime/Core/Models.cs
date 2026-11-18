using System;
using System.Globalization;
using System.Text;

namespace FUI.RedDot
{
    /// <summary>红点图中稳定且可比较的强类型键。</summary>
    public readonly struct RedDotKey : IEquatable<RedDotKey>, IComparable<RedDotKey>
    {
        readonly string value;

        /// <summary>经过转义的稳定路径；默认值返回空字符串。</summary>
        public string Value => value ?? string.Empty;

        /// <summary>当前键是否可用于注册、发布和订阅。</summary>
        public bool IsValid => !string.IsNullOrEmpty(value);

        RedDotKey(string value) { this.value = value; }

        /// <summary>使用单个稳定路径段创建根节点。</summary>
        public static RedDotKey Root(string name) => new RedDotKey(EscapeSegment(name));

        /// <summary>在指定父节点下创建静态子节点。</summary>
        public static RedDotKey Child(RedDotKey parent, string name)
        {
            EnsureParent(parent);
            return new RedDotKey(parent.Value + "/" + EscapeSegment(name));
        }

        /// <summary>使用 int 参数创建动态节点。</summary>
        public static RedDotKey Dynamic(RedDotKey parent, string name, int argument) => DynamicCore(parent, name, argument.ToString(CultureInfo.InvariantCulture));

        /// <summary>使用 long 参数创建动态节点。</summary>
        public static RedDotKey Dynamic(RedDotKey parent, string name, long argument) => DynamicCore(parent, name, argument.ToString(CultureInfo.InvariantCulture));

        /// <summary>使用 string 参数创建动态节点；null 按空字符串编码。</summary>
        public static RedDotKey Dynamic(RedDotKey parent, string name, string argument) => DynamicCore(parent, name, argument ?? string.Empty);

        /// <summary>使用标准 D 格式 Guid 创建动态节点。</summary>
        public static RedDotKey Dynamic(RedDotKey parent, string name, Guid argument) => DynamicCore(parent, name, argument.ToString("D", CultureInfo.InvariantCulture));

        /// <summary>使用多个稳定参数创建动态节点；长度前缀用于消除组合歧义。</summary>
        public static RedDotKey Dynamic(RedDotKey parent, string name, params RedDotArgument[] arguments)
        {
            EnsureParent(parent);
            if (arguments == null || arguments.Length == 0)
            {
                throw new ArgumentException("A dynamic red dot key requires at least one argument.", nameof(arguments));
            }

            var builder = new StringBuilder(parent.Value.Length + name.Length + arguments.Length * 12 + 2);
            builder.Append(parent.Value).Append('/').Append(EscapeSegment(name));
            for (var index = 0; index < arguments.Length; index++)
            {
                builder.Append('/').Append(EscapeDynamicArgument(arguments[index].Value));
            }
            return new RedDotKey(builder.ToString());
        }

        internal static string DynamicPrefix(RedDotKey parent, string name)
        {
            EnsureParent(parent);
            return parent.Value + "/" + EscapeSegment(name) + "/";
        }

        static RedDotKey DynamicCore(RedDotKey parent, string name, string argument)
        {
            EnsureParent(parent);
            return new RedDotKey(parent.Value + "/" + EscapeSegment(name) + "/" + EscapeDynamicArgument(argument));
        }

        static void EnsureParent(RedDotKey parent)
        {
            if (!parent.IsValid) throw new ArgumentException("A red dot parent key cannot be empty.", nameof(parent));
        }

        static string EscapeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) throw new ArgumentException("A red dot key segment cannot be empty.", nameof(segment));
            return Uri.EscapeDataString(segment);
        }

        static string EscapeDynamicArgument(string argument)
        {
            argument = argument ?? string.Empty;
            return Uri.EscapeDataString(argument.Length.ToString(CultureInfo.InvariantCulture) + "~" + argument);
        }

        public bool Equals(RedDotKey other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RedDotKey other && Equals(other);
        public override int GetHashCode() => value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        public int CompareTo(RedDotKey other) => string.Compare(value, other.value, StringComparison.Ordinal);
        public override string ToString() => Value;
        public static bool operator ==(RedDotKey left, RedDotKey right) => left.Equals(right);
        public static bool operator !=(RedDotKey left, RedDotKey right) => !left.Equals(right);
    }

    /// <summary>动态键参数不受区域设置影响的稳定字符串表示。</summary>
    public readonly struct RedDotArgument
    {
        /// <summary>规范化后的参数字符串。</summary>
        public string Value { get; }
        RedDotArgument(string value) { Value = value; }
        public static implicit operator RedDotArgument(int value) => new RedDotArgument(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator RedDotArgument(long value) => new RedDotArgument(value.ToString(CultureInfo.InvariantCulture));
        public static implicit operator RedDotArgument(string value) => new RedDotArgument(value ?? string.Empty);
        public static implicit operator RedDotArgument(Guid value) => new RedDotArgument(value.ToString("D", CultureInfo.InvariantCulture));
    }

    /// <summary>单个规则或来源的非负贡献值。</summary>
    public readonly struct RedDotValue : IEquatable<RedDotValue>
    {
        /// <summary>当前贡献数量，始终大于或等于零。</summary>
        public int Count { get; }
        /// <summary>创建贡献值；负数会抛出异常。</summary>
        public RedDotValue(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "A red dot count cannot be negative.");
            Count = count;
        }
        public bool Equals(RedDotValue other) => Count == other.Count;
        public override bool Equals(object obj) => obj is RedDotValue other && Equals(other);
        public override int GetHashCode() => Count;
        public override string ToString() => Count.ToString(CultureInfo.InvariantCulture);
        public static implicit operator RedDotValue(int count) => new RedDotValue(count);
        public static bool operator ==(RedDotValue left, RedDotValue right) => left.Equals(right);
        public static bool operator !=(RedDotValue left, RedDotValue right) => !left.Equals(right);
    }

    /// <summary>一次已提交的不可变节点状态。</summary>
    public readonly struct RedDotState : IEquatable<RedDotState>
    {
        /// <summary>状态所属节点。</summary>
        public RedDotKey Key { get; }
        /// <summary>聚合后的非负数量。</summary>
        public int Count { get; }
        /// <summary>由 Count 是否大于零推导。</summary>
        public bool IsVisible => Count > 0;
        /// <summary>包含本次状态变化的原子提交批次版本。</summary>
        public ulong BatchVersion { get; }

        /// <summary>创建一份不可变的已提交状态。</summary>
        public RedDotState(RedDotKey key, int count, ulong batchVersion = 0)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "A red dot count cannot be negative.");
            Key = key;
            Count = count;
            BatchVersion = batchVersion;
        }

        public bool Equals(RedDotState other) => Key == other.Key && Count == other.Count && BatchVersion == other.BatchVersion;
        public override bool Equals(object obj) => obj is RedDotState other && Equals(other);
        public override int GetHashCode() => ((Key.GetHashCode() * 397) ^ Count) * 397 ^ BatchVersion.GetHashCode();
        public override string ToString() => $"{Key}: {Count} (batch {BatchVersion})";
    }
}
