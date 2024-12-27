using FUI.Bindable;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    public partial class UIEntity
    {
        /// <summary>
        /// ����һ��UIʵ��
        /// </summary>
        /// <param name="bindingContext">�󶨵�������</param>
        /// <param name="behavior">��Ϊ</param>
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
        /// ����һ��UIʵ��
        /// </summary>
        /// <param name="viewName">��ͼ��</param>
        /// <param name="viewFactory">��ͼ����</param>
        /// <param name="viewModelType">��ͼģ������</param>
        /// <param name="viewBehaviorType">��ͼ��Ϊ����</param>
        /// <returns></returns>
        public static UIEntity Create(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type viewBehaviorType = null)
        {
            var view = viewFactory.Create(viewName);
            return Create(view, viewModelType, viewBehaviorType);
        }

        /// <summary>
        /// �첽����һ��UIʵ��
        /// </summary>
        /// <param name="viewName">��ͼ��</param>
        /// <param name="viewFactory">��ͼ����</param>
        /// <param name="viewModelType">��ͼģ������</param>
        /// <param name="viewBehaviorType">��ͼ��Ϊ����</param>
        /// <param name="token">ȡ�����</param>
        /// <returns></returns>
        public static async Task<UIEntity> CreateAsync(string viewName, IViewFactory viewFactory, Type viewModelType = null, Type viewBehaviorType = null, CancellationToken token = default)
        {
            var view = await viewFactory.CreateAsync(viewName, token);
            return Create(view, viewModelType, viewBehaviorType);
        }

        /// <summary>
        /// ����һ��UIʵ��
        /// </summary>
        /// <param name="view">��ͼ</param>
        /// <param name="viewModelType">��ͼģ������</param>
        /// <param name="viewBehaviorType">��ͼ��Ϊ����</param>
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
            //���û��ָ����ͼ��Ϊ��û��Ĭ�ϵ���ͼ��Ϊ, ��ʹ��EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior<ObservableObject>;
            return Create(bindingContext, behavior);
        }

        /// <summary>
        /// ����һ��UIʵ��
        /// </summary>
        /// <param name="view">��ͼ</param>
        /// <param name="viewModel">��ͼģ��</param>
        /// <returns></returns>
        public static UIEntity Create(IView view, ObservableObject viewModel)
        {
            var viewModelType = viewModel.GetType();
            BindingContextTypeResolver.TryGetContextType(viewModelType, view.Name, out var contextType, out _);
            BindingContextTypeResolver.TryGetBehaviorType(null, viewModelType, out var behaviorType);
            //���û��ָ����ͼ��Ϊ��û��Ĭ�ϵ���ͼ��Ϊ, ��ʹ��EmptyViewBehavior
            var behavior = behaviorType == null ? new EmptyViewBehavior() : Activator.CreateInstance(behaviorType) as IViewBehavior<ObservableObject>;
            var bindingContext = Activator.CreateInstance(contextType, view, viewModel) as BindingContext;
            return Create(bindingContext, behavior);
        }
    }
}