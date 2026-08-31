using System;

namespace FUI.Navigation
{
    /// <summary>
    /// 一次页面实例生命周期的唯一标识。
    /// 缓存复用会生成新 Handle，旧 Handle 永久保持终态，不能再次操作新实例。
    /// </summary>
    public readonly struct ViewHandle : IEquatable<ViewHandle>
    {
        public static ViewHandle Invalid => default;

        /// <summary>
        /// 仅用于比较和诊断的递增值，不代表页面类型或显示顺序。
        /// </summary>
        public long Value { get; }

        internal ViewHandle(long value)
        {
            Value = value;
        }

        public bool IsValid => Value != 0;

        public bool Equals(ViewHandle other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ViewHandle other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => IsValid ? $"ViewHandle({Value})" : "ViewHandle.Invalid";

        public static bool operator ==(ViewHandle left, ViewHandle right) => left.Equals(right);

        public static bool operator !=(ViewHandle left, ViewHandle right) => !left.Equals(right);
    }
}
