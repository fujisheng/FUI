using System;
using System.ComponentModel;
using FUI.Binding;
using FUI.Presentation;
using FUI.Rendering;

namespace FUI.Infrastructure
{
    /// <summary>
    /// 生成代码使用的绑定注册入口。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GeneratedBindingRegistry
    {
        public static void Register<TViewModel>(
            Func<IView, TViewModel, object> bindingContextFactory,
            Func<object> presenterFactory)
            where TViewModel : ObservableObject
        {
            if (bindingContextFactory == null)
            {
                throw new ArgumentNullException(nameof(bindingContextFactory));
            }

            if (presenterFactory == null)
            {
                throw new ArgumentNullException(nameof(presenterFactory));
            }

            ViewInstance.RegisterGeneratedFactory(
                typeof(TViewModel),
                (view, viewModel) =>
                {
                    ViewInstance.PrepareView(view);
                    return ViewInstance.CreateGenerated(
                        bindingContextFactory(view, (TViewModel)viewModel),
                        presenterFactory());
                });
        }
    }
}
