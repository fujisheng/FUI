using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 绑定上下文
    /// </summary>
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
        /// 当解绑的时候
        /// </summary>
        protected virtual void OnUnbinding() { }

        /// <summary>
        /// 是否已经绑定 防止重复绑定
        /// </summary>
        bool binded;

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

            if(!(viewModel is TObservableObject))
            {
                return false;
            }

            var context = this as IBindingContext;
            context.Unbinding();
            ViewModel = viewModel as TObservableObject;
            context.Binding();
            return true;
        }

        /// <summary>
        /// 绑定视图和视图模型
        /// </summary>
        void IBindingContext.Binding()
        {
            if (binded)
            {
                return;
            }

            if(this.ViewModel == null || this.View == null)
            {
                return;
            }

            ViewModel.PropertyChanged += OnPropertyChanged;
            OnBinding();
            binded = true;
        }

        /// <summary>
        /// 解绑视图和视图模型
        /// </summary>
        void IBindingContext.Unbinding()
        {
            if(!binded)
            {
                return;
            }

            if (this.ViewModel == null || this.View == null)
            {
                return;
            }

            ViewModel.PropertyChanged -= OnPropertyChanged;
            OnUnbinding();
            binded = false;
        }
        #endregion
    }
}