using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 绑定上下文
    /// </summary>
    public abstract class BindingContext<TObservableObject> : IBindingContext<TObservableObject> where TObservableObject : ObservableObject
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
        /// 这个绑定上下文的视图
        /// </summary>
        IView IBindingContext<TObservableObject>.View => View;

        /// <summary>
        /// 这个绑定上下文的视图模型
        /// </summary>
        TObservableObject IBindingContext<TObservableObject>.ViewModel => ViewModel;

        /// <summary>
        /// 这个绑定上下文的视图模型类型
        /// </summary>
        Type IBindingContext<TObservableObject>.ViewModelType => typeof(TObservableObject);

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
        /// 更新视图
        /// </summary>
        /// <param name="view">要更新的视图</param>
        /// <exception cref="System.Exception">当视图类型和当前不匹配的时候</exception>
        bool IBindingContext<TObservableObject>.UpdateView(IView view)
        {
            if (this.View == view)
            {
                return false;
            }

            if (this.View.GetType() != view.GetType())
            {
                return false;
            }

            var context = this as IBindingContext<TObservableObject>;
            context.Unbinding();
            View = view;
            context.Binding();
            return true;
        }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="viewModel">视图模型</param>
        bool IBindingContext<TObservableObject>.UpdateViewModel(ObservableObject viewModel)
        {
            if (this.ViewModel == viewModel)
            {
                return false;
            }

            if(!(viewModel is TObservableObject))
            {
                return false;
            }

            var context = this as IBindingContext<TObservableObject>;
            context.Unbinding();
            ViewModel = viewModel as TObservableObject;
            context.Binding();
            return true;
        }

        /// <summary>
        /// 绑定视图和视图模型
        /// </summary>
        void IBindingContext<TObservableObject>.Binding()
        {
            ViewModel.PropertyChanged += OnPropertyChanged;
            OnBinding();
        }

        /// <summary>
        /// 解绑视图和视图模型
        /// </summary>
        void IBindingContext<TObservableObject>.Unbinding()
        {
            ViewModel.PropertyChanged -= OnPropertyChanged;
            OnUnbinding();
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
    }
}