﻿using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// 绑定上下文
    /// </summary>
    public abstract class BindingContext
    {
        /// <summary>
        /// 视图
        /// </summary>
        public IView View { get; private set; }

        /// <summary>
        /// 视图模型
        /// </summary>
        public ObservableObject ViewModel { get; private set; }

        /// <summary>
        /// 构建一个绑定上下文
        /// </summary>
        /// <param name="view"></param>
        /// <param name="viewModel"></param>
        protected BindingContext(IView view, ObservableObject viewModel)
        {
            View = view;
            ViewModel = viewModel; 
        }

        /// <summary>
        /// 更新视图
        /// </summary>
        /// <param name="view">要更新的视图</param>
        public void UpdateView(IView view)
        {
            if(this.View == view)
            {
                return;
            }

            if(this.View.GetType() != view.GetType())
            {
                throw new System.Exception($"{this.GetType()} RebindingView Error {view} not {this.View.GetType()}");
            }

            InternalUnbinding();
            View = view;
            InternalBinding();
        }

        /// <summary>
        /// 更新视图模型
        /// </summary>
        /// <param name="viewModel">要更新的视图模型</param>
        public void UpdateViewModel(ObservableObject viewModel)
        {
            if(this.ViewModel == viewModel)
            {
                return;
            }

            if(this.ViewModel.GetType() != ViewModel.GetType())
            {
                throw new System.Exception($"{this.GetType()}  RebindingViewModel Error {viewModel} not {this.ViewModel.GetType()}");
            }

            InternalUnbinding();
            ViewModel = viewModel;
            InternalBinding();
        }

        public void InternalBinding()
        {
            ViewModel.PropertyChanged += OnPropertyChanged;
            Binding();
        }

        public void InternalUnbinding()
        {
            ViewModel.PropertyChanged -= OnPropertyChanged;
            Unbinding();
        }

        protected virtual void OnPropertyChanged(object sender, string propertyName) { }

        protected virtual void Binding() { }

        protected virtual void Unbinding() { }
    }
}