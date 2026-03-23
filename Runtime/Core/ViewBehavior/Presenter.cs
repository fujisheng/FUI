using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 展示器基类
    /// </summary>
    /// <typeparam name="TObservableObject">视图模型类型</typeparam>
    public abstract class Presenter<TObservableObject> : IPresenter, IPresenter<TObservableObject> where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 这个展示器所对应的视图模型
        /// </summary>
        protected TObservableObject VM { get; private set; }

        /// <summary>
        /// 当更新视图模型的时候
        /// </summary>
        protected virtual void OnUpdateViewModel(TObservableObject oldViewModel, TObservableObject newViewModel) { }

        /// <summary>
        /// 当创建这个展示器的时候
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

        Type IPresenter.ViewModelType => typeof(TObservableObject);

        bool IPresenter.UpdateViewModel(ObservableObject vm)
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

        void IPresenter.OnCreate(ObservableObject vm)
        {
            if(!(this as IPresenter).UpdateViewModel(vm))
            {
                throw new Exception($"create '{this}' failed, can not convert 'ObservableObject({vm})' to '{typeof(TObservableObject)}'");
            }

            OnCreate();
        }

        void IPresenter.OnEnable(object param) => OnOpen(param);
        void IPresenter.OnFocus() => OnFocus();
        void IPresenter.OnUnfocus() => OnUnfocus();
        void IPresenter.OnDisable() => OnClose();
        void IPresenter.OnDestroy() => OnDestroy();
        #endregion
    }
}
