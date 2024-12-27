using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 视图行为
    /// </summary>
    /// <typeparam name="TObservableObject">视图类型</typeparam>
    public interface IViewBehavior<in TObservableObject> where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 可以处理的视图模型类型
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="vm">视图模型</param>
        internal void UpdateViewModel(TObservableObject vm);

        /// <summary>
        /// 当创建这个视图行为的时候
        /// </summary>
        /// <param name="vm">创建时的视图模型</param>
        internal void InternalOnCreate(TObservableObject vm);

        /// <summary>
        /// 当打开这个View的时候
        /// </summary>
        /// <param name="param">打开时的参数</param>
        internal void InternalOnOpen(object param);

        /// <summary>
        /// 当这个界面是当前聚焦的View的时候
        /// </summary>
        internal void InternalOnFocus();

        /// <summary>
        /// 当这个界面失焦的时候
        /// </summary>
        internal void InternalOnUnfocus();

        /// <summary>
        /// 当这个界面关闭的时候
        /// </summary>
        internal void InternalOnClose();

        /// <summary>
        /// 当这个界面销毁的时候
        /// </summary>
        internal void InternalOnDestroy();
    }

    /// <summary>
    /// 视图行为
    /// </summary>
    public abstract class ViewBehavior<TObservableObject> : IViewBehavior<TObservableObject> where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 这个视图行为所对应的视图模型
        /// </summary>
        protected TObservableObject VM { get; private set; }

        /// <summary>
        /// 当创建这个视图行为的时候
        /// </summary>
        /// <param name="vm"></param>
        protected virtual void OnCreate() { }

        /// <summary>
        /// 当打开这个View的时候
        /// </summary>
        /// <param name="param">打开时传入的参数</param>
        protected virtual void OnOpen(object param) { }

        /// <summary>
        /// 当这个界面是当前聚焦的View的时候
        /// </summary>
        protected virtual void OnFocus() { }

        /// <summary>
        /// 当这个界面失焦的时候
        /// </summary>
        protected virtual void OnUnfocus() { }

        /// <summary>
        /// 当关闭这个View的时候
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// 当销毁这个View的时候
        /// </summary>
        protected virtual void OnDestroy() { }

        #region 内部调用

        Type IViewBehavior<TObservableObject>.ViewModelType => typeof(TObservableObject);

        void IViewBehavior<TObservableObject>.UpdateViewModel(TObservableObject vm)
        {
            this.VM = vm;
        }

        void IViewBehavior<TObservableObject>.InternalOnCreate(TObservableObject vm)
        {
            this.VM = vm;
            OnCreate();
        }
        void IViewBehavior<TObservableObject>.InternalOnOpen(object param) => OnOpen(param);
        void IViewBehavior<TObservableObject>.InternalOnFocus() => OnFocus();
        void IViewBehavior<TObservableObject>.InternalOnUnfocus() => OnUnfocus();
        void IViewBehavior<TObservableObject>.InternalOnClose() => OnClose();
        void IViewBehavior<TObservableObject>.InternalOnDestroy() => OnDestroy();
        #endregion
    }

    /// <summary>
    /// 空的视图行为 用于不需要行为的View
    /// </summary>
    public class EmptyViewBehavior : ViewBehavior<ObservableObject> { }
}
