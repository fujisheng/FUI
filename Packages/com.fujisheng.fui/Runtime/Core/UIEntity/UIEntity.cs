using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// UI实体 对应一个视图
    /// </summary>
    public partial class UIEntity
    {
        /// <summary>
        /// 这个实体的名字
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// view对应的类型
        /// </summary>
        Type viewType;

        /// <summary>
        /// 是否已经被销毁
        /// </summary>
        bool destroyed;

        /// <summary>
        /// 绑定上下文
        /// </summary>
        IBindingContext<ObservableObject> bindingContext;

        /// <summary>
        /// 绑定上下文类型
        /// </summary>
        public Type BindingContextType => bindingContext.GetType();

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
        IViewBehavior<ObservableObject> Behavior { get; set; }

        /// <summary>
        /// 层级
        /// </summary>
        public int Layer { get; private set; }

        /// <summary>
        /// 顺序
        /// </summary>
        public int Order { get; private set; }

        UIEntity() { }

        public override string ToString()=> $"UIEntity Name:{Name} ViewModel:{ViewModel} ViewBahavior:{Behavior} View:{View} Context:{bindingContext.GetType().ToString()}";

        /// <summary>
        /// 更新视图模型，当前视图和视图行为不变 允许更新成子类
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        public void UpdateViewModel(ObservableObject viewModel)
        {
            EnsureNotDestroyed();

            if(this.ViewModel == viewModel || viewModel == null)
            {
                return;
            }

            if (!this.bindingContext.UpdateViewModel(viewModel)) 
            {
                throw new System.Exception($"{this.GetType()}  UpdateViewModel Error {viewModel} not {this.ViewModel.GetType()}");
            }

            this.ViewModel = viewModel;
            this.Behavior.UpdateViewModel(viewModel);
            SynchronizeProperties();
        }

        /// <summary>
        /// 更新视图行为，当前视图和视图模型不变
        /// </summary>
        /// <param name="behavior"></param>
        public void UpdateBehavior<TBehavior>() where TBehavior : IViewBehavior<ObservableObject>
        {
            EnsureNotDestroyed();

            //需要校验新的行为是否和当前的视图模型匹配
            var behavior = Activator.CreateInstance(typeof(TBehavior)) as IViewBehavior<ObservableObject>;
            this.Behavior = behavior;
            this.Behavior.UpdateViewModel(ViewModel);
        }

        /// <summary>
        /// 同步所有属性值
        /// </summary>
        public void SynchronizeProperties()
        {
            EnsureNotDestroyed();

            if (this.ViewModel is ISynchronizeProperties synchronizeProperties)
            {
                synchronizeProperties.Synchronize();
            }
        }

        /// <summary>
        /// 当创建这个UI实体的时候
        /// </summary>
        void OnCreated()
        {
            this.Behavior.OnCreate(ViewModel);
            OnEntityCreated?.Invoke(this);
        }

        /// <summary>
        /// 激活这个UI实体
        /// </summary>
        public void Enable(object param = null)
        {
            EnsureNotDestroyed();

            this.bindingContext.Binding();
            SynchronizeProperties();
            this.View.Visible = true;
            this.Behavior.OnOpen(param);

            OnEntityEnabled?.Invoke(this);
        }

        /// <summary>
        /// 使这个UI实体无效
        /// </summary>
        public void Disable()
        {
            EnsureNotDestroyed();

            this.Behavior.OnClose();
            this.View.Visible = false;
            this.bindingContext.Unbinding();

            OnEntityDisabled?.Invoke(this);
        }

        /// <summary>
        /// 聚焦这个UI实体
        /// </summary>
        public void Focus()
        {
            EnsureNotDestroyed();

            this.Behavior.OnFocus();

            OnEntityFocused?.Invoke(this);
        }

        /// <summary>
        /// 失焦这个UI实体
        /// </summary>
        public void Unfocus()
        {
            EnsureNotDestroyed();

            this.Behavior.OnUnfocus();

            OnEntityUnfocused?.Invoke(this);
        }

        /// <summary>
        /// 销毁这个UI实体
        /// </summary>
        public void Destroy()
        {
            EnsureNotDestroyed();

            this.bindingContext.Unbinding();
            this.Behavior.OnDestroy();
            this.View.Destroy();

            OnEntityDestoryed?.Invoke(this);

            destroyed = true;
        }

        /// <summary>
        /// 冻结这个UI实体 只保留逻辑和数据 销毁视图层
        /// </summary>
        public void Freeze()
        {
            EnsureNotDestroyed();

            this.bindingContext.Unbinding();
            this.View.Destroy();
            this.View = null;

            OnEntityFreezed?.Invoke(this);
        }

        /// <summary>
        /// 解冻这个UI实体 重新根据现有的逻辑和数据构建UI实体
        /// </summary>
        public void Unfreeze(IViewFactory factory)
        {
            EnsureNotDestroyed();

            if(this.View != null)
            {
                return;
            }

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

            OnEntityUnfreezed?.Invoke(this);
        }

        /// <summary>
        /// 设置这个UI实体的层级
        /// </summary>
        /// <param name="layer">层级</param>
        public void SetLayer(int layer)
        {
            EnsureNotDestroyed();

            this.View.Layer = layer;
            this.Layer = layer;
        }

        /// <summary>
        /// 设置这个UI实体的顺序
        /// </summary>
        /// <param name="order"></param>
        public void SetOrder(int order)
        {
            EnsureNotDestroyed();

            this.View.Order = order;
            this.Order = order;
        }

        /// <summary>
        /// 确保这个实体还没有被销毁
        /// </summary>
        /// <exception cref="System.Exception">如果已经被销毁了但是确还在访问则抛出异常</exception>
        void EnsureNotDestroyed()
        {
            if (destroyed)
            {
                throw new System.Exception($"{this} has been destroyed, but you're still trying to access it");
            }
        }
    }
}