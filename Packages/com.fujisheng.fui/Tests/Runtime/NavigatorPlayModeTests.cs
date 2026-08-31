using FUI.Navigation;
using FUI.Resources;
using FUI.Transitions;
using NUnit.Framework;
using System.Linq;

namespace FUI.Tests
{
    public sealed class NavigatorPlayModeTests
    {
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
