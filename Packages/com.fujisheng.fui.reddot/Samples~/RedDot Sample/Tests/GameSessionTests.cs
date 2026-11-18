using System;
using System.Linq;
using FUI.RedDot;
using NUnit.Framework;

namespace FUI.Samples.RedDot.Tests
{
    /// <summary>使用页面同一套事实和适配器断言功能，不在测试中另造一套业务。</summary>
    public sealed class GameSessionTests
    {
        [Test]
        public void CargoSourcesKeepIdentityAndRemoveOnlyTheirOwnKeys()
        {
            using var game = new GameSession();
            Assert.That(game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(5));
            game.Transport.Remove("alloy");
            Assert.That(game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(5), "Pushes are pending until a batch commits.");
            game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(3));
            Assert.That(game.Center.Get(Nodes.Item("core")).Count, Is.EqualTo(1));
            game.DisconnectTransport(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Item("core")).Count, Is.Zero);
            Assert.That(game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(3));
            game.Transport.Set("alloy", 2); game.ConnectTransport(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(5));
        }

        [Test]
        public void RulesBatchAndStopIndependently()
        {
            using var game = new GameSession();
            var a = game.QuestRuleA.Evaluations; var b = game.QuestRuleB.Evaluations;
            for (var i = 0; i < 1000; i++) { game.QuestA.Resample(); game.QuestB.Resample(); }
            game.Center.Flush();
            Assert.That(game.QuestRuleA.Evaluations - a, Is.EqualTo(1));
            Assert.That(game.QuestRuleB.Evaluations - b, Is.EqualTo(1));
            game.ToggleTracking(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Quests).Count, Is.EqualTo(1));
            b = game.QuestRuleB.Evaluations; game.QuestB.Set(4); game.Center.Flush();
            Assert.That(game.QuestRuleB.Evaluations, Is.EqualTo(b));
            game.ToggleTracking(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Quests).Count, Is.EqualTo(5));
        }

        [Test]
        public void SharedRewardCountsTwoPathsAndCustomAggregationIgnoresHints()
        {
            using var game = new GameSession();
            Assert.That(game.Center.Get(Nodes.RewardRoutes).Count, Is.EqualTo(2), "Reward routes must exclude unrelated cargo and quest counts.");
            game.DisconnectTransport(); game.Production.Remove("alloy");
            game.QuestA.Set(0); game.QuestB.Set(0); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.QuestHint).Count, Is.EqualTo(4));
            Assert.That(game.Center.Get(Nodes.Quests).Count, Is.Zero);
            Assert.That(game.Center.Get(Nodes.Bag).Count, Is.EqualTo(1));
            Assert.That(game.Center.Get(Nodes.Operations).Count, Is.EqualTo(1));
            Assert.That(game.Center.Get(Nodes.Readiness).Count, Is.EqualTo(2));
            game.ClaimRewards(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Readiness).Count, Is.Zero);
            Assert.That(game.Center.Get(Nodes.RewardRoutes).Count, Is.Zero);
        }

        [Test]
        public void UnitConditionsAndLastObserverControlDynamicLifetime()
        {
            using var game = new GameSession();
            game.SelectUnit(1002); game.Unit.Train(); game.Center.Flush();
            var key = Nodes.HeroUpgrade(1002);
            Assert.That(game.Center.Get(key).Count, Is.Zero);
            game.Unit.Supply(); game.Center.Flush();
            Assert.That(game.Center.Get(key).Count, Is.EqualTo(1));
            var observer = game.Center.Subscribe(key, state => { });
            game.Discharge(); game.Center.Flush();
            Assert.That(game.Center.GetDiagnosticsSnapshot().Any(x => x.Key == key), Is.True);
            Assert.That(game.Center.Get(key).Count, Is.Zero);
            observer.Dispose(); observer.Dispose(); game.Center.Flush();
            Assert.That(game.Center.GetDiagnosticsSnapshot().Any(x => x.Key == key), Is.False);
            var count = game.Center.NodeCount;
            game.Center.Get(Nodes.HeroUpgrade(7890));
            Assert.That(game.Center.NodeCount, Is.EqualTo(count), "Get must not materialize a dynamic node.");
        }

        [Test]
        public void MailLongKeysAndMaxSourcesAreIndependent()
        {
            using var game = new GameSession();
            var key = Nodes.Message(game.SelectedMessage);
            Assert.That(game.Center.Get(key).Count, Is.EqualTo(1));
            game.ReadMessage(); game.Center.Flush();
            Assert.That(game.Center.Get(key).Count, Is.Zero);
            game.ArchiveMessage(); game.Center.Flush();
            Assert.That(game.Center.GetDiagnosticsSnapshot().Any(x => x.Key == key), Is.False);
            Assert.That(game.Center.Get(Nodes.Threat).Count, Is.EqualTo(5));
            game.DisconnectHighestThreat(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Threat).Count, Is.EqualTo(3));
        }

        [Test]
        public void FailuresRetainCorrectContributionAndRecoveryKeepsHistory()
        {
            using var game = new GameSession();
            game.FailRule(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Link).Count, Is.EqualTo(5), "Old rule contribution 2 plus healthy source 3.");
            Assert.That(game.History.Any(x => x.Kind == RedDotDiagnosticKind.RuleFailure), Is.True);
            game.FailAggregator(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Link).Count, Is.EqualTo(5));
            game.FailSubscriber();
            Assert.That(game.History.Any(x => x.Kind == RedDotDiagnosticKind.SubscriberFailure), Is.True);
            game.Recover(); game.Sensor.Set(4); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Link).Count, Is.EqualTo(7));
            Assert.That(game.History.Any(x => x.Kind == RedDotDiagnosticKind.AggregatorFailure), Is.True);
        }

        [Test]
        public void FailedSourceStartRollsBackAndStopCannotRepublish()
        {
            using var game = new GameSession();
            var previous = game.Center.Get(Nodes.Link).Count;
            Assert.That(game.TestSourceStart(), Does.Contain("start failure"));
            game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Link).Count, Is.EqualTo(previous));
            game.TestSourceStop(); game.Center.Flush();
            Assert.That(game.Center.Get(Nodes.Link).Count, Is.EqualTo(previous));
            Assert.That(game.History.Any(x => x.Kind == RedDotDiagnosticKind.SourceFailure), Is.True);
        }

        [Test]
        public void FleetGuidAndCompositeFactoriesUseSeparateWorlds()
        {
            using var a = new FleetWorld(2); using var b = new FleetWorld(5);
            Assert.That(a.Center.Get(Nodes.Season).Count, Is.EqualTo(2));
            Assert.That(b.Center.Get(Nodes.Season).Count, Is.EqualTo(5));
            a.Advance(); a.Center.Flush();
            Assert.That(a.Center.Get(Nodes.ExpeditionStage(FleetWorld.ExpeditionId, 1)).Count, Is.EqualTo(1));
            Assert.That(b.Center.Get(Nodes.Season).Count, Is.EqualTo(5));
            a.Claim(); a.Center.Flush();
            Assert.That(a.Center.Get(Nodes.Season).Count, Is.Zero);
        }

        [Test]
        public void TacticalMeasurementsUseTheActualBatchAndAtomicSnapshot()
        {
            using var simulation = new TacticalSimulation();
            Assert.That(simulation.Center.NodeCount, Is.EqualTo(10000));
            simulation.QueueRepeated(); simulation.Step();
            Assert.That(simulation.Evaluations, Is.EqualTo(1));
            Assert.That(simulation.Notifications, Is.EqualTo(2));
            simulation.QueueDistinct(); simulation.Step();
            Assert.That(simulation.Evaluations, Is.EqualTo(1000));
            Assert.That(simulation.Notifications, Is.EqualTo(1001));
            Assert.That(simulation.AtomicRead, Is.True);
            var version = simulation.Center.Get(simulation.Root).BatchVersion;
            simulation.QueueUnchanged(); simulation.Step();
            Assert.That(simulation.Notifications, Is.Zero);
            Assert.That(simulation.Center.Get(simulation.Root).BatchVersion, Is.EqualTo(version));
            Assert.That(simulation.AllocatedBytes, Is.Zero);
        }

        [Test]
        public void TacticalReentryIsNextBatchAndSaturationDoesNotOverflow()
        {
            using var simulation = new TacticalSimulation();
            simulation.QueueNextWave(); simulation.Step();
            Assert.That(simulation.Center.HasPendingChanges, Is.True);
            simulation.Step();
            Assert.That(simulation.Center.HasPendingChanges, Is.False);
            simulation.QueueRecursive(); simulation.Step();
            Assert.That(simulation.RecursiveRejected, Is.True);
            simulation.QueueSaturation(); simulation.Step();
            Assert.That(simulation.Center.Get(simulation.Root).Count, Is.EqualTo(int.MaxValue));
        }
    }
}
