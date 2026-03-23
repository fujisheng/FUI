using System;

namespace FUI.Manager
{
    /// <summary>
    /// 界面特性
    /// </summary>
    [Flags]
    public enum Attributes
    {
        /// <summary>
        /// 无任何特性
        /// </summary>
        None = 0,

        /// <summary>
        /// 是否全屏
        /// </summary>
        FullScreen = 1,

        /// <summary>
        /// 是否允许同时存在多个实例
        /// </summary>
        AllowMultiple = 1 << 1,

        /// <summary>
        /// 不入栈  不受Back操作影响
        /// </summary>
        Unstackable = 1 << 2,
    }
}