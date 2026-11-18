using FUI.Binding;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;

using System;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Infrastructure
{
    /// <summary>
    /// UI实例 对应一个视图
    /// </summary>
    internal partial class ViewInstance : IEquatable<ViewInstance>
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
        internal ObservableObject ViewModel
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

        internal IPresenter Presenter => SafeExecute(() => presenter);

        /// <summary>
        /// 展示器
        /// </summary>
        IPresenter presenter;
        // OnOpen 已成功、Enabled 尚未发布时，也已取得必须配对 OnClose 的所有权。
        bool pendingOpenClose;

        /// <summary>
        /// 实例的名字
        /// </summary>
        string name;

        /// <summary>
        /// 这个实例的名字
        /// </summary>
        internal string Name
        {
            get => SafeExecute(() => name);
            private set => SafeExecute(() => name = value);
        }

        int layer;
        /// <summary>
        /// 层级
        /// </summary>
        internal int Layer
        {
            get => SafeExecute(() => layer);
            set => SafeExecute(() => SetLayer(value));
        }

        int order;
        /// <summary>
        /// 顺序
        /// </summary>
        internal int Order
        {
            get => SafeExecute(() => order);
            set => SafeExecute(() => SetOrder(value));
        }

        bool interactable = true;
        /// <summary>
        /// 可交互性
        /// </summary>
        internal bool Interactable
        {
            get => SafeExecute(() => interactable);
            set => SafeExecute(() => SetInteractable(value));
        }

        /// <summary>
        /// 状态
        /// </summary>
        internal ViewInstanceState State { get; private set; }

        /// <summary>
        /// 私有化构造函数  防止外部创建
        /// </summary>
        ViewInstance() { }

        /// <summary>
        /// 创建一个UI实例
        /// </summary>
        /// <param name="bindingContext">绑定上下文</param>
        /// <param name="presenter">展示器</param>
        ViewInstance(IBindingContext bindingContext, IPresenter presenter)
        {
            this.bindingContext = bindingContext;
            this.view = bindingContext.View;
            this.presenter = presenter;

            State |= ViewInstanceState.Alive;

            this.viewModel = bindingContext.ViewModel;
            this.Name = view.Name;
            this.Layer = view.Layer;
            this.Order = view.Order;

            //当创建这个UI实例的时候
            this.presenter.OnCreate(viewModel);
            Created?.Invoke(this);
        }

        #region safe execute
        /// <summary>
        /// 确保这个实例还没有被销毁
        /// </summary>
        /// <exception cref="Exception">如果已经被销毁了但是却还在访问则抛出异常</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureNotDestroyed()
        {
            if (!State.HasFlag(ViewInstanceState.Alive))
            {
                throw new Exception($"ViewInstance({name}) has been destroyed, but you're still trying to access it.");
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
        internal void UpdateViewModel(ObservableObject viewModel)
        {
            SafeExecute(() =>
            {
                if (this.viewModel == viewModel || viewModel == null)
                {
                    return;
                }

                var previous = this.viewModel;
                if (!this.bindingContext.UpdateViewModel(viewModel))
                {
                    throw new Exception($"{this.GetType()}  UpdateViewModel Error {viewModel} not {this.viewModel.GetType()}");
                }

                try
                {
                    if (!this.presenter.UpdateViewModel(viewModel))
                    {
                        throw new InvalidOperationException($"Presenter {presenter.GetType()} cannot bind ViewModel {viewModel.GetType()}.");
                    }
                }
                catch (Exception presenterException)
                {
                    try
                    {
                        this.bindingContext.UpdateViewModel(previous);
                        this.presenter.UpdateViewModel(previous);
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException("Failed to update the ViewModel and roll back the ViewInstance.", presenterException, rollbackException);
                    }

                    throw;
                }

                this.viewModel = viewModel;
                SynchronizeProperties();
            });
        }

        /// <summary>
        /// 更新展示器，当前视图和视图模型不变
        /// </summary>
        /// <param name="behavior"></param>
        internal void UpdatePresenter<TPresenter>() where TPresenter : class, new()
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(ViewInstanceState.Enabled))
                {
                    throw new InvalidOperationException("A Presenter cannot be replaced while the ViewInstance is enabled. Disable it first.");
                }

                var nextPresenter = new TPresenter() as IPresenter;
                if (nextPresenter == null)
                {
                    throw new Exception($"{this}  UpdateBehavior Error {typeof(TPresenter)} not IPresenter");
                }

                // 新 Presenter 完整创建成功后再销毁旧实例，避免构造失败丢失原 Presenter。
                nextPresenter.OnCreate(viewModel);
                try
                {
                    this.presenter.OnDestroy();
                }
                catch (Exception exception)
                {
                    // 旧 Presenter 销毁失败时，新实例还未接管 ViewInstance，必须同步回收。
                    try
                    {
                        nextPresenter.OnDestroy();
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException("Failed to replace the Presenter and roll back the new Presenter.", exception, rollbackException);
                    }

                    throw;
                }
                this.presenter = nextPresenter;
                SynchronizeProperties();
            });
        }

        /// <summary>
        /// 同步所有属性值
        /// </summary>
        internal void SynchronizeProperties()
        {
            SafeExecute(() =>
            {
                // 只初始化当前投影，避免共享 ViewModel 的其他 View 被重复全量刷新。
                this.bindingContext.Synchronize();
            });
        }

        /// <summary>
        /// 激活这个UI实例
        /// </summary>
        internal void Enable(object param = null)
        {
            SafeExecute(() =>
            {
                if (State.HasFlag(ViewInstanceState.Enabled))
                {
                    return;
                }

                try
                {
                    // Binding、属性同步和 OnOpen 共同构成一次启用事务，全部成功后才对外标记 Enabled。
                    using ((view as IViewUpdate)?.BeginUpdate())
                    {
                        this.bindingContext.Binding();
                        SynchronizeProperties();
                        this.presenter.OnOpen(param);
                        if (!State.HasFlag(ViewInstanceState.Alive))
                        {
                            throw new OperationCanceledException("The View was destroyed during OnOpen; activation was cancelled.");
                        }
                        pendingOpenClose = true;
                        this.view.Visible = true;
                    }
                    if (!State.HasFlag(ViewInstanceState.Alive))
                    {
                        throw new OperationCanceledException("The View was destroyed during OnOpen; activation was cancelled.");
                    }
                    SetExternalValueSynchronization(true);

                    State |= ViewInstanceState.Enabled;
                    pendingOpenClose = false;
                    Enabled?.Invoke(this);
                }
                catch (Exception openingFailure)
                {
                    // 打开失败必须恢复不可见、未绑定状态，再把异常交给 Navigator 统一回滚。
                    Exception cleanupFailure = null;
                    if (State.HasFlag(ViewInstanceState.Alive))
                    {
                        var closeOpened = pendingOpenClose || State.HasFlag(ViewInstanceState.Enabled);
                        pendingOpenClose = false;
                        State &= ~ViewInstanceState.Enabled;
                        try { SetExternalValueSynchronization(false); }
                        catch (Exception exception) { cleanupFailure = exception; }
                        // OnOpen 成功后，最终几何提交仍可能失败；必须归还本次打开取得的资源。
                        if (closeOpened && State.HasFlag(ViewInstanceState.Alive))
                        {
                            try { presenter.OnClose(); }
                            catch (Exception exception) { cleanupFailure ??= exception; }
                        }
                        if (State.HasFlag(ViewInstanceState.Alive))
                        {
                            try { view.Visible = false; }
                            catch (Exception exception) { cleanupFailure ??= exception; }
                        }
                        if (State.HasFlag(ViewInstanceState.Alive))
                        {
                            try { bindingContext.Unbinding(); }
                            catch (Exception exception) { cleanupFailure ??= exception; }
                        }
                    }
                    if (cleanupFailure != null) throw new AggregateException(openingFailure, cleanupFailure);
                    throw;
                }
            });
        }

        /// <summary>
        /// 使这个UI实例无效
        /// </summary>
        internal void Disable()
        {
            SafeExecute(() =>
            {
                if (!State.HasFlag(ViewInstanceState.Enabled))
                {
                    return;
                }

                Exception failure = null;
                try
                {
                    this.presenter.OnClose();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    // 生命周期异常不能阻止解绑和隐藏，最终由调用方记录原始失败。
                    SetExternalValueSynchronization(false);
                    this.view.Visible = false;
                    try
                    {
                        this.bindingContext.Unbinding();
                    }
                    catch (Exception exception)
                    {
                        failure ??= exception;
                    }

                    State &= ~ViewInstanceState.Enabled;
                    Disabled?.Invoke(this);
                }

                if (failure != null)
                {
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            });
        }

        internal async ValueTask DisableAsync(CancellationToken cancellationToken)
        {
            Exception failure = null;
            try
            {
                await presenter.OnCloseAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            try
            {
                Disable();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        internal void Covered(bool hide)
        {
            SafeExecute(() =>
            {
                // Covered 只暂停焦点与外部值同步，不关闭实例，也不解除业务事件。
                presenter.OnUnfocus();
                presenter.OnCovered();
                SetExternalValueSynchronization(false);
                if (hide)
                {
                    view.Visible = false;
                }
            });
        }

        internal void Revealed(bool wasHidden)
        {
            SafeExecute(() =>
            {
                // Revealed 恢复原实例，不重复执行 Binding 或 OnOpen。
                if (wasHidden)
                {
                    view.Visible = true;
                }

                presenter.OnRevealed();
                (view as IViewInputFocus)?.FocusInput();
                presenter.OnFocus();
                SetExternalValueSynchronization(true);
            });
        }

        /// <summary>
        /// 聚焦这个UI实例
        /// </summary>
        internal void Focus()
        {
            SafeExecute(() =>
            {
                (view as IViewInputFocus)?.FocusInput();
                this.presenter.OnFocus();
                Focused?.Invoke(this);
            });
        }

        /// <summary>
        /// 失焦这个UI实例
        /// </summary>
        internal void Unfocus()
        {
            SafeExecute(() =>
            {
                this.presenter.OnUnfocus();
                Unfocused?.Invoke(this);
            });
        }

        /// <summary>
        /// 销毁这个UI实例
        /// </summary>
        internal void Destroy()
        {
            DestroyCore(true);
        }

        /// <summary>
        /// 只销毁当前投影的绑定与 Presenter，保留由父 Element 持有的静态 View。
        /// </summary>
        internal void DestroyProjection()
        {
            DestroyCore(false);
        }

        void DestroyCore(bool destroyView)
        {
            if (!State.HasFlag(ViewInstanceState.Alive))
            {
                return;
            }

            // 销毁是幂等终态；各阶段继续清理，同时保留第一个异常供上层诊断。
            Exception failure = null;
            SetExternalValueSynchronization(false);
            if (State.HasFlag(ViewInstanceState.Enabled))
            {
                try
                {
                    Disable();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }
            else if (pendingOpenClose)
            {
                pendingOpenClose = false;
                try { presenter.OnClose(); }
                catch (Exception exception) { failure = exception; }
            }

            try
            {
                this.presenter.OnDestroy();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            try
            {
                this.bindingContext?.Unbinding();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            try
            {
                if (destroyView)
                {
                    this.view?.Destroy();
                }
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            Destroyed?.Invoke(this);
            this.bindingContext = null;
            this.presenter = null;
            this.view = null;
            State = 0;

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        void SetExternalValueSynchronization(bool enabled)
        {
            if (view is IExternalValueSynchronizationHost host)
            {
                host.ExternalValueSynchronizationEnabled = enabled;
            }
        }

        /// <summary>
        /// 是否是这个视图
        /// </summary>
        /// <param name="view">视图</param>
        /// <returns></returns>
        internal bool OwnsView(IView view)
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
            return !State.HasFlag(ViewInstanceState.Alive) ? "null"
                : $"ViewInstance Name:{Name} Layer:{Layer} Order:{Order} ViewModel:{viewModel} ViewBahavior:{presenter} View:{view} Context:{bindingContext.GetType().ToString()}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ViewInstance);
        }

        static bool IsNull(ViewInstance instance)
        {
            return ReferenceEquals(instance, null) || !instance.State.HasFlag(ViewInstanceState.Alive);
        }

        public bool Equals(ViewInstance other)
        {
            var self = IsNull(this) ? null : this;
            other = IsNull(other) ? null : other;

            return ReferenceEquals(self, other);
        }

        public override int GetHashCode()
        {
            // 如果未存活，返回固定的哈希值 0
            return !State.HasFlag(ViewInstanceState.Alive) ? 0 : HashCode.Combine(Name, viewModel, view, bindingContext);
        }

        public static bool operator ==(ViewInstance left, ViewInstance right)
        {
            left = IsNull(left) ? null : left;
            right = IsNull(right) ? null : right;

            return ReferenceEquals(left, right);
        }

        public static bool operator !=(ViewInstance left, ViewInstance right)
        {
            return !(left == right);
        }
        #endregion
    }
}
