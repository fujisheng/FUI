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
        /// <param name="viewBehaviorType">视图行为类型</param>
        /// <returns></returns>
        public static UIEntity Create(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type viewBehaviorType = null)
        {
            var view = viewFactory.Create(viewName);
            return Create(view, viewModelType, viewBehaviorType);
        }

        /// <summary>
        /// 异步创建一个UI实体
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <param name="viewFactory">视图工厂</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="viewBehaviorType">视图行为类型</param>
        /// <param name="token">取消标记</param>
        /// <returns></returns>
        public static async Task<UIEntity> CreateAsync(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type viewBehaviorType = null, CancellationToken token = default)
        {
            var view = await viewFactory.CreateAsync(viewName, token);
            return Create(view, viewModelType, viewBehaviorType);
        }

        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="viewBehaviorType">视图行为类型</param>
        /// <returns></returns>
        public static UIEntity Create(IView view, Type viewModelType = null, Type viewBehaviorType = null)
        {
            if (view == null)
            {
                return null;
            }
            if (!BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out var contextType, out var resultViewModelType))
            {
                return null;
            }

            var viewModel = Activator.CreateInstance(resultViewModelType) as ObservableObject;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModel) as IBindingContext;
            BindingContextTypeResolver.TryGetBehaviorType(viewBehaviorType, resultViewModelType, out var behaviorType);
            //如果没有指定视图行为且没有默认的视图行为, 则使用EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior;
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
            Type viewModelType, behaviorType, contextType;

            //如果没有ViewModel, 则使用EmptyViewModel和EmptyBindingContext 适用于没有任何数据的情况 例如只是创建一个空的UI
            if (viewModel == null)
            {
                viewModelType = typeof(EmptyViewModel);
                contextType = typeof(EmptyBindingContext);
                behaviorType = typeof(EmptyViewBehavior);
                viewModelInstance = new EmptyViewModel();
                UnityEngine.Debug.LogWarning($"Create UIEntity {view} ViewModel is null, using EmptyViewModel and EmptyBindingContext");
            }
            else
            {
                viewModelType = viewModel.GetType();
                BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out contextType, out _);
                BindingContextTypeResolver.TryGetBehaviorType(null, viewModelType, out behaviorType);
            }

            //如果没有指定视图行为且没有默认的视图行为, 则使用EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModelInstance) as IBindingContext;
            return new UIEntity(bindingContext, behavior);
        }
    }
}