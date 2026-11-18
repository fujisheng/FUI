using System;

namespace FUI.Rendering
{
    /// <summary>将一次绑定和展示器启用中的后端更新合并；作用域结束前完成同步。</summary>
    /// <remarks>后端仍须在几何被显式读取时提供最新值，不能延后到下一帧。</remarks>
    public interface IViewUpdate
    {
        IDisposable BeginUpdate();
    }
}
