using System;

namespace FUI.Rendering
{
    /// <summary>宿主重建节点时暂停并恢复绑定；View 和 ViewModel 的页面生命周期保持不变。</summary>
    public interface IViewContentLifecycle
    {
        bool IsContentAvailable { get; }
        event Action ContentUnavailable;
        event Action ContentAvailable;
    }
}
