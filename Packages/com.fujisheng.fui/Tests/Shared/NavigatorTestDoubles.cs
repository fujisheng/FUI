using FUI.Binding;
using FUI.Infrastructure.Binding;
using FUI.Infrastructure;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Resources;
using FUI.Rendering;
using FUI.Transitions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Tests
{
    public sealed class TestViewModel : ObservableObject
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ReusableViewModel : ObservableObject { }

    public sealed class DerivedReusableViewModel : ReusableViewModel { }

    public sealed class NamedProjectionViewModel : ObservableObject { }

    public sealed class NamedProjectionBindingContext : BindingContext<NamedProjectionViewModel>
    {
        public NamedProjectionBindingContext(IView view, NamedProjectionViewModel viewModel)
            : base(view, viewModel)
        {
        }
    }

    public sealed class ReusableBindingContext : BindingContext<ReusableViewModel>
    {
        public static int ActiveBindings { get; private set; }

        public ReusableBindingContext(IView view, ReusableViewModel viewModel) : base(view, viewModel) { }

        public static void Reset() => ActiveBindings = 0;

        protected override void OnBinding() => ActiveBindings++;

        protected override void OnSynchronize()
        {
            if (View is TestView testView)
            {
                testView.SynchronizeCount++;
            }
        }

        protected override void OnUnbinding() => ActiveBindings--;
    }

    public sealed class TestBindingContext : BindingContext<TestViewModel>
    {
        public TestBindingContext(IView view, TestViewModel viewModel) : base(view, viewModel) { }
    }

    public sealed class FailingBindingContext : BindingContext<TestViewModel>
    {
        public static int ActiveListeners { get; private set; }

        public FailingBindingContext(IView view, TestViewModel viewModel) : base(view, viewModel) { }

        public static void Reset() => ActiveListeners = 0;

        protected override void OnBinding()
        {
            ActiveListeners++;
            throw new InvalidOperationException("测试绑定失败");
        }

        protected override void OnUnbinding()
        {
            if (ActiveListeners > 0)
            {
                ActiveListeners--;
            }
        }
    }

    public sealed class TestPresenter : Presenter<TestViewModel>
    {
        public Action OpenAction { get; set; }
        public Action FocusAction { get; set; }
        public Action CoveredAction { get; set; }
        public int CreateCount { get; private set; }
        public int OpenCount { get; private set; }
        public int FocusCount { get; private set; }
        public int UnfocusCount { get; private set; }
        public int CoveredCount { get; private set; }
        public int RevealedCount { get; private set; }
        public int CloseCount { get; private set; }
        public int DestroyCount { get; private set; }
        public int SynchronizeCount { get; set; }

        protected override void OnCreate() => CreateCount++;
        protected override void OnOpen(object param)
        {
            OpenCount++;
            OpenAction?.Invoke();
        }
        protected override void OnFocus()
        {
            FocusCount++;
            FocusAction?.Invoke();
        }
        protected override void OnUnfocus() => UnfocusCount++;
        protected override void OnCovered()
        {
            CoveredCount++;
            CoveredAction?.Invoke();
        }
        protected override void OnRevealed() => RevealedCount++;
        protected override void OnClose() => CloseCount++;
        protected override void OnDestroy() => DestroyCount++;
    }

    public sealed class TestView : IView
    {
        static readonly IElement[] noElements = Array.Empty<IElement>();

        public string Name { get; }
        public int Layer { get; set; }
        public int Order { get; set; }
        public bool Visible { get; set; }
        public bool Interactable { get; set; }
        public IReadOnlyList<IElement> Elements => noElements;
        public int InitializeCount { get; private set; }
        public int DestroyCount { get; private set; }
        public int SynchronizeCount { get; set; }

        public TestView(string name)
        {
            Name = name;
        }

        public T GetElement<T>(string path) where T : IElement => default;
        public IElement GetElement(string path, Type elementType) => null;
        public void Initialize() => InitializeCount++;
        public void Destroy() => DestroyCount++;
    }

    public sealed class TestAssetScope : IAssetScope
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
        public IAssetLease<T> Load<T>(string key) => throw new NotSupportedException();
        public ValueTask<IAssetLease<T>> LoadAsync<T>(string key, CancellationToken token) => throw new NotSupportedException();
        public IAssetLease<T> Instantiate<T>(string key) => throw new NotSupportedException();
        public ValueTask<IAssetLease<T>> InstantiateAsync<T>(string key, CancellationToken token) => throw new NotSupportedException();
    }

    public sealed class TestViewLease : IViewLease
    {
        public IView View { get; }
        public IAssetScope Assets { get; }
        public bool Disposed { get; private set; }

        public TestViewLease(TestView view)
        {
            View = view;
            Assets = new TestAssetScope();
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            Disposed = true;
            View.Destroy();
            Assets.Dispose();
        }
    }

    public sealed class TestPreloadLease : IPreloadLease
    {
        public RouteDescriptor Route { get; }
        public bool Disposed { get; private set; }
        public bool Transferred { get; private set; }
        public int DisposeCount { get; private set; }

        public TestPreloadLease(RouteDescriptor route)
        {
            Route = route;
        }

        public void Transfer()
        {
            Transferred = true;
            Disposed = true;
        }

        public void Dispose()
        {
            DisposeCount++;
            Disposed = true;
        }
    }

    public sealed class TestViewProvider : IViewProvider
    {
        public int CreateCount { get; private set; }
        public bool ControlPreloads { get; set; }
        public bool RequirePrepared { get; set; }
        public List<PendingPreload> Pending { get; } = new List<PendingPreload>();
        public int CreateAsyncCount { get; private set; }
        public int PreloadCount { get; private set; }
        public IPreloadLease LastCreatePreload { get; private set; }
        public TestPreloadLease LastPreloadLease { get; private set; }
        public List<TestViewLease> Leases { get; } = new List<TestViewLease>();
        public HashSet<string> ThrowOnCreate { get; } = new HashSet<string>();

        public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
        {
            if (RequirePrepared && (preload is not TestPreloadLease ready || ready.Disposed || !ready.Route.Equals(route)))
            {
                throw new InvalidOperationException("异步呈现必须消费已准备好的资源。");
            }
            CreateCount++;
            LastCreatePreload = preload;
            var lease = CreateLease(route);
            (preload as TestPreloadLease)?.Transfer();
            return lease;
        }

        public ValueTask<IViewLease> CreateAsync(RouteDescriptor route, IPreloadLease preload, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            CreateAsyncCount++;
            LastCreatePreload = preload;
            var lease = CreateLease(route);
            (preload as TestPreloadLease)?.Transfer();
            return new ValueTask<IViewLease>(lease);
        }

        public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            PreloadCount++;
            LastPreloadLease = new TestPreloadLease(route);
            if (ControlPreloads)
            {
                var pending = new PendingPreload(LastPreloadLease);
                Pending.Add(pending);
                return new ValueTask<IPreloadLease>(pending.Completion.Task);
            }
            return new ValueTask<IPreloadLease>(LastPreloadLease);
        }

        public sealed class PendingPreload
        {
            public TestPreloadLease Lease { get; }
            public TaskCompletionSource<IPreloadLease> Completion { get; } = new TaskCompletionSource<IPreloadLease>();
            public PendingPreload(TestPreloadLease lease) { Lease = lease; }
            // 故意忽略取消，验证不可取消的底层请求迟到时仍能释放资源。
            public void Complete() => Completion.SetResult(Lease);
            public void Fail() => Completion.SetException(new InvalidOperationException("测试资源加载失败"));
        }

        TestViewLease CreateLease(RouteDescriptor route)
        {
            if (ThrowOnCreate.Contains(route.AssetKey))
            {
                throw new InvalidOperationException($"测试 Provider 创建失败：{route.AssetKey}");
            }
            var lease = new TestViewLease(new TestView(route.AssetKey));
            Leases.Add(lease);
            return lease;
        }
    }

    public sealed class TestExecutionContext : IMainThreadDispatcher
    {
        public bool IsMainThread => true;
        public int AssertCount { get; private set; }
        public int SwitchCount { get; private set; }
        public bool CancelOnSwitch { get; set; }

        public void AssertMainThread() => AssertCount++;

        public ValueTask SwitchToMainThreadAsync(CancellationToken cancellationToken = default)
        {
            if (CancelOnSwitch)
            {
                throw new OperationCanceledException("测试线程切换取消", cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            SwitchCount++;
            return default;
        }
    }

    public sealed class TestInputGate : IInputGate
    {
        public readonly List<(ViewHandle handle, OperationPhase phase)> Acquired = new List<(ViewHandle, OperationPhase)>();
        public int Released { get; private set; }

        public IDisposable Acquire(ViewHandle handle, OperationPhase phase)
        {
            Acquired.Add((handle, phase));
            return new ReleaseLease(() => Released++);
        }

        sealed class ReleaseLease : IDisposable
        {
            Action release;
            public ReleaseLease(Action release) => this.release = release;
            public void Dispose()
            {
                release?.Invoke();
                release = null;
            }
        }
    }

    public sealed class ManualTransition : ITransition
    {
        readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        public bool IsPlaying { get; private set; }
        public int PlayCount { get; private set; }
        public int StopCount { get; private set; }

        public ValueTask PlayAsync(CancellationToken ct)
        {
            PlayCount++;
            IsPlaying = true;
            return new ValueTask(completion.Task);
        }

        public void Stop()
        {
            StopCount++;
            IsPlaying = false;
        }

        public void Complete()
        {
            IsPlaying = false;
            completion.TrySetResult(true);
        }
    }

    public sealed class TestTransitionProvider : ITransitionProvider
    {
        public ManualTransition Transition { get; } = new ManualTransition();
        public ITransition Get(IView view) => Transition;
    }

    public sealed class RouteHarness
    {
        public List<TestPresenter> Presenters { get; } = new List<TestPresenter>();

        public Route<TestViewModel> Create(string assetKey, RoutePolicy settings = null, Action onOpen = null)
        {
            return GeneratedRouteFactory.Create<TestViewModel>(
                assetKey,
                typeof(TestPresenter),
                () => new TestViewModel(),
                (view, viewModel) => new TestBindingContext(view, viewModel),
                () =>
                {
                    var presenter = new TestPresenter { OpenAction = onOpen };
                    Presenters.Add(presenter);
                    return presenter;
                },
                settings ?? RoutePolicy.Default);
        }
    }
}
