using System;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 可组合的表现行为，返回的资源由上下文逆序释放。
    /// </summary>
    public interface IViewBehavior
    {
        IDisposable Attach(ViewContext context);
    }
}
