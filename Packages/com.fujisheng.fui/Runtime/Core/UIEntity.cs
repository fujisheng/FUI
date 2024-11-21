using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// UI实体
    /// </summary>
    public partial class UIEntity
    {
        public string Name { get; private set; }

        Type viewType;

        /// <summary>
        /// 绑定上下文
        /// </summary>
        BindingContext bindingContext;

        /// <summary>
        /// 视图
        /// </summary>
        public IView View { get; private set; }

        /// <summary>
        /// 视图模型
        /// </summary>
        public ObservableObject ViewModel { get; private set; }

        /// <summary>
        /// 视图行为
        /// </summary>
        public ViewBehavior Behavior { get; private set; }

        /// <summary>
        /// 层级
        /// </summary>
        public int Layer { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; private set; }


        UIEntity() { }

        public override string ToString()=> $"UIEntity Name:{Name} ViewModel:{ViewModel} ViewBahavior:{Behavior} View:{View}";

        /// <summary>
        /// 更新视图模型，当前视图和视图行为不变
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        public void UpdateViewModel(ObservableObject viewModel)
        {
            if(this.ViewModel == viewModel || viewModel == null)
            {
                return;
            }
            this.bindingContext.UpdateViewModel(viewModel);
            this.ViewModel = viewModel;
            this.Behavior.UpdateViewModel(viewModel);
            SynchronizeProperties();
        }

        /// <summary>
        /// 更新视图行为，当前视图和视图模型不变
        /// </summary>
        /// <param name="behavior"></param>
        public void UpdateBehavior(ViewBehavior behavior)
        {
            this.Behavior = behavior;
            this.Behavior.UpdateViewModel(this.ViewModel);
        }

        /// <summary>
        /// 同步所有属性值
        /// </summary>
        public void SynchronizeProperties()
        {
            if(this.ViewModel is ISynchronizeProperties synchronizeProperties)
            {
                synchronizeProperties.Synchronize();
            }
        }

        /// <summary>
        /// 激活这个UI实体
        /// </summary>
        public void Enable(object param = null)
        {
            this.bindingContext.InternalBinding();
            SynchronizeProperties();
            this.View.Visible = true;
            this.Behavior.InternalOnOpen(param);
        }

        /// <summary>
        /// 使这个UI实体无效
        /// </summary>
        public void Disable()
        {
            this.Behavior.InternalOnClose();
            this.View.Visible = false;
            this.bindingContext.InternalUnbinding();
        }

        /// <summary>
        /// 聚焦这个UI实体
        /// </summary>
        public void Focus()
        {
            this.Behavior.InternalOnFocus();
        }

        /// <summary>
        /// 失焦这个UI实体
        /// </summary>
        public void Unfocus()
        {
            this.Behavior.InternalOnUnfocus();
        }

        /// <summary>
        /// 销毁这个UI实体
        /// </summary>
        public void Destroy()
        {
            this.bindingContext.InternalUnbinding();
            this.Behavior.InternalOnDestroy();
            this.View.Destroy();
        }

        /// <summary>
        /// 冻结这个UI实体 只保留逻辑和数据 销毁视图层
        /// </summary>
        public void Freeze()
        {
            this.bindingContext.InternalUnbinding();
            this.View.Destroy();
            this.View = null;
        }

        /// <summary>
        /// 解冻这个UI实体 重新根据现有的逻辑和数据构建UI实体
        /// </summary>
        public void Unfreeze(IViewFactory factory)
        {
            var view = factory.Create(Name);
            if(view == null)
            {
                throw new Exception($"Unfreeze failed, factory:{factory} cannot create viewName: {Name}, viewType: {viewType} viewModel:{this.ViewModel}");
            }

            this.View = view;
            this.bindingContext.UpdateView(view);
            this.View.Layer = this.Layer;
            this.View.Order = this.Order;
            SynchronizeProperties();
            this.View.Visible = true;
        }

        /// <summary>
        /// 设置这个UI实体的层级
        /// </summary>
        /// <param name="layer">层级</param>
        public void SetLayer(int layer)
        {
            this.View.Layer = layer;
            this.Layer = layer;
        }

        /// <summary>
        /// 设置这个UI实体的顺序
        /// </summary>
        /// <param name="order"></param>
        public void SetOrder(int order)
        {
            this.View.Order = order;
            this.Order = order;
        }
    }
}