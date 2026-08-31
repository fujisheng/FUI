using FUI.Binding;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;

using System;
using System.Collections.Generic;

namespace FUI.Infrastructure
{
    /// <summary>
    /// 提供生成绑定工厂的注册、解析和 ViewInstance 创建入口。
    /// </summary>
    internal partial class ViewInstance
    {
        static readonly Dictionary<Type, Func<IView, ObservableObject, ViewInstance>> generatedFactories
            = new Dictionary<Type, Func<IView, ObservableObject, ViewInstance>>();

        /// <summary>
        /// 由生成代码按 ViewModel 类型注册绑定工厂，避免运行时程序集扫描与反射构造。
        /// View 只是 ViewModel 数据的一种显示投影，不参与工厂键匹配。
        /// </summary>
        internal static void RegisterGeneratedFactory(
            Type viewModelType,
            Func<IView, ObservableObject, ViewInstance> factory)
        {
            if (viewModelType == null)
            {
                throw new ArgumentNullException(nameof(viewModelType));
            }
            generatedFactories[viewModelType] =
                factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// 由生成 Route 的直接构造委托调用。
        /// </summary>
        internal static ViewInstance CreateGenerated(object bindingContext, object presenter)
        {
            if (bindingContext is not IBindingContext typedBindingContext)
            {
                throw new ArgumentException("The generated BindingContext factory returned an invalid object.", nameof(bindingContext));
            }

            if (presenter is not IPresenter typedPresenter)
            {
                throw new ArgumentException("The generated Presenter factory returned an invalid object.", nameof(presenter));
            }

            return new ViewInstance(typedBindingContext, typedPresenter);
        }

        /// <summary>
        /// 使用生成工厂创建 Prototype、List Item 或 DynamicView 实例。
        /// </summary>
        internal static ViewInstance Create(IView view, ObservableObject viewModel)
        {
            if (view == null)
            {
                return null;
            }

            if (viewModel == null)
            {
                view.Initialize();
                var emptyViewModel = new EmptyViewModel();
                return new ViewInstance(new EmptyBindingContext(view, emptyViewModel), new EmptyPresenter());
            }

            if (!TryResolveGeneratedFactory(viewModel.GetType(), out var factory))
            {
                throw new InvalidOperationException(
                    $"No ViewModel binding factory is registered: ViewModel={viewModel.GetType().FullName}, View={view.Name}. Ensure that the generated Routes.Initialize() method has been called.");
            }
            return factory(view, viewModel);
        }

        static bool TryResolveGeneratedFactory(
            Type viewModelType,
            out Func<IView, ObservableObject, ViewInstance> factory)
        {
            // 派生 ViewModel 没有专属工厂时复用最近的基类绑定；View 不参与匹配。
            for (var current = viewModelType;
                 current != null && typeof(ObservableObject).IsAssignableFrom(current);
                 current = current.BaseType)
            {
                if (generatedFactories.TryGetValue(current, out factory))
                {
                    return true;
                }
            }

            factory = null;
            return false;
        }
    }
}
