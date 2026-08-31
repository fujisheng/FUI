using System;
using UnityEngine;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 提供 UGUI 渲染实现内部使用的名称规范化辅助方法。
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// 移除名字末尾的 " (Clone)"
        /// </summary>
        /// <param name="name">原始名字</param>
        /// <returns>移除后名字</returns>
        public static string RemoveCloneSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name ?? string.Empty;
            }

            const string cloneSuffix = "(Clone)";
            if (name.AsSpan().EndsWith(cloneSuffix.AsSpan(), StringComparison.Ordinal))
            {
                return name.AsSpan(0, name.Length - cloneSuffix.Length).ToString();
            }
            return name;
        }

        /// <summary>
        /// 根据 Unity 当前运行状态安全销毁临时对象。
        /// </summary>
        internal static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
