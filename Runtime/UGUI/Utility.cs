using System;

namespace FUI.UGUI
{
    public static class Utility
    {
        /// <summary>
        /// 移除名字末尾的 " (Clone)"
        /// </summary>
        /// <param name="name">原始名字</param>
        /// <returns>移除后名字</returns>
        public static string RemoveCloneSuffix(string name)
        {
            const string cloneSuffix = "(Clone)";
            if (name.AsSpan().EndsWith(cloneSuffix.AsSpan(), StringComparison.Ordinal))
            {
                return name.AsSpan(0, name.Length - cloneSuffix.Length).ToString();
            }
            return name;
        }
    }
}