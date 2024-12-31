using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 视图行为
    /// </summary>
    public abstract class ViewBehavior<TObservableObject> : IViewBehavior where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 这个视图行为所对应的视图模型
        /// </summary>
        protected TObservableObject VM { get; private set; }

        /// <summary>
        /// 当更新视图模型的时候
        /// </summary>
        protected virtual void OnUpdateViewModel(TObservableObject oldViewModel, TObservableObject newViewModel) { }

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

        #region internal

        Type IViewBehavior.ViewModelType => typeof(TObservableObject);

        bool IViewBehavior.UpdateViewModel(ObservableObject vm)
        {
            if(!(vm is TObservableObject tVM))
            {
                return false;
            }

            var oldViewModel = this.VM;
            this.VM = tVM;
            OnUpdateViewModel(oldViewModel, this.VM);
            return true;
        }

        void IViewBehavior.OnCreate(ObservableObject vm)
        {
            if(!(this as IViewBehavior).UpdateViewModel(vm))
            {
                throw new Exception($"create '{this}' failed, can not convert 'ObservableObject({vm})' to '{typeof(TObservableObject)}'");
            }

            OnCreate();
        }

        void IViewBehavior.OnEnable(object param) => OnOpen(param);
        void IViewBehavior.OnFocus() => OnFocus();
        void IViewBehavior.OnUnfocus() => OnUnfocus();
        void IViewBehavior.OnDisable() => OnClose();
        void IViewBehavior.OnDestroy() => OnDestroy();
        #endregion
    }
}
