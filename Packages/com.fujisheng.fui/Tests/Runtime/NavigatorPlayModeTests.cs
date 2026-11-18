using FUI.Navigation;
using FUI.Resources;
using FUI.Transitions;
using NUnit.Framework;
using System.Linq;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.TestTools;

namespace FUI.Tests
{
    public sealed class NavigatorPlayModeTests
    {
        [UnityTest]
        public IEnumerator WorkerCancellationAndLateLoadingReturnToUnityThread()
        {
            var provider = new TestViewProvider { ControlPreloads = true, RequirePrepared = true };
            var manager = new Navigator(provider);
            manager.Initialize();
            using var cancellation = new CancellationTokenSource();
            var thread = System.Environment.CurrentManagedThreadId;
            var presentedThread = 0;
            var cancelled = manager.OpenAsync(new RouteHarness().Create("Cancelled"),
                cancellationToken: cancellation.Token).AsTask();
            var survivor = manager.OpenAsync(new RouteHarness().Create("Survivor",
                onOpen: () => presentedThread = System.Environment.CurrentManagedThreadId)).AsTask();
            provider.Pending[1].Complete();
            var cancelWorker = Task.Run(() => cancellation.Cancel());
            var deadline = UnityEngine.Time.realtimeSinceStartup + 5f;
            while (!survivor.IsCompleted && UnityEngine.Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(cancelWorker.IsCompleted, Is.True);
            Assert.That(cancelled.IsCanceled, Is.True);
            Assert.That(survivor.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(presentedThread, Is.EqualTo(thread));
            var lateWorker = Task.Run(() => provider.Pending[0].Complete());
            deadline = UnityEngine.Time.realtimeSinceStartup + 5f;
            while ((!lateWorker.IsCompleted || provider.Pending[0].Lease.DisposeCount == 0)
                && UnityEngine.Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(provider.Pending[0].Lease.DisposeCount, Is.EqualTo(1));
            manager.Shutdown();
        }

        [Test]
        public void ShutdownReleasesActiveCachedViewsAndResourcesWithoutTransitions()
        {
            var provider = new TestViewProvider();
            var manager = new Navigator(provider, new TestExecutionContext());
            manager.Initialize();
            var keepAlive = new RouteHarness().Create(
                "PlayModeCached",
                new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            var active = new RouteHarness().Create("PlayModeActive");
            var cachedHandle = manager.Open(keepAlive);
            manager.Close(cachedHandle);
            var activeHandle = manager.Open(active);

            manager.Shutdown();

            Assert.That(manager.GetState(cachedHandle), Is.EqualTo(ViewState.Cached));
            Assert.That(manager.GetState(activeHandle), Is.EqualTo(ViewState.Destroyed));
            Assert.That(provider.Leases.All(lease => lease.Disposed), Is.True);
        }
    }
}
