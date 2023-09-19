using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// 界面容器
    /// </summary>
    internal class Container
    {
        public string Name { get; private set; }
        View view;
        ObservableObject viewModel;
        ViewBehavior behavior;

        private Container() { }

        /// <summary>
        /// 构建一个容器
        /// </summary>
        /// <param name="view">对应的View</param>
        /// <param name="viewModel">对应的ViewModel</param>
        /// <param name="behavior">对应的Behavior</param></param>
        public static Container Create(View view, ObservableObject viewModel, ViewBehavior behavior)
        {
            var container = new Container();
            container.Name = view.Name;
            container.view = view;
            container.viewModel = viewModel;
            container.behavior = behavior;
            behavior.OnCreate(viewModel);
            return container;
        }

        /// <summary>
        /// 打开这个容器
        /// </summary>
        internal void Open(object param)
        {
            this.view.Enable();
            this.behavior.OnOpen(param);
        }

        /// <summary>
        /// 关闭这个容器
        /// </summary>
        internal void Close()
        {
            this.behavior.OnClose();
            this.view.Disable();
        }

        /// <summary>
        /// 聚焦这个容器
        /// </summary>
        internal void Focus()
        {
            this.view.Binding(this.viewModel);
            this.behavior.OnFocus();
        }

        /// <summary>
        /// 失焦这个容器
        /// </summary>
        internal void Unfocus()
        {
            this.behavior.OnUnfocus();
            this.view.Unbinding();
        }

        /// <summary>
        /// 销毁这个容器
        /// </summary>
        internal void Destroy()
        {
            this.behavior.OnDestroy();
            this.view.Destroy();
        }
    }
}