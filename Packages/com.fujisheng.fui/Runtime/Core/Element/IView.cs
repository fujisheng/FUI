using System;
using System.Collections.Generic;

namespace FUI.Rendering
{
    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// 视图名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 层级
        /// </summary>
        int Layer { get; set; }

        /// <summary>
        /// 顺序
        /// </summary>
        int Order { get; set; }

        /// <summary>
        /// 可见性
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// 可交互性
        /// </summary>
        bool Interactable { get; set; }

        /// <summary>
        /// 所有子节点
        /// </summary>
        IReadOnlyList<IElement> Elements { get; }

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <typeparam name="T">子节点的类型</typeparam>
        /// <param name="path">子节点的路径</param>
        /// <returns></returns>
        T GetElement<T>(string path) where T : IElement;

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <param name="path">子节点路径</param>
        /// <param name="elementType">子节点类型</param>
        /// <returns></returns>
        IElement GetElement(string path, Type elementType);

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }

    /// <summary>由导航当前页请求键盘输入焦点；渲染后端负责等待页面可交互后恢复。</summary>
    public interface IViewInputFocus
    {
        void FocusInput();
    }

    /// <summary>
    /// 无法通过事件通知变化、需要在渲染前主动同步的外部值源。
    /// </summary>
    public interface IExternalValueSource
    {
        void SynchronizeExternalValues();
    }

    /// <summary>
    /// 以 View 为粒度汇总外部值同步，避免每个 Element 分别注册 Canvas 回调。
    /// </summary>
    public interface IExternalValueSynchronizationHost
    {
        bool ExternalValueSynchronizationEnabled { get; set; }

        void SetExternalValueSources(IReadOnlyList<IExternalValueSource> sources);

        void ClearExternalValueSources();
    }
}
