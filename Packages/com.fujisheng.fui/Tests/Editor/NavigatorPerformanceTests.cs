using FUI.Navigation;
using FUI.Resources;
using FUI.Transitions;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace FUI.Tests
{
    public sealed class NavigatorPerformanceTests
    {
        const int SampleCount = 30;

        [Test]
        public void CachedRouteReuseThirtyTimesDoesNotCreateViews()
        {
            var provider = new TestViewProvider();
            var manager = new Navigator(provider, new TestExecutionContext());
            manager.Initialize();
            var route = new RouteHarness().Create(
                "CachedGate",
                new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            var initial = manager.Open(route);
            manager.Close(initial);

            for (var index = 0; index < SampleCount; index++)
            {
                var handle = manager.Open(route);
                manager.Close(handle);
            }

            Assert.That(provider.CreateCount, Is.EqualTo(1));
            manager.Shutdown();
        }

        [Test, Performance]
        [Category("Performance")]
        public void ColdOpenMeasuresConstructBindingAndCommitThirtyTimes()
        {
            var provider = new TestViewProvider();
            var manager = new Navigator(provider, new TestExecutionContext());
            manager.Initialize();
            var route = new RouteHarness().Create("ColdPerformance");

            Measure.Method(() =>
                {
                    var handle = manager.Open(route);
                    manager.Close(handle);
                })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();

            Assert.That(provider.CreateCount, Is.EqualTo(35));
            manager.Shutdown();
        }

        [Test, Performance]
        [Category("Performance")]
        public void CachedRouteReuseMeasuresThirtyTimesWithoutCreatingAnotherView()
        {
            var provider = new TestViewProvider();
            var manager = new Navigator(provider, new TestExecutionContext());
            manager.Initialize();
            var route = new RouteHarness().Create(
                "CachedPerformance",
                new RoutePolicy(cacheMode: CacheMode.KeepAlive));
            var initial = manager.Open(route);
            manager.Close(initial);

            Measure.Method(() =>
                {
                    var handle = manager.Open(route);
                    manager.Close(handle);
                })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();

            Assert.That(provider.CreateCount, Is.EqualTo(1));
            manager.Shutdown();
        }
    }
}
