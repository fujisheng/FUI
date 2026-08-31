using FUI.Binding;
using FUI.Diagnostics;
using FUI.Presentation;
using FUI.Rendering;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Infrastructure.Binding
{
    /// <summary>
    /// 绑定上下文
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract class BindingContext<TObservableObject> : IBindingContext where TObservableObject : ObservableObject
    {
        /// <summary>
        /// 视图
        /// </summary>
        protected IView View { get; private set; }

        /// <summary>
        /// 视图模型
        /// </summary>
        protected TObservableObject ViewModel { get; private set; }

        /// <summary>
        /// 构建一个绑定上下文
        /// </summary>
        /// <param name="view"></param>
        /// <param name="viewModel"></param>
        protected BindingContext(IView view, TObservableObject viewModel)
        {
            View = view;
            ViewModel = viewModel; 
        }

        /// <summary>
        /// 当属性变化的时候
        /// </summary>
        /// <param name="sender">发生变化的对象</param>
        /// <param name="propertyName">发生变化的属性名</param>
        protected virtual void OnPropertyChanged(object sender, string propertyName) { }

        /// <summary>
        /// 当绑定的时候
        /// </summary>
        protected virtual void OnBinding() { }

        /// <summary>
        /// 初始化当前 View 使用的属性。生成的 BindingContext 会覆盖此方法并执行局部同步。
        /// </summary>
        protected virtual void OnSynchronize()
        {
            // 手写 BindingContext 保留旧行为；生成代码会覆盖该方法，避免共享数据向其他 View 广播。
            if (ViewModel is ISynchronizeProperties synchronizeProperties)
            {
                synchronizeProperties.Synchronize();
            }
        }

        /// <summary>
        /// 当解绑的时候
        /// </summary>
        protected virtual void OnUnbinding() { }

        /// <summary>
        /// Binding 与 Unbinding 的事务状态，防止重入并保证失败时可以完整回滚。
        /// </summary>
        enum BindingState
        {
            Unbound,
            Binding,
            Bound,
            Unbinding,
        }

        BindingState state;
        CancellationTokenSource commandCancellation;

        protected void RunAsyncCommand(Func<CancellationToken, ValueTask> command)
        {
            if (command == null || state != BindingState.Bound)
            {
                return;
            }

            // Command 生命周期从属于本次 Binding；页面解绑时统一取消仍在运行的异步命令。
            var token = commandCancellation?.Token ?? CancellationToken.None;
            ValueTask operation;
            try
            {
                operation = command(token);
            }
            catch (Exception exception)
            {
                Log.Instance.LogException(exception);
                return;
            }

            _ = ObserveCommandAsync(operation, token);
        }

        static async Task ObserveCommandAsync(ValueTask operation, CancellationToken token)
        {
            try
            {
                await operation;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                Log.Instance.LogException(exception);
            }
        }

        #region internal

        /// <summary>
        /// 这个绑定上下文的视图
        /// </summary>
        IView IBindingContext.View => View;

        /// <summary>
        /// 这个绑定上下文的视图模型
        /// </summary>
        ObservableObject IBindingContext.ViewModel => ViewModel;

        /// <summary>
        /// 这个绑定上下文的视图模型类型
        /// </summary>
        Type IBindingContext.ViewModelType => typeof(TObservableObject);

        bool IBindingContext.IsBound => state == BindingState.Bound;

        /// <summary>
        /// 更新视图
        /// </summary>
        /// <param name="view">要更新的视图</param>
        /// <exception cref="System.Exception">当视图类型和当前不匹配的时候</exception>
        bool IBindingContext.UpdateView(IView view)
        {
            if (this.View == view)
            {
                return false;
            }

            if (this.View.GetType() != view.GetType())
            {
                return false;
            }

            var context = this as IBindingContext;
            context.Unbinding();
            View = view;
            context.Binding();
            return true;
        }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        bool IBindingContext.UpdateViewModel(ObservableObject viewModel)
        {
            if (this.ViewModel == viewModel)
            {
                return false;
            }

            if (!(viewModel is TObservableObject))
            {
                return false;
            }

            var context = this as IBindingContext;
            var previous = ViewModel;
            var wasBound = state == BindingState.Bound;
            context.Unbinding();
            ViewModel = (TObservableObject)viewModel;
            try
            {
                if (wasBound)
                {
                    context.Binding();
                }
                return true;
            }
            catch (Exception bindingException)
            {
                // 换绑失败时恢复旧数据和旧监听，不能让共享投影停留在半绑定状态。
                ViewModel = previous;
                try
                {
                    if (wasBound)
                    {
                        context.Binding();
                    }
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Failed to update the ViewModel and restore the previous bindings.", bindingException, rollbackException);
                }

                throw;
            }
        }

        /// <summary>
        /// 绑定视图和视图模型
        /// </summary>
        void IBindingContext.Binding()
        {
            if (state == BindingState.Bound || state == BindingState.Binding)
            {
                return;
            }

            if (this.ViewModel == null || this.View == null)
            {
                return;
            }

            // 只有 OnBinding 全部成功后才进入 Bound；中途异常必须撤销已经建立的监听。
            state = BindingState.Binding;
            commandCancellation?.Dispose();
            commandCancellation = new CancellationTokenSource();
            ViewModel.PropertyChanged += OnPropertyChanged;
            try
            {
                OnBinding();
                state = BindingState.Bound;
            }
            catch
            {
                try
                {
                    OnUnbinding();
                }
                finally
                {
                    ViewModel.PropertyChanged -= OnPropertyChanged;
                    commandCancellation.Cancel();
                    commandCancellation.Dispose();
                    commandCancellation = null;
                    state = BindingState.Unbound;
                }

                throw;
            }
        }

        void IBindingContext.Synchronize()
        {
            if (state != BindingState.Bound)
            {
                return;
            }

            OnSynchronize();
        }

        /// <summary>
        /// 解绑视图和视图模型
        /// </summary>
        void IBindingContext.Unbinding()
        {
            if (state == BindingState.Unbound || state == BindingState.Unbinding)
            {
                return;
            }

            if (this.ViewModel == null || this.View == null)
            {
                return;
            }

            // finally 保证业务解绑异常不会阻止框架监听和 Command Token 被释放。
            state = BindingState.Unbinding;
            try
            {
                OnUnbinding();
            }
            finally
            {
                ViewModel.PropertyChanged -= OnPropertyChanged;
                commandCancellation?.Cancel();
                commandCancellation?.Dispose();
                commandCancellation = null;
                state = BindingState.Unbound;
            }
        }
        #endregion
    }
}
