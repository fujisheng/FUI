using System;
using System.ComponentModel;
using FUI.Binding;
using FUI.Presentation;
using FUI.Rendering;

namespace FUI.Infrastructure
{
    /// <summary>
    /// 生成代码使用的 Route 构造入口。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GeneratedRouteFactory
    {
        public static FUI.Navigation.Route<TViewModel> Create<TViewModel>(
            string assetKey,
            Type presenterType,
            Func<TViewModel> viewModelFactory,
            Func<IView, TViewModel, object> bindingContextFactory,
            Func<object> presenterFactory,
            FUI.Navigation.RoutePolicy policy,
            bool supportsSynchronousOpen = true)
            where TViewModel : ObservableObject
        {
            return new FUI.Navigation.Route<TViewModel>(
                assetKey,
                presenterType,
                viewModelFactory,
                bindingContextFactory,
                presenterFactory,
                policy,
                supportsSynchronousOpen);
        }
    }
}
