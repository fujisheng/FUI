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
        /// <param name="bindingContext">绑定的上下文</param>
        /// <param name="behavior">行为</param>
        /// <returns></returns>
        static UIEntity Create(BindingContext bindingContext, IViewBehavior<ObservableObject> behavior)
        {
            var entity = new UIEntity
            {
                bindingContext = bindingContext,
                View = bindingContext.View,
                ViewModel = bindingContext.ViewModel,
                Behavior = behavior,
                Name = bindingContext.View.Name,
                viewType = bindingContext.View.GetType(),
            };

            entity.OnCreated();
            return entity;
        }

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
            var bindingContext = Activator.CreateInstance(contextType, view, viewModel) as BindingContext;
            BindingContextTypeResolver.TryGetBehaviorType(viewBehaviorType, resultViewModelType, out var behaviorType);
            //如果没有指定视图行为且没有默认的视图行为, 则使用EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior<ObservableObject>;
            return Create(bindingContext, behavior);
        }

        /// <summary>
        /// 创建一个UI实体
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="viewModel">视图模型</param>
        /// <returns></returns>
        public static UIEntity Create(IView view, ObservableObject viewModel)
        {
            var viewModelType = viewModel.GetType();
            BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out var contextType, out _);
            BindingContextTypeResolver.TryGetBehaviorType(null, viewModelType, out var behaviorType);
            //如果没有指定视图行为且没有默认的视图行为, 则使用EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior<ObservableObject>;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModel) as BindingContext;
            return Create(bindingContext, behavior);
        }
    }
}