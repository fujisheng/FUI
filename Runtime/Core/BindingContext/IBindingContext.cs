using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// 绑定上下文
    /// </summary>
    internal interface IBindingContext
    {
        /// <summary>
        /// 这个绑定上下文所对应的视图
        /// </summary>
        internal IView View { get; }

        /// <summary>
        /// 这个绑定上下文所对应的视图模型类型
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// 这个绑定上下文所对应的视图模型
        /// </summary>
        internal ObservableObject ViewModel { get; }

        /// <summary>
        /// 更新View
        /// </summary>
        /// <param name="view">更新后的view</param>
        /// <returns></returns>
        internal bool UpdateView(IView view);

        /// <summary>
        /// 更新ViewModel 可以更新为原始ViewModel的子类
        /// </summary>
        /// <param name="viewModel">更新后的ViewModel</param>
        /// <returns></returns>
        internal bool UpdateViewModel(ObservableObject viewModel);

        /// <summary>
        /// 绑定
        /// </summary>
        internal void Binding();

        /// <summary>
        /// 解绑
        /// </summary>
        internal void Unbinding();
    }
}