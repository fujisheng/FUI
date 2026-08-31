using System;

namespace FUI.Infrastructure
{
    /// <summary>
    /// 定义 ViewInstance 生命周期状态变化时使用的框架内部事件。
    /// </summary>
    internal partial class ViewInstance
    {
        /// <summary>
        /// 当有ViewInstance创建时
        /// </summary>
        internal static event Action<ViewInstance> Created;

        /// <summary>
        /// 当有ViewInstance启用时
        /// </summary>
        internal static event Action<ViewInstance> Enabled;

        /// <summary>
        /// 当有ViewInstance禁用时
        /// </summary>
        internal static event Action<ViewInstance> Disabled;

        /// <summary>
        /// 当有ViewInstance获取焦点时
        /// </summary>
        internal static event Action<ViewInstance> Focused;

        /// <summary>
        /// 当有ViewInstance失去焦点时
        /// </summary>
        internal static event Action<ViewInstance> Unfocused;

        /// <summary>
        /// 当有ViewInstance销毁时
        /// </summary>
        internal static event Action<ViewInstance> Destroyed;

    }
}
