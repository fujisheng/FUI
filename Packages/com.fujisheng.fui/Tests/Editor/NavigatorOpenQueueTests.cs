using FUI.Navigation;
using FUI.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Tests
{
    [Timeout(10000)]
    public sealed class NavigatorOpenQueueTests
    {
        TestViewProvider provider;
        Navigator navigator;
        RouteHarness routes;
        List<string> opened;

        [SetUp]
        public void SetUp()
        {
            provider = new TestViewProvider { ControlPreloads = true, RequirePrepared = true };
            navigator = new Navigator(provider, new TestExecutionContext());
            navigator.Initialize();
            routes = new RouteHarness();
            opened = new List<string>();
        }

        [TearDown]
        public void TearDown() => navigator.Shutdown();

        Route<TestViewModel> Route(string name, RoutePolicy policy = null)
        {
            return routes.Create(name, policy, () => opened.Add(name));
        }

        static async Task ExpectFailureAsync<T>(Task request) where T : Exception
        {
            // 不使用会同步阻塞 Unity 主线程的 Assert.ThrowsAsync。
            Assert.That(await Task.WhenAny(request, Task.Delay(5000)), Is.SameAs(request), "打开请求未能结束");
            try
            {
                await request;
            }
            catch (T)
            {
                return;
            }
            Assert.Fail("打开请求没有抛出预期异常：" + typeof(T).Name);
        }

        static int GetPendingOpenCount(Navigator target)
        {
            var field = typeof(Navigator).GetField("openRequests",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var queue = field?.GetValue(target);
            var count = queue?.GetType().GetProperty("Count")?.GetValue(queue);
            return count is int value ? value : -1;
        }

        [Test]
        public async Task LoadsStartTogetherButAllPresentationFollowsCallOrder()
        {
            var a = navigator.OpenAsync(Route("A")).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            var c = navigator.OpenAsync(Route("C")).AsTask();
            Assert.That(provider.Pending.Count, Is.EqualTo(3));
            provider.Pending[1].Complete();
            provider.Pending[2].Complete();
            Assert.That(opened, Is.Empty);
            Assert.That(provider.CreateCount, Is.Zero);
            Assert.That(b.IsCompleted || c.IsCompleted, Is.False);
            provider.Pending[0].Complete();
            await Task.WhenAll(a, b, c);
            Assert.That(opened, Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(provider.CreateAsyncCount, Is.Zero);
            Assert.That(provider.Leases.Select(lease => lease.View.Order), Is.Ordered);
            Assert.That(routes.Presenters.Last().FocusCount, Is.EqualTo(1));
        }

        [Test]
        public async Task SynchronousOpenBypassesWaitingAsyncOpen()
        {
            var a = navigator.OpenAsync(Route("A")).AsTask();
            provider.RequirePrepared = false;
            navigator.Open(Route("B"));
            Assert.That(opened, Is.EqualTo(new[] { "B" }));
            provider.RequirePrepared = true;
            provider.Pending[0].Complete();
            await a;
            Assert.That(opened, Is.EqualTo(new[] { "B", "A" }));
        }

        [Test]
        public async Task OpenCompletionAndNextPresentationDoNotWaitForAnimation()
        {
            var enter = new TestTransitionProvider();
            var a = navigator.OpenAsync(Route("A", new RoutePolicy(enterTransitionProvider: enter))).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            var first = await a;
            var second = await b;
            Assert.That(enter.Transition.IsPlaying, Is.True);
            Assert.That(opened, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(navigator.GetState(first), Is.EqualTo(ViewState.Covered));
            Assert.That(provider.Leases[0].View.Interactable, Is.False);
            enter.Transition.Complete();
            await Task.Yield();
            Assert.That(provider.Leases[0].View.Interactable, Is.False,
                "被覆盖页面不能被旧进入动画重新激活");
            navigator.Close(second);
            Assert.That(navigator.GetState(first), Is.EqualTo(ViewState.Open));
            Assert.That(provider.Leases[0].View.Interactable, Is.True);
            Assert.That(routes.Presenters[1].FocusCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RevealedPageAcceptsInputWhileItsOriginalEnterAnimationIsRunning()
        {
            var enter = new TestTransitionProvider();
            var a = navigator.OpenAsync(Route("A", new RoutePolicy(enterTransitionProvider: enter))).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            var first = await a;
            var second = await b;

            navigator.Close(second);
            Assert.That(navigator.GetState(first), Is.EqualTo(ViewState.Open));
            Assert.That(enter.Transition.IsPlaying, Is.True);
            Assert.That(provider.Leases[0].View.Interactable, Is.True);

            enter.Transition.Complete();
            await Task.Yield();
            Assert.That(provider.Leases[0].View.Interactable, Is.True);
        }

        [Test]
        public async Task DependenciesPrepareTogetherAndCommitInsideTheirParentRequest()
        {
            var before = Route("Before");
            var after = Route("After");
            var parent = Route("Parent", new RoutePolicy(dependencies: new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => before),
                new RouteDependency(DependencyTiming.AttachedAfter, () => after),
            }));
            var a = navigator.OpenAsync(parent).AsTask();
            var b = navigator.OpenAsync(Route("Next")).AsTask();
            Assert.That(provider.Pending.Count, Is.EqualTo(4));
            foreach (var pending in provider.Pending.AsEnumerable().Reverse())
            {
                pending.Complete();
            }
            var handle = await a;
            await b;
            Assert.That(opened, Is.EqualTo(new[] { "Before", "Parent", "After", "Next" }));
            navigator.Close(handle);
            Assert.That(provider.Leases.Take(3).All(lease => lease.Disposed), Is.True);
            Assert.That(routes.Presenters[0].FocusCount, Is.Zero);
            Assert.That(routes.Presenters[2].FocusCount, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(1)]
        public async Task CancellationSkipsRequestWithoutWaitingForItsLoader(int cancelledIndex)
        {
            using var cancellation = new CancellationTokenSource();
            var a = navigator.OpenAsync(Route("A"), cancellationToken: cancelledIndex == 0 ? cancellation.Token : default).AsTask();
            var b = navigator.OpenAsync(Route("B"), cancellationToken: cancelledIndex == 1 ? cancellation.Token : default).AsTask();
            var survivorIndex = 1 - cancelledIndex;
            provider.Pending[survivorIndex].Complete();
            cancellation.Cancel();
            var cancelled = cancelledIndex == 0 ? a : b;
            await ExpectFailureAsync<OperationCanceledException>(cancelled);
            Assert.That(cancelled.IsCanceled, Is.True);
            await (cancelledIndex == 0 ? b : a);
            provider.Pending[cancelledIndex].Complete();
            Assert.That(opened, Is.EqualTo(new[] { cancelledIndex == 0 ? "B" : "A" }));
            Assert.That(provider.Pending[cancelledIndex].Lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CancelledNonHeadRequestIsRemovedImmediately()
        {
            using var cancellation = new CancellationTokenSource();
            var head = navigator.OpenAsync(Route("Head")).AsTask();
            var cancelled = navigator.OpenAsync(Route("Cancelled"), cancellationToken: cancellation.Token).AsTask();

            cancellation.Cancel();
            await ExpectFailureAsync<OperationCanceledException>(cancelled);
            Assert.That(GetPendingOpenCount(navigator), Is.EqualTo(1));

            provider.Pending[0].Complete();
            await head;
            provider.Pending[1].Complete();
            Assert.That(provider.Pending[1].Lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadFailureReleasesQueueAndDoesNotCreateFailedPage()
        {
            var a = navigator.OpenAsync(Route("A")).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Fail();
            await ExpectFailureAsync<InvalidOperationException>(a);
            await b;
            Assert.That(opened, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public async Task OpenFailureRollsBackDependenciesAndAllowsNextRequest()
        {
            var shared = Route("Shared");
            var bad = routes.Create("Bad", new RoutePolicy(dependencies: new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => shared),
            }), () => throw new InvalidOperationException("测试 OnOpen 失败"));
            var a = navigator.OpenAsync(bad).AsTask();
            var b = navigator.OpenAsync(Route("Next")).AsTask();
            foreach (var pending in provider.Pending.AsEnumerable().Reverse())
            {
                pending.Complete();
            }
            await ExpectFailureAsync<InvalidOperationException>(a);
            await b;
            Assert.That(provider.Leases.Take(2).All(lease => lease.Disposed), Is.True);
            Assert.That(opened.Last(), Is.EqualTo("Next"));
        }

        [Test]
        public async Task DuplicateSingleInstanceRequestsReuseAtCommitAndReleaseUnusedPreload()
        {
            var route = Route("Single");
            var a = navigator.OpenAsync(route).AsTask();
            var b = navigator.OpenAsync(route).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            Assert.That(await a, Is.EqualTo(await b));
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(opened.Count, Is.EqualTo(1));
            Assert.That(provider.Pending[1].Lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task MultipleInstanceRequestsKeepIndependentHandles()
        {
            var route = Route("Multiple", new RoutePolicy(allowMultiple: true));
            var a = navigator.OpenAsync(route).AsTask();
            var b = navigator.OpenAsync(route).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            Assert.That(await a, Is.Not.EqualTo(await b));
            Assert.That(provider.CreateCount, Is.EqualTo(2));
        }

        [Test]
        public async Task CacheReuseGetsNewHandleAndReleasesUnusedPreparedResource()
        {
            var route = Route("Cached", new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            provider.RequirePrepared = false;
            var old = navigator.Open(route);
            navigator.Close(old);
            provider.RequirePrepared = true;
            var next = navigator.OpenAsync(route).AsTask();
            provider.Pending.Single().Complete();
            Assert.That(await next, Is.Not.EqualTo(old));
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(provider.Pending.Single().Lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ShutdownCancelsQueueAndDisposesLateResources()
        {
            var a = navigator.OpenAsync(Route("A")).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            provider.Pending[1].Complete();
            navigator.Shutdown();
            await ExpectFailureAsync<OperationCanceledException>(a);
            await ExpectFailureAsync<OperationCanceledException>(b);
            Assert.That(a.IsCanceled && b.IsCanceled, Is.True);
            provider.Pending[0].Complete();
            Assert.That(provider.CreateCount, Is.Zero);
            Assert.That(provider.Pending.All(pending => pending.Lease.DisposeCount == 1), Is.True);
        }

        [Test]
        public async Task PendingRequestUsesCapturedProviderAfterProviderReplacement()
        {
            var a = navigator.OpenAsync(Route("A")).AsTask();
            var replacement = new TestViewProvider { RequirePrepared = true };
            navigator.SetViewProvider(replacement);
            provider.Pending[0].Complete();
            await a;
            Assert.That(provider.CreateCount, Is.EqualTo(1));
            Assert.That(replacement.CreateCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task StandalonePreloadRejectsLateResultAfterInvalidation(bool shutdown)
        {
            var request = navigator.PreloadAsync(Route("Preload")).AsTask();
            if (shutdown)
            {
                navigator.Shutdown();
            }
            else
            {
                navigator.SetViewProvider(new TestViewProvider());
            }

            provider.Pending.Single().Complete();
            await ExpectFailureAsync<OperationCanceledException>(request);
            Assert.That(provider.Pending.Single().Lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ReentrantAsyncOpenIsQueuedAfterAlreadyRegisteredRequests()
        {
            Task<ViewHandle> nested = null;
            var c = Route("C");
            var a = navigator.OpenAsync(routes.Create("A", onOpen: () =>
            {
                opened.Add("A");
                nested = navigator.OpenAsync(c).AsTask();
            })).AsTask();
            var b = navigator.OpenAsync(Route("B")).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            provider.Pending[2].Complete();
            await Task.WhenAll(a, b, nested);
            Assert.That(opened, Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public async Task BindingFailureDoesNotLeakListenersOrBlockQueue()
        {
            FailingBindingContext.Reset();
            var bad = GeneratedRouteFactory.Create<TestViewModel>("BadBinding", typeof(TestPresenter),
                () => new TestViewModel(), (view, vm) => new FailingBindingContext(view, vm),
                () => new TestPresenter(), RoutePolicy.Default);
            var failed = navigator.OpenAsync(bad).AsTask();
            var next = navigator.OpenAsync(Route("Next")).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            await ExpectFailureAsync<InvalidOperationException>(failed);
            await next;
            Assert.That(FailingBindingContext.ActiveListeners, Is.Zero);
            Assert.That(provider.Leases.First().Disposed, Is.True);
            Assert.That(provider.Pending[0].Lease.Transferred, Is.True);
        }

        [Test]
        public async Task AttachedFailurePreservesExistingSharedPageAndRestoresFocus()
        {
            var shared = Route("Shared");
            provider.RequirePrepared = false;
            var sharedHandle = navigator.Open(shared);
            provider.RequirePrepared = true;
            var attached = routes.Create("BadAttached", onOpen: () => throw new InvalidOperationException("测试附属页面失败"));
            var parent = Route("Parent", new RoutePolicy(dependencies: new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => shared),
                new RouteDependency(DependencyTiming.AttachedAfter, () => attached),
            }));
            var failed = navigator.OpenAsync(parent, options: OpenOptions.Replace).AsTask();
            foreach (var pending in provider.Pending.AsEnumerable().Reverse())
            {
                pending.Complete();
            }
            await ExpectFailureAsync<InvalidOperationException>(failed);
            Assert.That(navigator.IsAlive(sharedHandle), Is.True);
            Assert.That(navigator.GetState(sharedHandle), Is.EqualTo(ViewState.Open));
            Assert.That(provider.Leases.Skip(1).All(lease => lease.Disposed), Is.True);
            Assert.That(provider.Leases[0].Disposed, Is.False);
        }

        [Test]
        public async Task PreparedLeaseIsConsumedWithoutSecondPreload()
        {
            var route = Route("Prepared");
            provider.ControlPreloads = false;
            await navigator.PreloadAsync(route);
            var preload = provider.LastPreloadLease;
            provider.ControlPreloads = true;
            await navigator.OpenAsync(route);
            Assert.That(provider.PreloadCount, Is.EqualTo(1));
            Assert.That(preload.Transferred, Is.True);
        }

        [Test]
        public async Task QueuedRebindPreservesHandleAndUsesLatestViewModel()
        {
            var route = Route("Rebind");
            var firstModel = new TestViewModel();
            var secondModel = new TestViewModel();
            var a = navigator.OpenAsyncWithViewModel(route, firstModel).AsTask();
            var b = navigator.OpenAsyncWithViewModel(route, secondModel, options: OpenOptions.Rebind).AsTask();
            provider.Pending[1].Complete();
            provider.Pending[0].Complete();
            var handle = await a;
            Assert.That(await b, Is.EqualTo(handle));
            Assert.That(navigator.TryGetViewModel<TestViewModel>(handle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(secondModel));
        }

        [Test]
        public async Task ReusedPageFailureRestoresViewModelFocusOrderAndInteraction()
        {
            provider.ControlPreloads = false;
            provider.RequirePrepared = false;
            var target = Route("Target");
            var current = Route("Current", new RoutePolicy(coverageMode: CoverageMode.KeepVisible));
            var originalModel = new TestViewModel();
            var replacementModel = new TestViewModel();
            var targetHandle = navigator.OpenWithViewModel(target, originalModel);
            var currentHandle = navigator.Open(current);
            routes.Presenters[1].CoveredAction = () => throw new InvalidOperationException("测试焦点提交失败");
            var targetOrder = provider.Leases[0].View.Order;
            var currentOrder = provider.Leases[1].View.Order;

            provider.ControlPreloads = true;
            provider.RequirePrepared = true;
            var request = navigator.OpenAsyncWithViewModel(target, replacementModel, options: OpenOptions.Rebind).AsTask();
            provider.Pending.Single().Complete();
            await ExpectFailureAsync<InvalidOperationException>(request);

            Assert.That(navigator.TryGetViewModel<TestViewModel>(targetHandle, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(originalModel));
            Assert.That(navigator.GetState(targetHandle), Is.EqualTo(ViewState.Covered));
            Assert.That(navigator.GetState(currentHandle), Is.EqualTo(ViewState.Open));
            Assert.That(provider.Leases[0].View.Order, Is.EqualTo(targetOrder));
            Assert.That(provider.Leases[1].View.Order, Is.EqualTo(currentOrder));
            Assert.That(provider.Leases[0].View.Interactable, Is.False);
            Assert.That(provider.Leases[1].View.Interactable, Is.True);
        }

        [Test]
        public async Task DependencyCycleFailsBeforeStartingResourceLoading()
        {
            Route<TestViewModel> cyclic = null;
            cyclic = Route("Cycle", new RoutePolicy(dependencies: new[]
            {
                new RouteDependency(DependencyTiming.RequiredBefore, () => cyclic),
            }));
            var request = navigator.OpenAsync(cyclic).AsTask();
            await ExpectFailureAsync<InvalidOperationException>(request);
            Assert.That(provider.PreloadCount, Is.Zero);
        }

        [Test]
        public async Task ShutdownFromOnOpenCannotCommitOrReviveRequest()
        {
            var request = navigator.OpenAsync(routes.Create("ShutdownInsideOpen", onOpen: navigator.Shutdown)).AsTask();
            provider.Pending.Single().Complete();
            await ExpectFailureAsync<OperationCanceledException>(request);
            Assert.That(request.IsCanceled, Is.True);
            Assert.That(provider.Leases.All(lease => lease.Disposed), Is.True);
        }
    }
}
