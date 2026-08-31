using FUI.Navigation;
using FUI.Infrastructure;
using FUI.Presentation;
using FUI.Resources;
using FUI.Rendering;
using FUI.Transitions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Tests
{
    public sealed class NavigatorTests
    {
        TestViewProvider provider;
        TestExecutionContext executionContext;
        TestInputGate inputGate;
        Navigator manager;

        [SetUp]
        public void SetUp()
        {
            ReusableBindingContext.Reset();
            GeneratedBindingRegistry.Register<ReusableViewModel>(
                (view, viewModel) => new ReusableBindingContext(view, viewModel),
                () => new EmptyPresenter());
            provider = new TestViewProvider();
            executionContext = new TestExecutionContext();
            inputGate = new TestInputGate();
            manager = new Navigator(provider, executionContext, inputGate);
            manager.Initialize();
        }

        [Test]
        public void SameViewModelCanBindIndependentViewsAndDetachSeparately()
        {
            var viewModel = new ReusableViewModel();
            var firstView = new TestView("FirstProjection");
            var secondView = new TestView("SecondProjection");
            var first = Projection.Create(firstView, viewModel);
            var second = Projection.Create(secondView, viewModel);

            first.Activate();
            second.Activate();

            Assert.That(first.ViewModel, Is.SameAs(viewModel));
            Assert.That(second.ViewModel, Is.SameAs(viewModel));
            Assert.That(firstView.InitializeCount, Is.EqualTo(1));
            Assert.That(secondView.InitializeCount, Is.EqualTo(1));
            Assert.That(firstView.SynchronizeCount, Is.EqualTo(1));
            Assert.That(secondView.SynchronizeCount, Is.EqualTo(1));
            Assert.That(ReusableBindingContext.ActiveBindings, Is.EqualTo(2));

            first.Dispose();

            Assert.That(ReusableBindingContext.ActiveBindings, Is.EqualTo(1));
            Assert.That(second.ViewModel, Is.SameAs(viewModel));

            second.Dispose();
            Assert.That(ReusableBindingContext.ActiveBindings, Is.Zero);
        }

        [Test]
        public void ViewModelFactorySupportsCompatibleProjectionWithDifferentViewName()
        {
            GeneratedBindingRegistry.Register<NamedProjectionViewModel>(
                (view, viewModel) => new NamedProjectionBindingContext(view, viewModel),
                () => new EmptyPresenter());
            var viewModel = new NamedProjectionViewModel();
            var projectedView = new TestView("CompatibleNestedView");
            var projection = Projection.Create(projectedView, viewModel);

            projection.Activate();

            Assert.That(projection.ViewModel, Is.SameAs(viewModel));
            Assert.That(projectedView.InitializeCount, Is.EqualTo(1));

            projection.Dispose();
        }

        [Test]
        public void TopLevelRouteCanBindExistingViewModel()
        {
            var route = new RouteHarness().Create("AssignedViewModel");
            var assigned = new TestViewModel();

            var handle = manager.OpenWithViewModel(route, assigned);

            Assert.That(manager.TryGetViewModel<TestViewModel>(handle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(assigned));
        }

        [Test]
        public void SameDataContractSupportsIndependentViewVariantsAndPolicies()
        {
            var harness = new RouteHarness();
            var full = harness.Create("StatusFull", new RoutePolicy(layer: Layer.Panel));
            var compact = full.CreateViewVariant(
                "StatusCompact",
                new RoutePolicy(layer: Layer.Popup, coverageMode: CoverageMode.KeepVisible));
            var shared = new TestViewModel();

            var fullHandle = manager.OpenWithViewModel(full, shared);
            var compactHandle = manager.OpenWithViewModel(compact, shared);

            Assert.That(full, Is.Not.EqualTo(compact));
            Assert.That(full.Settings.Layer, Is.EqualTo(Layer.Panel));
            Assert.That(compact.Settings.Layer, Is.EqualTo(Layer.Popup));
            Assert.That(manager.TryGetViewModel<TestViewModel>(fullHandle, out var fullViewModel), Is.True);
            Assert.That(manager.TryGetViewModel<TestViewModel>(compactHandle, out var compactViewModel), Is.True);
            Assert.That(fullViewModel, Is.SameAs(shared));
            Assert.That(compactViewModel, Is.SameAs(shared));
        }

        [Test]
        public void NonMultipleRouteRequiresExplicitRebindForDifferentViewModel()
        {
            var route = new RouteHarness().Create("RebindExisting");
            var first = new TestViewModel();
            var second = new TestViewModel();
            var handle = manager.OpenWithViewModel(route, first);

            Assert.Throws<InvalidOperationException>(() => manager.OpenWithViewModel(route, second));
            var reboundHandle = manager.OpenWithViewModel(route, second, options: OpenOptions.Rebind);

            Assert.That(reboundHandle, Is.EqualTo(handle));
            Assert.That(manager.TryGetViewModel<TestViewModel>(handle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(second));
            Assert.That(provider.CreateCount, Is.EqualTo(1));
        }

        [Test]
        public void DerivedViewModelFallsBackToBaseBindingFactory()
        {
            var viewModel = new DerivedReusableViewModel();
            var projection = Projection.Create(new TestView("DerivedProjection"), viewModel);

            projection.Activate();

            Assert.That(projection.ViewModel, Is.SameAs(viewModel));
            Assert.That(ReusableBindingContext.ActiveBindings, Is.EqualTo(1));

            projection.Dispose();
            Assert.That(ReusableBindingContext.ActiveBindings, Is.Zero);
        }

        [TearDown]
        public void TearDown()
        {
            manager.Shutdown();
        }

        [Test]
        public void CoverAndRevealDoNotRepeatOpenOrCloseAndHiddenViewBecomesInvisible()
        {
            var firstHarness = new RouteHarness();
            var secondHarness = new RouteHarness();
            var first = firstHarness.Create("First");
            var second = secondHarness.Create("Second", new RoutePolicy(coverageMode: CoverageMode.Hide));

            var firstHandle = manager.Open(first);
            var secondHandle = manager.Open(second);

            Assert.That(manager.GetState(firstHandle), Is.EqualTo(ViewState.Covered));
            Assert.That(provider.Leases[0].View.Visible, Is.False);
            Assert.That(firstHarness.Presenters[0].OpenCount, Is.EqualTo(1));
            Assert.That(firstHarness.Presenters[0].CoveredCount, Is.EqualTo(1));
            Assert.That(firstHarness.Presenters[0].CloseCount, Is.Zero);

            manager.Close(secondHandle);

            Assert.That(manager.GetState(firstHandle), Is.EqualTo(ViewState.Open));
            Assert.That(provider.Leases[0].View.Visible, Is.True);
            Assert.That(firstHarness.Presenters[0].RevealedCount, Is.EqualTo(1));
            Assert.That(firstHarness.Presenters[0].OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void AllowMultipleSupportsExactCloseBringToFrontAndBack()
        {
            var harness = new RouteHarness();
            var route = harness.Create("Multi", new RoutePolicy(allowMultiple: true));
            var first = manager.Open(route);
            var second = manager.Open(route);

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(provider.Leases[0].View.Order, Is.EqualTo(0));
            Assert.That(provider.Leases[1].View.Order, Is.EqualTo(1));
            Assert.That(manager.BringToFront(first), Is.True);
            Assert.That(provider.Leases[0].View.Order, Is.EqualTo(1));
            Assert.That(provider.Leases[1].View.Order, Is.EqualTo(0));

            Assert.That(manager.Close(first), Is.True);
            Assert.That(manager.IsAlive(first), Is.False);
            Assert.That(manager.IsAlive(second), Is.True);
            Assert.That(manager.Back(), Is.True);
            Assert.That(manager.IsAlive(second), Is.False);
        }

        [Test]
        public void KeepAliveReuseSkipsProviderAndInvalidatesOldHandlePermanently()
        {
            var harness = new RouteHarness();
            var route = harness.Create("Cached", new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            var oldHandle = manager.Open(route);
            Assert.That(manager.TryGetViewModel<TestViewModel>(oldHandle, out var oldViewModel), Is.True);

            manager.Close(oldHandle);
            var newHandle = manager.Open(route);

            Assert.That(manager.GetState(oldHandle), Is.EqualTo(ViewState.Cached));
            Assert.That(manager.IsAlive(oldHandle), Is.False);
            Assert.That(newHandle, Is.Not.EqualTo(oldHandle));
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(manager.TryGetViewModel<TestViewModel>(newHandle, out var newViewModel), Is.True);
            Assert.That(newViewModel, Is.SameAs(oldViewModel));
        }

        [Test]
        public void KeepAliveViewCanRebindToNewViewModel()
        {
            var route = new RouteHarness().Create(
                "CachedRebind",
                new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            var first = new TestViewModel();
            var second = new TestViewModel();
            var oldHandle = manager.OpenWithViewModel(route, first);

            manager.Close(oldHandle);
            var newHandle = manager.OpenWithViewModel(route, second);

            Assert.That(newHandle, Is.Not.EqualTo(oldHandle));
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(manager.TryGetViewModel<TestViewModel>(newHandle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(second));
        }

        [Test]
        public async System.Threading.Tasks.Task AsyncRouteCanBindExistingViewModel()
        {
            var route = new RouteHarness().Create("AssignedAsync");
            var assigned = new TestViewModel();

            var handle = await manager.OpenAsyncWithViewModel(route, assigned);

            Assert.That(manager.TryGetViewModel<TestViewModel>(handle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(assigned));
            await manager.CloseAsync(handle);
        }

        [Test]
        public void LruCapacityEvictsOldestCachedView()
        {
            var route = new RouteHarness().Create(
                "LRU",
                new RoutePolicy(cacheMode: CacheMode.KeepAlive, allowMultiple: true, cacheCapacity: 1));
            var first = manager.Open(route);
            var second = manager.Open(route);

            manager.Close(first);
            manager.Close(second);

            Assert.That(provider.Leases[0].Disposed, Is.True);
            Assert.That(provider.Leases[1].Disposed, Is.False);
            manager.Open(route);
            Assert.That(provider.CreateCount, Is.EqualTo(2));
        }

        [Test]
        public void ClosingTopLevelViewDestroysItExactlyOnceThroughLease()
        {
            var route = new RouteHarness().Create("SingleOwner");
            var handle = manager.Open(route);

            manager.Close(handle);

            Assert.That(((TestView)provider.Leases.Single().View).DestroyCount, Is.EqualTo(1));
            Assert.That(provider.Leases.Single().Disposed, Is.True);
        }

        [Test]
        public void TimedCacheExpiresAndDisposesLeaseOnTick()
        {
            var route = new RouteHarness().Create(
                "Timed",
                new RoutePolicy(cacheMode: CacheMode.Timed, cacheDuration: 2f));
            var handle = manager.Open(route);
            manager.Close(handle);

            manager.Tick(1.9f);
            Assert.That(provider.Leases[0].Disposed, Is.False);
            manager.Tick(0.1f);
            Assert.That(provider.Leases[0].Disposed, Is.True);
        }

        [Test]
        public void PreloadDoesNotCreateViewAndTransfersLeaseOnOpen()
        {
            var harness = new RouteHarness();
            var route = harness.Create("Preloaded");

            manager.PreloadAsync(route).AsTask().GetAwaiter().GetResult();
            Assert.That(provider.PreloadCount, Is.EqualTo(1));
            Assert.That(provider.CreateCount + provider.CreateAsyncCount, Is.Zero);
            Assert.That(harness.Presenters, Is.Empty);

            manager.Open(route);
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(provider.LastCreatePreload, Is.Not.Null);
            Assert.That(harness.Presenters.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancelledPreloadDisposesUncommittedLeaseAfterThreadSwitch()
        {
            var route = new RouteHarness().Create("CancelledPreload");
            executionContext.CancelOnSwitch = true;

            Assert.That(
                async () => await manager.PreloadAsync(route, CancellationToken.None).AsTask(),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(provider.LastPreloadLease, Is.Not.Null);
            Assert.That(provider.LastPreloadLease.Disposed, Is.True);
            Assert.That(provider.LastPreloadLease.Transferred, Is.False);
        }

        [Test]
        public void ReplaceFailurePreservesExistingView()
        {
            var oldRoute = new RouteHarness().Create("Old");
            var failedRoute = new RouteHarness().Create("Failed");
            provider.ThrowOnCreate.Add("Failed");
            var oldHandle = manager.Open(oldRoute);

            Assert.Throws<InvalidOperationException>(() => manager.Open(failedRoute, options: OpenOptions.Replace));
            Assert.That(manager.GetState(oldHandle), Is.EqualTo(ViewState.Open));
            Assert.That(manager.IsAlive(oldHandle), Is.True);
        }

        [Test]
        public void BindingFailureRollsBackInReverseOrderWithoutListeners()
        {
            FailingBindingContext.Reset();
            var presenter = new TestPresenter();
            var route = GeneratedRouteFactory.Create<TestViewModel>(
                "BindingFailure",
                typeof(TestPresenter),
                () => new TestViewModel(),
                (view, viewModel) => new FailingBindingContext(view, viewModel),
                () => presenter,
                RoutePolicy.Default);

            Assert.Throws<InvalidOperationException>(() => manager.Open(route));
            Assert.That(FailingBindingContext.ActiveListeners, Is.Zero);
            Assert.That(provider.Leases.Single().Disposed, Is.True);
            Assert.That(presenter.DestroyCount, Is.EqualTo(1));
        }

        [Test]
        public void BindingFailureDoesNotReuseTransferredPreloadLease()
        {
            FailingBindingContext.Reset();
            var route = GeneratedRouteFactory.Create<TestViewModel>(
                "PreloadedBindingFailure",
                typeof(TestPresenter),
                () => new TestViewModel(),
                (view, viewModel) => new FailingBindingContext(view, viewModel),
                () => new TestPresenter(),
                RoutePolicy.Default);

            manager.PreloadAsync(route).AsTask().GetAwaiter().GetResult();
            var preload = provider.LastPreloadLease;
            Assert.Throws<InvalidOperationException>(() => manager.Open(route));
            Assert.That(preload.Transferred, Is.True);

            Assert.Throws<InvalidOperationException>(() => manager.Open(route));
            Assert.That(provider.LastCreatePreload, Is.Null);
            Assert.That(provider.Leases.Count, Is.EqualTo(2));
            Assert.That(provider.Leases.All(lease => lease.Disposed), Is.True);
        }

        [Test]
        public void SharedDependencyReleasesAfterLastParentCloses()
        {
            var dependency = new RouteHarness().Create("Dependency");
            var dependencySettings = new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => dependency),
            };
            var firstParent = new RouteHarness().Create("ParentA", new RoutePolicy(dependencies: dependencySettings));
            var secondParent = new RouteHarness().Create("ParentB", new RoutePolicy(dependencies: dependencySettings));

            var first = manager.Open(firstParent);
            var dependencyHandle = inputGate.Acquired.Single(item => item.phase == OperationPhase.Open && item.handle != first).handle;
            var second = manager.Open(secondParent);

            Assert.That(provider.CreateCount, Is.EqualTo(3));
            manager.Close(first);
            Assert.That(manager.IsAlive(dependencyHandle), Is.True);
            manager.Close(second);
            Assert.That(manager.IsAlive(dependencyHandle), Is.False);
        }

        [Test]
        public void AttachedAfterDependencySkipsHistoryAndClosesWithParentOnBack()
        {
            var statusHarness = new RouteHarness();
            var shopHarness = new RouteHarness();
            var status = statusHarness.Create("Status");
            var dependencies = new[]
            {
                new RouteDependency(DependencyTiming.AttachedAfter, () => status),
            };
            var shop = shopHarness.Create("Shop", new RoutePolicy(dependencies: dependencies));

            var shopHandle = manager.Open(shop);
            var statusHandle = inputGate.Acquired
                .Single(item => item.phase == OperationPhase.Open && item.handle != shopHandle)
                .handle;

            Assert.That(manager.GetState(shopHandle), Is.EqualTo(ViewState.Open));
            Assert.That(shopHarness.Presenters.Single().FocusCount, Is.EqualTo(1));
            Assert.That(shopHarness.Presenters.Single().CoveredCount, Is.Zero);
            Assert.That(statusHarness.Presenters.Single().FocusCount, Is.Zero);
            Assert.That(provider.Leases.Single(item => item.View.Name == "Shop").View.Order, Is.EqualTo(0));
            Assert.That(provider.Leases.Single(item => item.View.Name == "Status").View.Order, Is.EqualTo(1));

            Assert.That(manager.Back(), Is.True);
            Assert.That(manager.IsAlive(shopHandle), Is.False);
            Assert.That(manager.IsAlive(statusHandle), Is.False);
            Assert.That(manager.Back(), Is.False);
        }

        [Test]
        public void RequiredBeforeDependencyDoesNotTakeFocusOrEnterBackHistory()
        {
            var statusHarness = new RouteHarness();
            var parentHarness = new RouteHarness();
            var status = statusHarness.Create("RequiredStatus");
            var dependencies = new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => status),
            };
            var parent = parentHarness.Create("Parent", new RoutePolicy(dependencies: dependencies));

            var parentHandle = manager.Open(parent);
            var statusHandle = inputGate.Acquired
                .Single(item => item.phase == OperationPhase.Open && item.handle != parentHandle)
                .handle;

            Assert.That(statusHarness.Presenters.Single().FocusCount, Is.Zero);
            Assert.That(parentHarness.Presenters.Single().FocusCount, Is.EqualTo(1));
            Assert.That(provider.Leases.Single(item => item.View.Name == "RequiredStatus").View.Order, Is.EqualTo(0));
            Assert.That(provider.Leases.Single(item => item.View.Name == "Parent").View.Order, Is.EqualTo(1));

            Assert.That(manager.Back(), Is.True);
            Assert.That(manager.IsAlive(parentHandle), Is.False);
            Assert.That(manager.IsAlive(statusHandle), Is.False);
            Assert.That(manager.Back(), Is.False);
        }

        [Test]
        public void ExplicitlyOpenedDependencySurvivesParentClosure()
        {
            var statusHarness = new RouteHarness();
            var parentHarness = new RouteHarness();
            var status = statusHarness.Create("ExplicitStatus");
            var dependencies = new[]
            {
                new RouteDependency(DependencyTiming.AttachedAfter, () => status),
            };
            var parent = parentHarness.Create("ParentWithStatus", new RoutePolicy(dependencies: dependencies));

            var statusHandle = manager.Open(status);
            var parentHandle = manager.Open(parent);

            Assert.That(statusHarness.Presenters.Single().FocusCount, Is.EqualTo(1));
            Assert.That(parentHarness.Presenters.Single().FocusCount, Is.EqualTo(1));
            Assert.That(parentHarness.Presenters.Single().CoveredCount, Is.Zero);

            Assert.That(manager.Back(), Is.True);
            Assert.That(manager.IsAlive(parentHandle), Is.False);
            Assert.That(manager.IsAlive(statusHandle), Is.True);
            Assert.That(manager.GetState(statusHandle), Is.EqualTo(ViewState.Open));
            Assert.That(statusHarness.Presenters.Single().RevealedCount, Is.EqualTo(1));
        }

        [Test]
        public void LateTransitionCompletionCannotReviveHandleClosedDuringOpening()
        {
            var transitionProvider = new TestTransitionProvider();
            var route = new RouteHarness().Create(
                "Opening",
                new RoutePolicy(enterTransitionProvider: transitionProvider));
            var handle = manager.Open(route);

            Assert.That(manager.GetState(handle), Is.EqualTo(ViewState.Opening));
            Assert.That(manager.Close(handle), Is.True);
            Assert.That(manager.GetState(handle), Is.EqualTo(ViewState.Destroyed));
            transitionProvider.Transition.Complete();

            Assert.That(manager.GetState(handle), Is.EqualTo(ViewState.Destroyed));
            Assert.That(transitionProvider.Transition.StopCount, Is.EqualTo(1));
            Assert.That(inputGate.Released, Is.EqualTo(inputGate.Acquired.Count));
        }
    }
}
