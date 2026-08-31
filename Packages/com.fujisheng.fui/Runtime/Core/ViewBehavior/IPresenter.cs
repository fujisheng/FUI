using FUI.Binding;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Presentation
{
    /// <summary>
    /// 展示器标记接口
    /// </summary>
    /// <typeparam name="TObservableObject">可观察对象</typeparam>
    public interface IPresenter<in TObservableObject> where TObservableObject : ObservableObject { }

    /// <summary>
    /// 展示器
    /// </summary>
    internal interface IPresenter
    {
        /// <summary>
        /// 可以处理的视图模型类型
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="vm">视图模型</param>
        internal bool UpdateViewModel(ObservableObject vm);

        /// <summary>
        /// 当创建这个展示器的时候
        /// </summary>
        /// <param name="vm">创建时的视图模型</param>
        internal void OnCreate(ObservableObject vm);

        /// <summary>
        /// 当激活这个View的时候
        /// </summary>
        /// <param name="param">打开时的参数</param>
        internal void OnOpen(object param);

        /// <summary>
        /// 同步 OnOpen 完成后执行的异步打开阶段。
        /// </summary>
        internal ValueTask OnOpenAsync(object param, CancellationToken cancellationToken);

        /// <summary>
        /// 当这个界面是当前聚焦的View的时候
        /// </summary>
        internal void OnFocus();

        /// <summary>
        /// 当这个界面失焦的时候
        /// </summary>
        internal void OnUnfocus();

        /// <summary>
        /// 被新页面覆盖；不会触发 OnClose，也不会解除绑定。
        /// </summary>
        internal void OnCovered();

        /// <summary>
        /// 从 Covered 恢复；不会重新触发 OnOpen。
        /// </summary>
        internal void OnRevealed();

        /// <summary>
        /// 当反激活这个View的时候
        /// </summary>
        internal void OnClose();

        /// <summary>
        /// 同步 OnClose 之前执行的异步关闭阶段。
        /// </summary>
        internal ValueTask OnCloseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 当这个界面销毁的时候
        /// </summary>
        internal void OnDestroy();
    }
}
