using FUI.Binding;
using FUI.Infrastructure;
using FUI.Presentation;
using FUI.Rendering;
using System;

namespace FUI.Navigation
{
    /// <summary>
    /// 页面变体的强类型运行时键。Route 相等性由资源、有效 ViewModel 和 Presenter 共同决定。
    /// </summary>
    public abstract class Route : IEquatable<Route>
    {
        internal RouteDescriptor Descriptor { get; }

        /// <summary>Provider 使用的资源标识。</summary>
        public string AssetKey => Descriptor.AssetKey;

        /// <summary>该 Route 变体实际构造的 ViewModel 类型。</summary>
        public Type ViewModelType => Descriptor.ViewModelType;

        /// <summary>该 Route 变体实际构造的 Presenter 类型。</summary>
        public Type PresenterType => Descriptor.PresenterType;

        /// <summary>编译期固化的页面配置。</summary>
        public RoutePolicy Settings => Descriptor.Settings;

        protected Route(RouteDescriptor descriptor)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public bool Equals(Route other) => other != null && Descriptor.Equals(other.Descriptor);

        public override bool Equals(object obj) => Equals(obj as Route);

        public override int GetHashCode() => Descriptor.GetHashCode();

        public override string ToString() => Descriptor.ToString();
    }

    /// <summary>
    /// 带 ViewModel 类型约束的页面 Route，由 Source Generator 生成并供业务层直接使用。
    /// </summary>
    public sealed class Route<TViewModel> : Route where TViewModel : ObservableObject
    {
        readonly Func<TViewModel> viewModelFactory;
        readonly Func<IView, TViewModel, object> bindingContextFactory;
        readonly Func<object> presenterFactory;

        internal Route(
            string assetKey,
            Type presenterType,
            Func<TViewModel> viewModelFactory,
            Func<IView, TViewModel, object> bindingContextFactory,
            Func<object> presenterFactory,
            RoutePolicy settings)
            : base(new RouteDescriptor(
                assetKey,
                typeof(TViewModel),
                presenterType,
                (view, assignedViewModel) =>
                {
                    // Route 只描述投影配方；调用方可以复用已有数据，也可以使用默认工厂创建数据。
                    var viewModel = assignedViewModel == null
                        ? viewModelFactory()
                        : assignedViewModel as TViewModel
                            ?? throw new ArgumentException(
                                $"Route {assetKey} cannot bind ViewModel {assignedViewModel.GetType().FullName}.",
                                nameof(assignedViewModel));
                    ViewInstance.PrepareView(view);
                    var bindingContext = bindingContextFactory(view, viewModel);
                    var presenter = presenterFactory();
                    return ViewInstance.CreateGenerated(bindingContext, presenter);
                },
                settings))
        {
            if (viewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(viewModelFactory));
            }

            if (bindingContextFactory == null)
            {
                throw new ArgumentNullException(nameof(bindingContextFactory));
            }

            if (presenterFactory == null)
            {
                throw new ArgumentNullException(nameof(presenterFactory));
            }

            this.viewModelFactory = viewModelFactory;
            this.bindingContextFactory = bindingContextFactory;
            this.presenterFactory = presenterFactory;
        }

        /// <summary>
        /// 基于同一数据和绑定契约创建另一个顶层 View 变体；新变体拥有独立 Asset、Settings 和缓存键。
        /// </summary>
        public Route<TViewModel> CreateViewVariant(
            string assetKey,
            RoutePolicy settings)
        {
            return new Route<TViewModel>(
                assetKey,
                PresenterType,
                viewModelFactory,
                bindingContextFactory,
                presenterFactory,
                settings ?? RoutePolicy.Default);
        }
    }

    /// <summary>
    /// Provider 和 Manager 使用的不可变 Route 描述，不包含运行时反射查找。
    /// </summary>
    public sealed class RouteDescriptor : IEquatable<RouteDescriptor>
    {
        readonly Func<IView, ObservableObject, ViewInstance> instanceFactory;

        public string AssetKey { get; }

        public Type ViewModelType { get; }

        public Type PresenterType { get; }

        public RoutePolicy Settings { get; }


        internal RouteDescriptor(
            string assetKey,
            Type viewModelType,
            Type presenterType,
            Func<IView, ObservableObject, ViewInstance> instanceFactory,
            RoutePolicy settings)
        {
            AssetKey = string.IsNullOrWhiteSpace(assetKey)
                ? throw new ArgumentException("The Route asset key cannot be empty.", nameof(assetKey))
                : assetKey;
            ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
            PresenterType = presenterType ?? throw new ArgumentNullException(nameof(presenterType));
            this.instanceFactory = instanceFactory ?? throw new ArgumentNullException(nameof(instanceFactory));
            Settings = settings ?? RoutePolicy.Default;
        }

        internal ViewInstance CreateInstance(IView view, ObservableObject viewModel = null)
        {
            return instanceFactory(view, viewModel);
        }

        public bool Equals(RouteDescriptor other)
        {
            // 同一 Prefab 搭配派生 ViewModel 或不同 Presenter 时必须形成独立页面变体。
            return other != null
                && AssetKey == other.AssetKey
                && ViewModelType == other.ViewModelType
                && PresenterType == other.PresenterType;
        }

        public override bool Equals(object obj) => Equals(obj as RouteDescriptor);

        public override int GetHashCode() => HashCode.Combine(AssetKey, ViewModelType, PresenterType);

        public override string ToString() => $"{AssetKey}<{ViewModelType.Name}, {PresenterType.Name}>";
    }
}
