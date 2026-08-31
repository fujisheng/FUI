using System;

namespace FUI.Infrastructure
{
    /// <summary>
    /// 表示框架内部 ViewInstance 的存活和激活状态。
    /// </summary>
    [Flags]
    internal enum ViewInstanceState
    {
        /// <summary>
        /// 存活
        /// </summary>
        Alive = 1 << 0,

        /// <summary>
        /// 激活
        /// </summary>
        Enabled = 1 << 1,
    }
}
