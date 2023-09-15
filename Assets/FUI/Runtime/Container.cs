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

        /// <summary>
        /// 初始化容器
        /// </summary>
        /// <param name="view">对应的View</param>
        /// <param name="viewModel">对应的ViewModel</param>
        /// <param name="behavior">对应的Behavior</param></param>
        internal Container(View view, ObservableObject viewModel, ViewBehavior behavior)
        {
            this.Name = view.Name;
            this.view = view;
            this.viewModel = viewModel;
            this.behavior = behavior;

            this.behavior.OnCreate(viewModel);
        }

        /// <summary>
        /// 打开这个容器
        /// </summary>
        internal void Open(object param)
        {
            this.behavior.OnOpen(param);
        }

        /// <summary>
        /// 关闭这个容器
        /// </summary>
        internal void Close()
        {
            this.behavior.OnClose();
        }

        /// <summary>
        /// 暂停这个容器
        /// </summary>
        internal void Pause()
        {
            this.behavior.OnPause();
            this.view.Unbinding();
        }

        /// <summary>
        /// 恢复这个容器
        /// </summary>
        internal void Resume()
        {
            this.view.Binding(this.viewModel);
            this.behavior.OnResume();
        }

        /// <summary>
        /// 销毁这个容器
        /// </summary>
        internal void Destroy()
        {
            this.behavior.OnDestroy();
            this.view.OnDestroy();
        }
    }
}