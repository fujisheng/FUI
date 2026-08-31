using System;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Presentation;

namespace FUI.Rendering
{
    /// <summary>
    /// 为自定义 Element 和原型视图创建受控嵌套投影。
    /// </summary>
    public static class Projection
    {
        public static ProjectionHandle<TViewModel> Create<TViewModel>(
            IView view,
            TViewModel viewModel)
            where TViewModel : ObservableObject
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            return new ProjectionHandle<TViewModel>(
                ViewInstance.Create(view, viewModel),
                instance => instance.Destroy());
        }
    }
}
