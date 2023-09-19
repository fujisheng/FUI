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
        /// 当创建这个视图行为的时候
        /// </summary>
        /// <param name="vm"></param>
        public virtual void OnCreate(ObservableObject vm)
        {
            this.VM = vm;
        }

        /// <summary>
        /// 当打开这个View的时候
        /// </summary>
        /// <param name="param">打开时传入的参数</param>
        public virtual void OnOpen(object param) { }

        /// <summary>
        /// 当这个界面是当前聚焦的View的时候
        /// </summary>
        public virtual void OnFocus() { }

        /// <summary>
        /// 当这个界面失焦的时候
        /// </summary>
        public virtual void OnUnfocus() { }

        /// <summary>
        /// 当关闭这个View的时候
        /// </summary>
        public virtual void OnClose() { }

        /// <summary>
        /// 当销毁这个View的时候
        /// </summary>
        public virtual void OnDestroy() { }
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
        /// 当创建的时候
        /// </summary>
        /// <param name="vm"></param>
        public sealed override void OnCreate(ObservableObject vm)
        {
            if(vm is TObservableObject tvm)
            {
                this.VM = tvm;
                OnCreate();
            }
        }

        /// <summary>
        /// 当创建这个视图行为的时候
        /// </summary>
        /// <param name="vm"></param>
        protected virtual void OnCreate() { }
    }
}
