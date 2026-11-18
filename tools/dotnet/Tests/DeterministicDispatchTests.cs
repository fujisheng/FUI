using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FUI.Navigation;
using FUI.Resources;
using FUI.Rendering;
using FUI.Tests;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class DeterministicDispatchTests
    {
        [Test]
        public void WorkerCompletionCannotPresentUntilCapturedThreadPumps()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var provider = new ThreadCheckedProvider(dispatcher);
                var navigator = new Navigator(provider, dispatcher);
                navigator.Initialize();
                try
                {
                    var request = navigator.OpenAsync(new RouteHarness().Create("Worker")).AsTask();
                    RunOnWorker(() => provider.Pending[0].Complete());
                    Assert.That(request.IsCompleted, Is.False);
                    Assert.That(provider.Created, Is.Empty);
                    Assert.That(context.PendingCount, Is.GreaterThan(0));
                    context.Drain();
                    Assert.That(request.IsCompletedSuccessfully, Is.True);
                    Assert.That(provider.Created.Single().Thread, Is.EqualTo(context.ThreadId));
                    Assert.That(navigator.IsAlive(request.Result), Is.True);
                }
                finally { navigator.Shutdown(); }
                Assert.That(provider.Created.Single().DisposeCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void WorkerCancellationAndLateLeaseReleaseCommitOnCapturedThread()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var provider = new ThreadCheckedProvider(dispatcher);
                var navigator = new Navigator(provider, dispatcher);
                navigator.Initialize();
                using (var cancellation = new CancellationTokenSource())
                {
                    try
                    {
                        var routes = new RouteHarness();
                        var cancelled = navigator.OpenAsync(routes.Create("Cancelled"), cancellationToken: cancellation.Token).AsTask();
                        var survivor = navigator.OpenAsync(routes.Create("Survivor")).AsTask();
                        RunOnWorker(() => cancellation.Cancel());
                        Assert.That(cancelled.IsCompleted, Is.False);
                        context.Drain();
                        Assert.That(cancelled.IsCanceled, Is.True);

                        RunOnWorker(() => provider.Pending[1].Complete());
                        context.Drain();
                        Assert.That(survivor.IsCompletedSuccessfully, Is.True);
                        RunOnWorker(() => provider.Pending[0].Complete());
                        Assert.That(provider.Pending[0].Lease.DisposeCount, Is.Zero);
                        context.Drain();
                        Assert.That(provider.Pending[0].Lease.DisposeCount, Is.EqualTo(1));
                        Assert.That(provider.Created.Select(item => item.Name), Is.EqualTo(new[] { "Survivor" }));
                    }
                    finally { navigator.Shutdown(); }
                }
                Assert.That(provider.Created.Single().DisposeCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void FailedMainThreadCommitReleasesDependencyAndDoesNotBlockNext()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var provider = new ThreadCheckedProvider(dispatcher);
                var navigator = new Navigator(provider, dispatcher);
                navigator.Initialize();
                try
                {
                    var routes = new RouteHarness();
                    var dependency = routes.Create("Dependency");
                    var parent = routes.Create("Bad", new RoutePolicy(dependencies: new[]
                    {
                        new RouteDependency(DependencyTiming.RequiredBefore, () => dependency),
                    }), () => throw new InvalidOperationException("Expected lifecycle failure"));
                    var failed = navigator.OpenAsync(parent).AsTask();
                    var next = navigator.OpenAsync(routes.Create("Next")).AsTask();
                    RunOnWorker(() =>
                    {
                        foreach (var pending in provider.Pending.AsEnumerable().Reverse()) pending.Complete();
                    });
                    Assert.That(provider.Created, Is.Empty);
                    context.Drain();
                    Assert.That(failed.IsFaulted, Is.True);
                    Assert.That(failed.Exception.GetBaseException(), Is.TypeOf<InvalidOperationException>());
                    Assert.That(next.IsCompletedSuccessfully, Is.True);
                    Assert.That(provider.Created.Take(2).Select(item => item.DisposeCount), Is.EqualTo(new[] { 1, 1 }));
                    Assert.That(provider.Created.Last().Name, Is.EqualTo("Next"));
                }
                finally { navigator.Shutdown(); }
                Assert.That(provider.Created.All(item => item.DisposeCount == 1), Is.True);
            });
        }

        [Test]
        public void FailedFocusCommitRestoresExistingPageOnCapturedThread()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var provider = new ThreadCheckedProvider(dispatcher);
                var navigator = new Navigator(provider, dispatcher);
                navigator.Initialize();
                try
                {
                    var previous = new RouteHarness();
                    var opened = navigator.OpenAsync(previous.Create("Previous")).AsTask();
                    RunOnWorker(() => provider.Pending[0].Complete());
                    context.Drain();
                    Assert.That(opened.IsCompletedSuccessfully, Is.True);
                    var failing = new RouteHarness();
                    var route = failing.Create("BadFocus", new RoutePolicy(coverageMode: CoverageMode.Hide),
                        () => failing.Presenters.Single().FocusAction = () => throw new InvalidOperationException("Expected focus failure"));
                    var rejected = navigator.OpenAsync(route).AsTask();
                    RunOnWorker(() => provider.Pending[1].Complete());
                    context.Drain();
                    Assert.That(rejected.IsFaulted, Is.True);
                    Assert.That(navigator.GetState(opened.Result), Is.EqualTo(ViewState.Open));
                    Assert.That(provider.Created[0].View.Visible, Is.True);
                    Assert.That(provider.Created[0].View.Interactable, Is.True);
                    Assert.That(previous.Presenters.Single().CoveredCount, Is.EqualTo(1));
                    Assert.That(previous.Presenters.Single().FocusCount, Is.EqualTo(2));
                    Assert.That(provider.Created[0].DisposeCount, Is.Zero);
                    Assert.That(provider.Created[1].DisposeCount, Is.EqualTo(1));
                }
                finally { navigator.Shutdown(); }
                Assert.That(provider.Created.All(item => item.DisposeCount == 1), Is.True);
            });
        }

        [Test]
        public void ActualDispatcherRejectsWorkerMutationAndSwitchesOnlyWhenPumped()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                Task switched = null;
                var completedOn = 0;
                RunOnWorker(() =>
                {
                    Assert.That(dispatcher.IsMainThread, Is.False);
                    Assert.Throws<InvalidOperationException>(() => dispatcher.AssertMainThread());
                    switched = Switch();
                });
                Assert.That(switched.IsCompleted, Is.False);
                context.Drain();
                SpinWait.SpinUntil(() => switched.IsCompleted, 5000);
                TestContext.WriteLine("Switch status: " + switched.Status + "; continuation thread: " + completedOn +
                    "; captured thread: " + context.ThreadId + "; error: " + switched.Exception?.GetBaseException());
                Assert.That(switched.IsCompletedSuccessfully, Is.True);
                Assert.That(completedOn, Is.EqualTo(context.ThreadId));

                async Task Switch()
                {
                    await dispatcher.SwitchToMainThreadAsync();
                    completedOn = Environment.CurrentManagedThreadId;
                    dispatcher.AssertMainThread();
                }
            });
        }

        [Test]
        public void SwitchIgnoresWorkerSchedulingContextButPreservesAsyncLocal()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var local = new AsyncLocal<string>();
                local.Value = "owner";
                Task switched = null;
                var completedOn = 0;
                string value = null;
                RunOnWorker(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new RejectWorkerPostContext());
                    local.Value = "worker";
                    switched = Switch();
                });
                context.Drain();
                Assert.That(switched.IsCompletedSuccessfully, Is.True);
                Assert.That(completedOn, Is.EqualTo(context.ThreadId));
                Assert.That(value, Is.EqualTo("worker"));
                Assert.That(local.Value, Is.EqualTo("owner"));
                Assert.That(SynchronizationContext.Current, Is.SameAs(context));

                async Task Switch()
                {
                    await dispatcher.SwitchToMainThreadAsync();
                    completedOn = Environment.CurrentManagedThreadId;
                    value = local.Value;
                }
            });
        }

        [Test]
        public void SafeAwaiterFlowsAsyncLocalInsideCapturedSynchronizationContext()
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                var local = new AsyncLocal<string>();
                local.Value = "owner";
                var callbacks = 0;
                RunOnWorker(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new RejectWorkerPostContext());
                    local.Value = "worker";
                    var awaiter = dispatcher.SwitchToMainThreadAsync().GetAwaiter();
                    awaiter.OnCompleted(() =>
                    {
                        awaiter.GetResult();
                        Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(context.ThreadId));
                        Assert.That(SynchronizationContext.Current, Is.SameAs(context));
                        Assert.That(local.Value, Is.EqualTo("worker"));
                        callbacks++;
                    });
                });
                Assert.That(callbacks, Is.Zero);
                context.Drain();
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(SynchronizationContext.Current, Is.SameAs(context));
                Assert.That(local.Value, Is.EqualTo("owner"));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationBeforePumpIsObservedOnCapturedThread(bool configureAwaitFalse)
        {
            InContext(context =>
            {
                var dispatcher = new UnityMainThreadDispatcher();
                using (var cancellation = new CancellationTokenSource())
                {
                    Task switched = null;
                    var observedOn = 0;
                    RunOnWorker(() => switched = Switch());
                    cancellation.Cancel();
                    Assert.That(switched.IsCompleted, Is.False);
                    context.Drain();
                    Assert.That(switched.IsCanceled, Is.True);
                    Assert.That(observedOn, Is.EqualTo(context.ThreadId));

                    async Task Switch()
                    {
                        try
                        {
                            if (configureAwaitFalse)
                                await dispatcher.SwitchToMainThreadAsync(cancellation.Token).ConfigureAwait(false);
                            else
                                await dispatcher.SwitchToMainThreadAsync(cancellation.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            observedOn = Environment.CurrentManagedThreadId;
                            throw;
                        }
                    }
                }
            });
        }

        sealed class RejectWorkerPostContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object state)
                => throw new InvalidOperationException("The switch must target the captured context.");
        }

        static void InContext(Action<PumpContext> action)
        {
            var previous = SynchronizationContext.Current;
            var context = new PumpContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try { action(context); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        static void RunOnWorker(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception error) { failure = error; }
            });
            thread.IsBackground = true;
            thread.Start();
            Assert.That(thread.Join(5000), Is.True, "后台步骤未结束");
            if (failure != null) throw new AssertionException("后台步骤失败", failure);
        }

        sealed class PumpContext : SynchronizationContext
        {
            readonly ConcurrentQueue<(SendOrPostCallback Callback, object State)> queue = new ConcurrentQueue<(SendOrPostCallback, object)>();
            public int ThreadId { get; } = Environment.CurrentManagedThreadId;
            public int PendingCount => queue.Count;
            public override void Post(SendOrPostCallback callback, object state) => queue.Enqueue((callback, state));
            public void Drain()
            {
                Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(ThreadId));
                var count = 0;
                while (queue.TryDequeue(out var work))
                {
                    Assert.That(++count, Is.LessThan(1000), "消息队列未收敛");
                    work.Callback(work.State);
                }
            }
        }

        sealed class ThreadCheckedProvider : IViewProvider
        {
            readonly IMainThreadDispatcher dispatcher;
            public List<PendingLoad> Pending { get; } = new List<PendingLoad>();
            public List<OwnedView> Created { get; } = new List<OwnedView>();
            public ThreadCheckedProvider(IMainThreadDispatcher dispatcher) { this.dispatcher = dispatcher; }
            public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
            {
                dispatcher.AssertMainThread();
                var ready = (OwnedPreload)preload;
                Assert.That(ready.Route, Is.EqualTo(route));
                Assert.That(ready.DisposeCount, Is.Zero);
                ready.Transferred = true;
                var view = new OwnedView(route.AssetKey, dispatcher);
                Created.Add(view);
                return view;
            }
            public ValueTask<IViewLease> CreateAsync(RouteDescriptor route, IPreloadLease preload, CancellationToken token)
                => new ValueTask<IViewLease>(Create(route, preload));
            public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
            {
                dispatcher.AssertMainThread();
                token.ThrowIfCancellationRequested();
                var pending = new PendingLoad(new OwnedPreload(route, dispatcher));
                Pending.Add(pending);
                return new ValueTask<IPreloadLease>(pending.Completion.Task);
            }
        }

        sealed class PendingLoad
        {
            public OwnedPreload Lease { get; }
            public TaskCompletionSource<IPreloadLease> Completion { get; } = new TaskCompletionSource<IPreloadLease>();
            public PendingLoad(OwnedPreload lease) { Lease = lease; }
            public void Complete() => Completion.SetResult(Lease);
        }

        sealed class OwnedPreload : IPreloadLease
        {
            readonly IMainThreadDispatcher dispatcher;
            public RouteDescriptor Route { get; }
            public bool Transferred { get; set; }
            public int DisposeCount { get; private set; }
            public OwnedPreload(RouteDescriptor route, IMainThreadDispatcher dispatcher) { Route = route; this.dispatcher = dispatcher; }
            public void Dispose()
            {
                dispatcher.AssertMainThread();
                if (!Transferred) DisposeCount++;
            }
        }

        sealed class OwnedView : IViewLease
        {
            readonly IMainThreadDispatcher dispatcher;
            public string Name { get; }
            public int Thread { get; } = Environment.CurrentManagedThreadId;
            public int DisposeCount { get; private set; }
            public IView View { get; }
            public IAssetScope Assets { get; } = new TestAssetScope();
            public OwnedView(string name, IMainThreadDispatcher dispatcher) { Name = name; this.dispatcher = dispatcher; View = new TestView(name); }
            public void Dispose()
            {
                dispatcher.AssertMainThread();
                DisposeCount++;
                View.Destroy();
                Assets.Dispose();
            }
        }
    }
}
