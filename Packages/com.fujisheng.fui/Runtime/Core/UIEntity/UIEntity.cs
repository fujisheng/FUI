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
            private set => SafeExecute(() => viewModel = value);
        }

        /// <summary>
        /// 上下文类型
        /// </summary>
        Type contextType;

        /// <summary>
        /// 绑定上下文类型
        /// </summary>
        public Type BindingContextType
        {
            get => SafeExecute(() => contextType);
            private set =>SafeExecute(() => contextType = value);
        }

        /// <summary>
        /// 视图行为
        /// </summary>
        IViewBehavior behavior;

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
        /// <param name="behavior">行为</param>
        UIEntity(IBindingContext bindingContext, IViewBehavior behavior)
        {
            this.bindingContext = bindingContext;
            this.view = bindingContext.View;
            this.behavior = behavior;

            State |= UIEntityState.Alive;

            this.ViewModel = bindingContext.ViewModel;
            this.BindingContextType = bindingContext.GetType();

            this.Name = view.Name;
            this.Layer = view.Layer;
            this.Order = view.Order;

            //当创建这个UI实体的时候
            this.behavior.OnCreate(ViewModel);
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
        /// 更新视图模型，当前视图和视图行为不变 允许更新成子类
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        public void UpdateViewModel(ObservableObject viewModel)
        {
            SafeExecute(() =>
            {
                if (this.ViewModel == viewModel || viewModel == null)
                {
                    return;
                }

                if (!this.bindingContext.UpdateViewModel(viewModel))
                {
                    throw new Exception($"{this.GetType()}  UpdateViewModel Error {viewModel} not {this.ViewModel.GetType()}");
                }

                this.behavior.UpdateViewModel(viewModel);
                SynchronizeProperties();
            });
        }

        /// <summary>
        /// 更新视图行为，当前视图和视图模型不变
        /// </summary>
        /// <param name="behavior"></param>
        public void UpdateBehavior<TBehavior>()
        {
            SafeExecute(() =>
            {
                if (!typeof(IViewBehavior).IsAssignableFrom(typeof(TBehavior)))
                {
                    throw new Exception($"{this}  UpdateBehavior Error {typeof(TBehavior)} not IViewBehavior");
                }

                this.behavior = Activator.CreateInstance(typeof(TBehavior)) as IViewBehavior;
                this.behavior.UpdateViewModel(ViewModel);
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
                if (this.ViewModel is ISynchronizeProperties synchronizeProperties)
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
            SafeExecute(() =>
            {
                if (State.HasFlag(UIEntityState.Freezed) || State.HasFlag(UIEntityState.Enabled))
                {
                    return;
                }

                this.bindingContext.Binding();
                SynchronizeProperties();
                this.behavior.OnEnable(param);
                this.view.Visible = true;

                State |= UIEntityState.Enabled;
                OnEntityEnabled?.Invoke(this);
            });
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

                this.behavior.OnDisable();
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

                this.behavior.OnFocus();
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

                this.behavior.OnUnfocus();
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
                    this.behavior.OnDisable();
                }

                this.behavior.OnDestroy();
                this.bindingContext?.Unbinding();
                this.view?.Destroy();

                OnEntityDestoryed?.Invoke(this);

                this.bindingContext = null;
                this.behavior = null;
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
                    throw new Exception($"Unfreeze failed, factory:{factory} cannot create viewName: {Name} viewModel:{this.ViewModel}");
                }

                //恢复上下文
                this.bindingContext = Activator.CreateInstance(contextType, view, ViewModel) as IBindingContext;
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

        #region override and overload
        public override string ToString()
        {
            return !State.HasFlag(UIEntityState.Alive) ? "null"
                : $"UIEntity Name:{Name} ViewModel:{ViewModel} ViewBahavior:{behavior} View:{view} Context:{bindingContext.GetType().ToString()}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UIEntity);
        }

        public bool Equals(UIEntity other)
        {
            if (ReferenceEquals(null, other))
            {
                return !State.HasFlag(UIEntityState.Alive);
            }

            if(ReferenceEquals(this, other))
            {
                return true;
            }

            return State.HasFlag(UIEntityState.Alive) && other.State.HasFlag(UIEntityState.Alive);
        }

        public override int GetHashCode()
        {
            return !State.HasFlag(UIEntityState.Alive) ? 0 : HashCode.Combine(Name, ViewModel, view, bindingContext);
        }

        public static bool operator ==(UIEntity left, UIEntity right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return !right?.State.HasFlag(UIEntityState.Alive) ?? true;
            }

            return left.Equals(right);
        }

        public static bool operator !=(UIEntity left, UIEntity right)
        {
            return !(left == right);
        }
        #endregion
    }
}