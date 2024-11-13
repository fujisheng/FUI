using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// 视图行为
    /// </summary>
    public abstract class ViewBehavior
    {
        /// <summary>
        /// 所对应的视图模型
        /// </summary>
        protected ObservableObject VM { get; private set; }

        /// <summary>
        /// 设置ViewModel
        /// </summary>
        /// <param name="vm"></param>
        internal virtual void UpdateViewModel(ObservableObject vm)
        {
            this.VM = vm;
        }

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
        internal void InternalOnCreate(ObservableObject vm) 
        {
            UpdateViewModel(vm);
            OnCreate();
        }
        internal void InternalOnOpen(object param) => OnOpen(param);
        internal void InternalOnFocus() => OnFocus();
        internal void InternalOnUnfocus() => OnUnfocus();
        internal void InternalOnClose() => OnClose();
        internal void InternalOnDestroy() => OnDestroy();
        #endregion
    }

    /// <summary>
    /// 视图行为
    /// </summary>
    public abstract class ViewBehavior<TObservableObject> : ViewBehavior where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 这个视图行为所对应的视图模型
        /// </summary>
        protected new TObservableObject VM { get; private set; }

        /// <summary>
        /// 设置ViewModel
        /// </summary>
        /// <param name="vm"></param>
        internal sealed override void UpdateViewModel(ObservableObject vm)
        {
            if(vm is TObservableObject tvm)
            {
                this.VM = tvm;
            }
        }
    }

    /// <summary>
    /// 空的视图行为 用于不需要行为的View
    /// </summary>
    public class EmptyViewBehavior : ViewBehavior { }
}
