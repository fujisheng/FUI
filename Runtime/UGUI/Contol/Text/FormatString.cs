using System.Collections.Generic;
using System.Text;
using System;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 格式化字符串
    /// </summary>
    public struct FormatString : IEquatable<FormatString>
    {
        public readonly string Text;
        public readonly object[] Args;

        public FormatString(string text, params object[] args)
        {
            Text = text;
            Args = args ?? Array.Empty<object>();
        }

        public bool Equals(FormatString other)
        {
            if (!string.Equals(Text, other.Text, StringComparison.Ordinal))
            {
                return false;
            }

            if (Args == null && other.Args == null)
            {
                return true;
            }
            
            if (Args == null || other.Args == null)
            {
                return false;
            }

            if (Args.Length != other.Args.Length)
            {
                return false;
            }

            for (int i = 0; i < Args.Length; i++)
            {
                if (!Equals(Args[i], other.Args[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Text?.GetHashCode() ?? 0;
                foreach (var arg in Args)
                {
                    hash = (hash * 397) ^ (arg?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Text) || Args.Length == 0)
            {
                return Text ?? string.Empty;
            }

            var sb = new StringBuilder(Text.Length + Args.Length * 8);
            sb.AppendFormat(Text, Args);
            return sb.ToString();
        }
    }
}