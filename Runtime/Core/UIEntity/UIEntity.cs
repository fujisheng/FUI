using FUI.Bindable;

using System;
using System.Runtime.CompilerServices;

namespace FUI
{
    /// <summary>
    /// UI实体 对应一个视图
    /// </summary>
    public partial class UIEntity : IEquatable<UIEntity>
    {
        /// <summary>
        /// 绑定上下文
        /// </summary>
        IBindingContext bindingContext;

        /// <summary>
        /// 视图
        /// </summary>
        IView view;

        /// <summary>
        /// 视图模型
        /// </summary>
        ObservableObject viewModel;

        /// <summary>
        /// 视图模型
        /// </summary>
        public ObservableObject ViewModel
        {
            get => SafeExecute(() => viewModel);
        }

        /// <summary>
        /// 视图
        /// </summary>
        internal IView View
        {
            get => SafeExecute(() => view);
        }

        /// <summary>
        /// 绑定上下文
        /// </summary>
        internal IBindingContext BindingContext
        {
            get => SafeExecute(()=> bindingContext);
        }

        /// <summary>
        /// 上下文类型
        /// </summary>
        Type contextType;

        /// <summary>
        /// 展示器
        /// </summary>
        IPresenter presenter;

        /// <summary>
        /// 实体的名字
        /// </summary>
        string name;

        /// <summary>
        /// 这个实体的名字
        /// </summary>
        public string Name
        {
            get => SafeExecute(() => name);
            private set => SafeExecute(() => name = value);
        }

        int layer;
        /// <summary>
        /// 层级
        /// </summary>
        public int Layer
        {
            get => SafeExecute(() => layer);
            set => SafeExecute(() => SetLayer(value));
        }

        int order;
        /// <summary>
        /// 顺序
        /// </summary>
        public int Order
        {
            get => SafeExecute(() => order);
            set => SafeExecute(() => SetOrder(value));
        }

        bool interactable = true;
        /// <summary>
        /// 可交互性
        /// </summary>
        public bool Interactable
        {
            get => SafeExecute(() => interactable);
            set => SafeExecute(() => SetInteractable(value));
        }

        /// <summary>
        /// 状态
        /// </summary>
        public UIEntityState State { get; private set; }

        /// <summary>
        /// 私有化构造函数  防止外部创建
        /// </summary>
        UIEntity() { }

        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="bindingContext">绑定上下文</param>
        /// <param name="presenter">展示器</param>
        UIEntity(IBindingContext bindingContext, IPresenter presenter)
        {
            this.bindingContext = bindingContext;
            this.view = bindingContext.View;
            this.presenter = presenter;

            State |= UIEntityState.Alive;

            this.viewModel = bindingContext.ViewModel;
            this.contextType = bindingContext.GetType();

            this.Name = view.Name;
            this.Layer = view.Layer;
            this.Order = view.Order;

            //当创建这个UI实体的时候
            this.presenter.OnCreate(viewModel);
            OnEntityCreated?.Invoke(this);
        }

        #region safe execute
        /// <summary>
        /// 确保这个实体还没有被销毁
        /// </summary>
        /// <exception cref="Exception">如果已经被销毁了但是却还在访问则抛出异常</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureNotDestroyed()
        {
            if (!State.HasFlag(UIEntityState.Alive))
            {
                throw new Exception($"UIEntity({name}) has been destroyed, but you're still trying to access it.");
            }
        }

        /// <summary>
        /// 带校验的安全执行
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">委托</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T SafeExecute<T>(Func<T> func)
        {
            EnsureNotDestroyed();
            return func();
        }

        /// <summary>
        /// 带校验的安全执行
        /// </summary>
        /// <param name="action">委托</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SafeExecute(Action action)
        {
            EnsureNotDestroyed();
            action();
        }
        #endregion

        /// <summary>
        /// 更新视图模型，当前视图和展示器不变 允许更新成子类
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        public void UpdateViewModel(ObservableObject viewModel)
        {
            SafeExecute(() =>
            {
                if (this.viewModel == viewModel || viewModel == null)
                {
                    return;
                }

                if (!this.bindingContext.UpdateViewModel(viewModel))
                {
                    throw new Exception($"{this.GetType()}  UpdateViewModel Error {viewModel} not {this.viewModel.GetType()}");
                }

                this.presenter.UpdateViewModel(viewModel);
                this.viewModel = viewModel;
                SynchronizeProperties();
            });
        }

        /// <summary>
        /// 更新展示器，当前视图和视图模型不变
        /// </summary>
        /// <param name="behavior"></param>
        public void UpdatePresenter<TPresenter>()
        {
            SafeExecute(() =>
            {
                if (!typeof(IPresenter).IsAssignableFrom(typeof(TPresenter)))
                {
                    throw new Exception($"{this}  UpdateBehavior Error {typeof(TPresenter)} not IPresenter");
                }

                this.presenter = Activator.CreateInstance(typeof(TPresenter)) as IPresenter;
                this.presenter.UpdateViewModel(viewModel);
                SynchronizeProperties();
            });
        }

        /// <summary>
        /// 同步所有属性值
        /// </summary>
        public void SynchronizeProperties()
        {
            SafeExecute(() =>
            {
                if (this.viewModel is ISynchronizeProperties synchronizeProperties)
                {
                    synchronizeProperties.Synchronize();
                }
            });
        }

