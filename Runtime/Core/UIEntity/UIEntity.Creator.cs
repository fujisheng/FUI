using FUI.Bindable;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    public partial class UIEntity
    {
        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <param name="viewFactory">视图工厂</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="presenterType">展示器类型</param>
        /// <returns></returns>
        public static UIEntity Create(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type presenterType = null)
        {
            var view = viewFactory.Create(viewName);
            return Create(view, viewModelType, presenterType);
        }

        /// <summary>
        /// 异步创建一个UI实体
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <param name="viewFactory">视图工厂</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="presenterType">展示器类型</param>
        /// <param name="token">取消标记</param>
        /// <returns></returns>
        public static async Task<UIEntity> CreateAsync(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type presenterType = null, CancellationToken token = default)
        {
            var view = await viewFactory.CreateAsync(viewName, token);
            return Create(view, viewModelType, presenterType);
        }

        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="presenterType">展示器类型</param>
        /// <returns></returns>
        public static UIEntity Create(IView view, Type viewModelType = null, Type presenterType = null)
        {
            if (view == null)
            {
                return null;
            }
            if (!BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out var contextType, out var resultViewModelType))
            {
                contextType = typeof(EmptyBindingContext);
                resultViewModelType = typeof(EmptyViewModel);
                Logger.Instance.LogError($"Create UIEntity {view} ViewModelType {viewModelType} is invalid, using EmptyViewModel and EmptyBindingContext");
            }

            view.Initialize();
            var viewModel = Activator.CreateInstance(resultViewModelType) as ObservableObject;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModel) as IBindingContext;
            BindingContextTypeResolver.TryGetPresenterType(presenterType, resultViewModelType, out var behaviorType);
            //如果没有指定展示器且没有默认的展示器, 则使用EmptyPresenter
            var behavior = behaviorType == null ? new EmptyPresenter() : Activator.CreateInstance(behaviorType) as IPresenter;
            return new UIEntity(bindingContext, behavior);
        }

        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="viewModel">视图模型</param>
        /// <returns></returns>
        public static UIEntity Create(IView view, ObservableObject viewModel)
        {
            ObservableObject viewModelInstance = viewModel;
            Type viewModelType, presenterType, contextType;

            //如果没有ViewModel, 则使用EmptyViewModel和EmptyBindingContext 适用于没有任何数据的情况 例如只是创建一个空的UI
            if (viewModel == null)
            {
                viewModelType = typeof(EmptyViewModel);
                contextType = typeof(EmptyBindingContext);
                presenterType = typeof(EmptyPresenter);
                viewModelInstance = new EmptyViewModel();
                Logger.Instance.LogWarning($"Create UIEntity {view} ViewModel is null, using EmptyViewModel and EmptyBindingContext");
            }
            else
            {
                viewModelType = viewModel.GetType();
                if(!BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out contextType, out _))
                {
                    contextType = typeof(EmptyBindingContext);
                    viewModelType = typeof(EmptyViewModel);
                    viewModelInstance = new EmptyViewModel();
                    Logger.Instance.LogError($"Create UIEntity {view} ViewModelType {viewModelType} is invalid, using EmptyViewModel and EmptyBindingContext");
                    presenterType = typeof(EmptyPresenter);
                }
                else
                {
                    BindingContextTypeResolver.TryGetPresenterType(null, viewModelType, out presenterType);
                }
            }

            view.Initialize();
            //如果没有指定展示器且没有默认的展示器, 则使用EmptyPresenter
            var behavior = presenterType == null ? new EmptyPresenter() : Activator.CreateInstance(presenterType) as IPresenter;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModelInstance) as IBindingContext;
            return new UIEntity(bindingContext, behavior);
        }
    }
}