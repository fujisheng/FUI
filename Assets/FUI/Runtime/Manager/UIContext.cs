using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 界面上下文
    /// </summary>
    class UIContext : IPresentation
    {
        internal string Name { get; private set; }

        Type viewType;
        IView view;
        ObservableObject viewModel;
        ViewBehavior behavior;

        private UIContext() { }

        /// <summary>
        /// 构建一个上下文
        /// </summary>
        /// <param name="view">对应的View</param>
        /// <param name="viewModel">对应的ViewModel</param>
        /// <param name="behavior">对应的Behavior</param></param>
        internal static UIContext Create(IView view, ObservableObject viewModel, ViewBehavior behavior)
        {
            var context = new UIContext
            {
                Name = view.Name,

                viewType = view.GetType(),
                view = view,
                viewModel = viewModel,
                behavior = behavior
            };

            context.SynchronizeProperties();

            behavior.InternalOnCreate(viewModel);
            return context;
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
        /// 打开这个上下文
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
        /// 关闭这个上下文
        /// </summary>
        internal void Close()
        {
            this.behavior.InternalOnClose();
            this.view.Disable();
            this.view.Unbinding();
        }

        /// <summary>
        /// 聚焦这个上下文
        /// </summary>
        internal void Focus()
        {
            this.behavior.InternalOnFocus();
        }

        /// <summary>
        /// 失焦这个上下文
        /// </summary>
        internal void Unfocus()
        {
            this.behavior.InternalOnUnfocus();
        }

        /// <summary>
        /// 销毁这个上下文
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
        /// 冻结这个上下文 只保留逻辑和数据
        /// </summary>
        internal void Freeze()
        {
            this.view.Unbinding();
            this.view.Destroy();
            this.view = null;
        }

        /// <summary>
        /// 解冻这个上下文 重新根据现有的逻辑和数据构建界面
        /// </summary>
        internal void Unfreeze(IUIBuilder builder)
        {
            var view = builder.BuildView(new UIBuildParam(Name, viewType), this.viewModel);
            if(view == null)
            {
                throw new Exception($"Unfreeze ViewBuilder.BuildView failed, viewName: {Name}, viewType: {viewType} viewModel:{this.viewModel}");
            }

            this.view = view;
            this.view.Binding(this.viewModel);
            SynchronizeProperties();
        }

        void IPresentation.Initialize()
        {
            SynchronizeProperties();
            behavior.InternalOnCreate(viewModel);
        }

        void IPresentation.Destroy()
        {
            this.Destroy();
        }
    }
}