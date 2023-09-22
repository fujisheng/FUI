using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 界面容器
    /// </summary>
    class ViewContainer
    {
        internal string Name { get; private set; }

        Type viewType;
        IView view;
        ObservableObject viewModel;
        ViewBehavior behavior;

        private ViewContainer() { }

        /// <summary>
        /// 构建一个容器
        /// </summary>
        /// <param name="view">对应的View</param>
        /// <param name="viewModel">对应的ViewModel</param>
        /// <param name="behavior">对应的Behavior</param></param>
        internal static ViewContainer Create(IView view, ObservableObject viewModel, ViewBehavior behavior)
        {
            var container = new ViewContainer
            {
                Name = view.Name,

                viewType = view.GetType(),
                view = view,
                viewModel = viewModel,
                behavior = behavior
            };

            container.SynchronizeProperties();

            behavior.InternalOnCreate(viewModel);
            return container;
        }

        /// <summary>
        /// 同步所有属性值
        /// </summary>
        void SynchronizeProperties()
        {
            if(this.viewModel is ISynchronizeProperties synchronizeProperties)
            {
                synchronizeProperties.Synchronize();
            }
        }

        /// <summary>
        /// 打开这个容器
        /// </summary>
        internal void Open(object param)
        {
            if(this.view.BindingContext == null)
            {
                this.view.Binding(this.viewModel);
                SynchronizeProperties();
            }

            if(this.view.BindingContext != this.viewModel)
            {
                this.view.Unbinding();
                this.view.Binding(this.viewModel);
                SynchronizeProperties();
            }

            this.view.Enable();
            this.behavior.InternalOnOpen(param);
        }

        /// <summary>
        /// 关闭这个容器
        /// </summary>
        internal void Close()
        {
            this.behavior.InternalOnClose();
            this.view.Disable();
            this.view.Unbinding();
        }

        /// <summary>
        /// 聚焦这个容器
        /// </summary>
        internal void Focus()
        {
            this.behavior.InternalOnFocus();
        }

        /// <summary>
        /// 失焦这个容器
        /// </summary>
        internal void Unfocus()
        {
            this.behavior.InternalOnUnfocus();
        }

        /// <summary>
        /// 销毁这个容器
        /// </summary>
        internal void Destroy()
        {
            if(this.view.BindingContext != null)
            {
                this.view.Unbinding();
            }

            this.behavior.InternalOnDestroy();
            this.view.Destroy();
        }

        /// <summary>
        /// 冻结这个容器 只保留逻辑和数据
        /// </summary>
        internal void Freeze()
        {
            this.view.Unbinding();
            this.view.Destroy();
            this.view = null;
        }

        /// <summary>
        /// 解冻这个容器 重新根据现有的逻辑和数据构建界面
        /// </summary>
        internal void Unfreeze(IViewBuilder builder)
        {
            var view = builder.BuildView(new ViewBuildParam(Name, viewType), this.viewModel);
            if(view == null)
            {
                throw new Exception($"Unfreeze ViewBuilder.BuildView failed, viewName: {Name}, viewType: {viewType} viewModel:{this.viewModel}");
            }

            this.view = view;
            this.view.Binding(this.viewModel);
            SynchronizeProperties();
        }
    }
}