        /// <summary>
        /// 激活这个UI实体
        /// </summary>
        public void Enable(object param = null)
        {
            try
            {
                SafeExecute(() =>
                {
                    if (State.HasFlag(UIEntityState.Freezed) || State.HasFlag(UIEntityState.Enabled))
                    {
                        return;
                    }

                    this.bindingContext.Binding();
                    SynchronizeProperties();
                    this.presenter.OnEnable(param);
                    this.view.Visible = true;

                    State |= UIEntityState.Enabled;
                    OnEntityEnabled?.Invoke(this);
                });
            }
            catch (Exception e)
            {
                Logger.Instance.LogException(e);
            }
        }

        /// <summary>
        /// 使这个UI实体无效
        /// </summary>
        public void Disable()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Freezed) || !State.HasFlag(UIEntityState.Enabled))
                {
                    return;
                }

                this.presenter.OnDisable();
                this.view.Visible = false;
                this.bindingContext.Unbinding();

                State &= ~UIEntityState.Enabled;
                OnEntityDisabled?.Invoke(this);
            });
        }

        /// <summary>
        /// 聚焦这个UI实体
        /// </summary>
        public void Focus()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Freezed))
                {
                    return;
                }

                this.presenter.OnFocus();
                OnEntityFocused?.Invoke(this);
            });
        }

        /// <summary>
        /// 失焦这个UI实体
        /// </summary>
        public void Unfocus()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Freezed))
                {
                    return;
                }

                this.presenter.OnUnfocus();
                OnEntityUnfocused?.Invoke(this);
            });
        }

        /// <summary>
        /// 销毁这个UI实体
        /// </summary>
        public void Destroy()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Enabled))
                {
                    this.presenter.OnDisable();
                    OnEntityDisabled?.Invoke(this);
                }

                this.presenter.OnDestroy();
                this.bindingContext?.Unbinding();
                this.view?.Destroy();

                OnEntityDestoryed?.Invoke(this);

                this.bindingContext = null;
                this.presenter = null;
                this.view = null;

                State &= ~UIEntityState.Alive;
            });
        }

        /// <summary>
        /// 冻结这个UI实体 只保留逻辑和数据 销毁视图层
        /// </summary>
        public void Freeze()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Freezed))
                {
                    return;
                }

                //销毁绑定上下文
                this.bindingContext.Unbinding();
                this.bindingContext = null;

                //销毁视图
                this.view.Visible = false;
                this.view.Destroy();
                this.view = null;

                OnEntityFreezed?.Invoke(this);

                //标记为冻结状态
                State |= UIEntityState.Freezed;
            });
        }

        /// <summary>
        /// 解冻这个UI实体 重新根据现有的逻辑和数据构建UI实体
        /// </summary>
        public void Unfreeze(IViewFactory factory)
        {
            SafeExecute(() =>
            {
                if (!State.HasFlag(UIEntityState.Freezed))
                {
                    return;
                }

                if (this.view != null)
                {
                    return;
                }

                var view = factory.Create(name);
                if (view == null)
                {
                    throw new Exception($"Unfreeze failed, factory:{factory} cannot create viewName: {Name} viewModel:{this.viewModel}");
                }

                //恢复上下文
                this.bindingContext = Activator.CreateInstance(contextType, view, viewModel) as IBindingContext;
                this.bindingContext.Binding();
                SynchronizeProperties();

                //恢复视图
                this.view = view;
                this.view.Layer = this.layer;
                this.view.Order = this.order;

                //使其可见
                this.view.Visible = true;
                OnEntityUnfreezed?.Invoke(this);

                //解冻
                State &= ~UIEntityState.Freezed;
            });
        }

        /// <summary>
        /// 是否是这个视图
        /// </summary>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public bool OwnsView(IView view)
        {
            return SafeExecute(() => this.view == view);
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        /// <param name="layer">层级</param>
        void SetLayer(int layer)
        {
            this.layer = layer;
            if (this.view != null)
            {
                this.view.Layer = layer;
            }
        }

        /// <summary>
        /// 设置顺序
        /// </summary>
        /// <param name="order">顺序</param>
        void SetOrder(int order)
        {
            this.order = order;
            if (this.view != null)
            {
                this.view.Order = order;
            }
        }

        /// <summary>
        /// 设置可交互性
        /// </summary>
        /// <param name="interactable">是否可交互</param>
        void SetInteractable(bool interactable)
        {
            this.interactable = interactable;
            if (this.view != null)
            {
                this.view.Interactable = interactable;
            }
        }

        #region override and overload
        public override string ToString()
        {
            return !State.HasFlag(UIEntityState.Alive) ? "null"
                : $"UIEntity Name:{Name} Layer:{Layer} Order:{Order} ViewModel:{viewModel} ViewBahavior:{presenter} View:{view} Context:{bindingContext.GetType().ToString()}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UIEntity);
        }

        static bool IsNull(UIEntity entity)
        {
            return ReferenceEquals(entity, null) || !entity.State.HasFlag(UIEntityState.Alive);
        }

        public bool Equals(UIEntity other)
        {
            var self = IsNull(this) ? null : this;
            other = IsNull(other) ? null : other;

            return ReferenceEquals(self, other);
        }

        public override int GetHashCode()
        {
            // 如果未存活，返回固定的哈希值 0
            return !State.HasFlag(UIEntityState.Alive) ? 0 : HashCode.Combine(Name, viewModel, view, bindingContext);
        }

        public static bool operator ==(UIEntity left, UIEntity right)
        {
            left = IsNull(left) ? null : left;
            right = IsNull(right) ? null : right;

            return ReferenceEquals(left, right);
        }

        public static bool operator !=(UIEntity left, UIEntity right)
        {
            return !(left == right);
        }
        #endregion
    }
}