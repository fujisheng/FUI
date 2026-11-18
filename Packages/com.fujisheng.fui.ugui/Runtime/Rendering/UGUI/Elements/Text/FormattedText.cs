using System;
using System.Collections.Generic;
using System.Globalization;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 表示带参数和可选格式提供程序的不可变格式化文本。
    /// </summary>
    public readonly struct FormattedText : IEquatable<FormattedText>, FUI.Rendering.IFormattedTextValue
    {
        readonly object[] arguments;

        /// <summary>格式字符串。</summary>
        public string Format { get; }

        /// <summary>格式化参数的只读视图。</summary>
        public IReadOnlyList<object> Arguments => arguments ?? Array.Empty<object>();

        /// <summary>可选格式提供程序；为空时使用当前区域文化。</summary>
        public IFormatProvider FormatProvider { get; }

        /// <summary>使用当前文化创建格式化文本。</summary>
        public FormattedText(string format, params object[] arguments)
            : this(format, null, arguments)
        {
        }

        /// <summary>使用指定格式提供程序创建格式化文本。</summary>
        public FormattedText(string format, IFormatProvider formatProvider, params object[] arguments)
        {
            Format = format;
            FormatProvider = formatProvider;
            this.arguments = arguments == null || arguments.Length == 0
                ? Array.Empty<object>()
                : (object[])arguments.Clone();
        }

        /// <summary>比较格式、格式提供程序和全部参数。</summary>
        public bool Equals(FormattedText other)
        {
            if (!string.Equals(Format, other.Format, StringComparison.Ordinal)
                || !Equals(FormatProvider, other.FormatProvider))
            {
                return false;
            }

            var left = arguments ?? Array.Empty<object>();
            var right = other.arguments ?? Array.Empty<object>();
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (!Equals(left[index], right[index]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is FormattedText other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Format?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ (FormatProvider?.GetHashCode() ?? 0);
                var values = arguments ?? Array.Empty<object>();
                for (var index = 0; index < values.Length; index++)
                {
                    hash = (hash * 397) ^ (values[index]?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        /// <summary>按指定格式提供程序或当前文化生成字符串。</summary>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Format))
            {
                return Format ?? string.Empty;
            }

            var values = arguments ?? Array.Empty<object>();
            return values.Length == 0
                ? Format
                : string.Format(FormatProvider ?? CultureInfo.CurrentCulture, Format, values);
        }

        /// <summary>判断两个值是否相等。</summary>
        public static bool operator ==(FormattedText left, FormattedText right) => left.Equals(right);

        /// <summary>判断两个值是否不相等。</summary>
        public static bool operator !=(FormattedText left, FormattedText right) => !left.Equals(right);
    }
}
