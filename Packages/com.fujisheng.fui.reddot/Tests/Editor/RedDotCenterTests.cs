using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace FUI.RedDot.Tests
{
    public sealed class RedDotCenterTests
    {
        [Test]
        public void SharedDescendantIsCountedOncePerIncomingPath()
        {
            var root = RedDotKey.Root("Root");
            var left = RedDotKey.Child(root, "Left");
            var right = RedDotKey.Child(root, "Right");
            var leaf = RedDotKey.Child(root, "Leaf");
            using var center = CreateCenter(
                new[] { root, left, right, leaf },
                new[]
                {
                    new RedDotEdgeDefinition(root, left),
                    new RedDotEdgeDefinition(root, right),
                    new RedDotEdgeDefinition(left, leaf),
                    new RedDotEdgeDefinition(right, leaf),
                });
            var rule = new MutableRule(2);
            using var registration = center.RegisterRule(leaf, rule);

            center.Flush();

            Assert.That(center.Get(leaf).Count, Is.EqualTo(2));
            Assert.That(center.Get(left).Count, Is.EqualTo(2));
            Assert.That(center.Get(right).Count, Is.EqualTo(2));
            Assert.That(center.Get(root).Count, Is.EqualTo(4));
        }

        [Test]
        public void SumSaturatesAtMaximumInteger()
        {
            var key = RedDotKey.Root("Saturated");
            using var center = CreateCenter(new[] { key });
            using var first = center.RegisterRule(key, new MutableRule(int.MaxValue));
            using var second = center.RegisterRule(key, new MutableRule(1));
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void FlushVisitsOnlyDirtyNodeAndItsAncestors()
        {
            var root = RedDotKey.Root("Affected");
            var changed = RedDotKey.Child(root, "Changed");
            var untouched = RedDotKey.Child(root, "Untouched");
            var changedAggregator = new CountingAggregator();
            var untouchedAggregator = new CountingAggregator();
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(
                new[]
                {
                    new RedDotNodeDefinition(root, "Affected"),
                    new RedDotNodeDefinition(changed, "Changed", changedAggregator),
                    new RedDotNodeDefinition(untouched, "Untouched", untouchedAggregator),
                },
                new[]
                {
                    new RedDotEdgeDefinition(root, changed),
                    new RedDotEdgeDefinition(root, untouched),
                }));
            var rule = new MutableRule(1);
            using var registration = center.RegisterRule(changed, rule);
            center.Flush();
            changedAggregator.Reset();
            untouchedAggregator.Reset();

            rule.Set(2);
            center.Flush();

            Assert.That(changedAggregator.CallCount, Is.EqualTo(1));
            Assert.That(untouchedAggregator.CallCount, Is.Zero);
        }

        [Test]
        public void RuleAndSourceContributionsAreIndependent()
        {
            var key = RedDotKey.Root("Inbox");
            using var center = CreateCenter(new[] { key });
            var rule = new MutableRule(2);
            var first = new MutableSource();
            var second = new MutableSource();
            using var ruleRegistration = center.RegisterRule(key, rule);
            var firstRegistration = center.RegisterSource(first);
            using var secondRegistration = center.RegisterSource(second);
            first.Publish(key, 3);
            second.Publish(key, 4);
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(9));

            firstRegistration.Dispose();
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(6));
        }

        [Test]
        public void AggregatorFailureKeepsLastCommittedState()
        {
            var key = RedDotKey.Root("Safe");
            var aggregator = new ThrowingAggregator();
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(new[]
            {
                new RedDotNodeDefinition(key, "Safe", aggregator),
            }));
            var rule = new MutableRule(3);
            using var registration = center.RegisterRule(key, rule);
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(3));

            aggregator.Throw = true;
            rule.Set(7);
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(3));
            Assert.That(center.GetDiagnosticsSnapshot()[0].LastDiagnostic.HasValue, Is.True);
        }

        [Test]
        public void CustomAggregatorCanIgnoreChildInputs()
        {
            var root = RedDotKey.Root("Custom");
            var child = RedDotKey.Child(root, "Child");
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(
                new[]
                {
                    new RedDotNodeDefinition(root, "Custom", new DirectOnlyAggregator()),
                    new RedDotNodeDefinition(child, "Child"),
                },
                new[] { new RedDotEdgeDefinition(root, child) }));
            using var rootRegistration = center.RegisterRule(root, new MutableRule(2));
            using var childRegistration = center.RegisterRule(child, new MutableRule(8));
            center.Flush();

            Assert.That(center.Get(child).Count, Is.EqualTo(8));
            Assert.That(center.Get(root).Count, Is.EqualTo(2));
        }

        [Test]
        public void RuleAndSubscriberFailuresAreIsolated()
        {
            var key = RedDotKey.Root("Failures");
            using var center = CreateCenter(new[] { key });
            var rule = new ThrowingRule(2);
            using var registration = center.RegisterRule(key, rule);
            center.Flush();
            var successfulCallbacks = 0;
            using var throwingSubscription = center.Subscribe(key, _ => throw new InvalidOperationException("Expected callback failure."));
            using var successfulSubscription = center.Subscribe(key, _ => successfulCallbacks++);
            Assert.That(successfulCallbacks, Is.EqualTo(1));

            rule.Throw = true;
            rule.Invalidate();
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(2));

            rule.Throw = false;
            rule.Set(5);
            center.Flush();
            Assert.That(center.Get(key).Count, Is.EqualTo(5));
            Assert.That(successfulCallbacks, Is.EqualTo(2));
        }

        [Test]
        public void ReentrantInvalidationRunsInNextBatch()
        {
            var key = RedDotKey.Root("Reentrant");
            using var center = CreateCenter(new[] { key });
            var rule = new ReentrantRule();
            using var registration = center.RegisterRule(key, rule);

            center.Flush();
            Assert.That(rule.EvaluationCount, Is.EqualTo(1));
            Assert.That(center.HasPendingChanges, Is.True);

            center.Flush();
            Assert.That(rule.EvaluationCount, Is.EqualTo(2));
        }

        [Test]
        public void SubscriptionIsImmediatePreciseAndIdempotentlyDisposable()
        {
            var firstKey = RedDotKey.Root("First");
            var secondKey = RedDotKey.Root("Second");
            using var center = CreateCenter(new[] { firstKey, secondKey });
            var firstRule = new MutableRule();
            var secondRule = new MutableRule();
            using var firstRegistration = center.RegisterRule(firstKey, firstRule);
            using var secondRegistration = center.RegisterRule(secondKey, secondRule);
            var states = new List<RedDotState>();
            var subscription = center.Subscribe(firstKey, states.Add);
            Assert.That(states.Count, Is.EqualTo(1));

            secondRule.Set(5);
            center.Flush();
            Assert.That(states.Count, Is.EqualTo(1));

            firstRule.Set(2);
            center.Flush();
            Assert.That(states.Count, Is.EqualTo(2));
            subscription.Dispose();
            subscription.Dispose();
            firstRule.Set(3);
            center.Flush();
            Assert.That(states.Count, Is.EqualTo(2));
        }

        [Test]
        public void DynamicNodeIsCreatedOnSubscriptionAndPrunedAfterRelease()
        {
            var root = RedDotKey.Root("Heroes");
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(
                new[] { new RedDotNodeDefinition(root, "Heroes") },
                dynamicTemplates: new[] { new RedDotDynamicTemplate(root, "Upgrade", "Hero Upgrade") }));
            var dynamicKey = RedDotKey.Dynamic(root, "Upgrade", 1001);
            Assert.That(center.Get(dynamicKey).Count, Is.Zero);
            Assert.That(center.NodeCount, Is.EqualTo(1));

            var subscription = center.Subscribe(dynamicKey, _ => { });
            Assert.That(center.NodeCount, Is.EqualTo(2));
            subscription.Dispose();
            center.Flush();
            Assert.That(center.NodeCount, Is.EqualTo(1));
        }

        [Test]
        public void CatalogRejectsCycles()
        {
            var first = RedDotKey.Root("First");
            var second = RedDotKey.Root("Second");
            using var center = new RedDotCenter();
            Assert.Throws<InvalidOperationException>(() => center.RegisterCatalog(new RedDotCatalogDefinition(
                new[]
                {
                    new RedDotNodeDefinition(first, "First"),
                    new RedDotNodeDefinition(second, "Second"),
                },
                new[]
                {
                    new RedDotEdgeDefinition(first, second),
                    new RedDotEdgeDefinition(second, first),
                })));
        }

        [Test]
        public void FailedPortStartRollsBackRegistrationsAndDynamicNodes()
        {
            var root = RedDotKey.Root("Rollback");
            var dynamicKey = RedDotKey.Dynamic(root, "Entry", 1);
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(
                new[] { new RedDotNodeDefinition(root, "Rollback") },
                dynamicTemplates: new[] { new RedDotDynamicTemplate(root, "Entry", "Entry") }));

            var failedRule = new FailingStartRule();
            Assert.Throws<InvalidOperationException>(() => center.RegisterRule(root, failedRule));
            Assert.That(failedRule.StopCount, Is.EqualTo(1));
            Assert.That(center.GetDiagnosticsSnapshot()[0].RuleCount, Is.Zero);

            Assert.Throws<InvalidOperationException>(() => center.RegisterSource(new FailingStartSource(dynamicKey)));
            center.Flush();
            Assert.That(center.NodeCount, Is.EqualTo(1));
        }

        [Test]
        public void CatalogCopiesCallerOwnedArrays()
        {
            var root = RedDotKey.Root("Immutable");
            var child = RedDotKey.Child(root, "Child");
            var nodes = new[] { new RedDotNodeDefinition(root, "Root"), new RedDotNodeDefinition(child, "Child") };
            var edges = new[] { new RedDotEdgeDefinition(root, child) };
            var parents = new[] { root };
            var templates = new[] { new RedDotDynamicTemplate(parents, "Entry", "Entry") };
            var catalog = new RedDotCatalogDefinition(nodes, edges, templates);

            nodes[0] = default;
            edges[0] = default;
            parents[0] = default;
            templates[0] = default;

            using var center = new RedDotCenter();
            center.RegisterCatalog(catalog);
            Assert.That(center.Get(root).Count, Is.Zero);
            Assert.That(center.Subscribe(RedDotKey.Dynamic(root, "Entry", 1), _ => { }), Is.Not.Null);
        }

        [Test]
        public void ChangedNodesInOneFlushShareBatchVersion()
        {
            var root = RedDotKey.Root("Batch");
            var child = RedDotKey.Child(root, "Child");
            using var center = CreateCenter(new[] { root, child }, new[] { new RedDotEdgeDefinition(root, child) });
            var rule = new MutableRule(3);
            using var registration = center.RegisterRule(child, rule);

            center.Flush();

            Assert.That(center.Get(root).BatchVersion, Is.GreaterThan(0));
            Assert.That(center.Get(root).BatchVersion, Is.EqualTo(center.Get(child).BatchVersion));
            var committedVersion = center.Get(root).BatchVersion;
            rule.Set(3);
            center.Flush();
            Assert.That(center.Get(root).BatchVersion, Is.EqualTo(committedVersion));
        }

        [Test]
        public void DiagnosticsPreserveFailuresAcrossOtherSuccessfulRules()
        {
            var key = RedDotKey.Root("Diagnostics");
            using var center = CreateCenter(new[] { key });
            var diagnostics = new List<RedDotDiagnostic>();
            center.DiagnosticReported += diagnostics.Add;
            var failing = new ThrowingRule(1) { Throw = true };
            using var failingRegistration = center.RegisterRule(key, failing);
            center.Flush();
            var first = center.GetDiagnosticsSnapshot()[0].LastDiagnostic;
            var failureBatchVersion = center.Get(key).BatchVersion;
            using var successRegistration = center.RegisterRule(key, new MutableRule(2));
            center.Flush();

            Assert.That(first.HasValue, Is.True);
            Assert.That(center.GetDiagnosticsSnapshot()[0].LastDiagnostic.Value.Owner, Is.EqualTo(nameof(ThrowingRule)));
            Assert.That(first.Value.BatchVersion, Is.EqualTo(failureBatchVersion));
            Assert.That(diagnostics.Count, Is.EqualTo(1));
        }

        [Test]
        public void DirtySnapshotCoversRulesSourcesAndNodes()
        {
            var key = RedDotKey.Root("Dirty");
            using var center = CreateCenter(new[] { key });
            var rule = new MutableRule(1);
            var ruleRegistration = center.RegisterRule(key, rule);
            Assert.That(center.GetDiagnosticsSnapshot()[0].IsDirty, Is.True);
            center.Flush();

            var source = new MutableSource();
            using var sourceRegistration = center.RegisterSource(source);
            source.Publish(key, 2);
            Assert.That(center.GetDiagnosticsSnapshot()[0].IsDirty, Is.True);
            center.Flush();

            ruleRegistration.Dispose();
            Assert.That(center.GetDiagnosticsSnapshot()[0].IsDirty, Is.True);
        }

        [Test]
        public void DiagnosticsVersionChangesOnlyWhenObservableSnapshotDataChanges()
        {
            var key = RedDotKey.Root("DiagnosticsVersion");
            using var center = CreateCenter(new[] { key });
            var catalogVersion = center.DiagnosticsVersion;

            center.GetDiagnosticsSnapshot();
            Assert.That(center.DiagnosticsVersion, Is.EqualTo(catalogVersion));

            using var registration = center.RegisterRule(key, new MutableRule(1));
            var dirtyVersion = center.DiagnosticsVersion;
            Assert.That(dirtyVersion, Is.GreaterThan(catalogVersion));

            center.Flush();
            Assert.That(center.DiagnosticsVersion, Is.GreaterThan(dirtyVersion));
        }

        [Test]
        public void SourceStopCannotPublishReentrantly()
        {
            var key = RedDotKey.Root("Stop");
            using var center = CreateCenter(new[] { key });
            var source = new ReentrantStopSource(key);
            var registration = center.RegisterSource(source);
            registration.Dispose();

            Assert.That(source.PublishWasRejected, Is.True);
            center.Flush();
            Assert.That(center.Get(key).Count, Is.Zero);
        }

        [Test]
        public void PublicAccessIsRestrictedToCreatingThread()
        {
            using var center = CreateCenter(new[] { RedDotKey.Root("Thread") });
            var exceptions = new List<Exception>();
            var thread = new Thread(() =>
            {
                try { _ = center.NodeCount; } catch (Exception exception) { exceptions.Add(exception); }
                try { center.DiagnosticReported += _ => { }; } catch (Exception exception) { exceptions.Add(exception); }
            });
            thread.Start();
            thread.Join();

            Assert.That(exceptions, Has.Count.EqualTo(2));
            Assert.That(exceptions, Has.All.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void RegistrationDisposeCanRetryOnOwnerThreadAfterWrongThreadFailure()
        {
            var key = RedDotKey.Root("RegistrationThread");
            using var center = CreateCenter(new[] { key });
            var ruleRegistration = center.RegisterRule(key, new MutableRule(1));
            var source = new MutableSource();
            var sourceRegistration = center.RegisterSource(source);
            var subscriptionRegistration = center.Subscribe(key, _ => { });
            source.Publish(key, 2);
            center.Flush();

            var exceptions = new List<Exception>();
            var thread = new Thread(() =>
            {
                TryDispose(ruleRegistration, exceptions);
                TryDispose(sourceRegistration, exceptions);
                TryDispose(subscriptionRegistration, exceptions);
            });
            thread.Start();
            thread.Join();

            Assert.That(exceptions, Has.Count.EqualTo(3));
            Assert.That(exceptions, Has.All.TypeOf<InvalidOperationException>());
            var retained = center.GetDiagnosticsSnapshot()[0];
            Assert.That(retained.RuleCount, Is.EqualTo(1));
            Assert.That(retained.SubscriberCount, Is.EqualTo(1));
            Assert.That(center.Get(key).Count, Is.EqualTo(3));

            ruleRegistration.Dispose();
            sourceRegistration.Dispose();
            subscriptionRegistration.Dispose();
            center.Flush();

            var released = center.GetDiagnosticsSnapshot()[0];
            Assert.That(released.RuleCount, Is.Zero);
            Assert.That(released.SubscriberCount, Is.Zero);
            Assert.That(center.Get(key).Count, Is.Zero);
        }

        [Test]
        public void DynamicCleanupInspectsOnlyCandidates()
        {
            var root = RedDotKey.Root("Large");
            var definitions = new RedDotNodeDefinition[10000];
            definitions[0] = new RedDotNodeDefinition(root, "Large");
            for (var index = 1; index < definitions.Length; index++)
            {
                var key = RedDotKey.Root("Static" + index);
                definitions[index] = new RedDotNodeDefinition(key, key.Value);
            }
            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(
                definitions,
                dynamicTemplates: new[] { new RedDotDynamicTemplate(root, "Entry", "Entry") }));
            var subscription = center.Subscribe(RedDotKey.Dynamic(root, "Entry", 1), _ => { });
            subscription.Dispose();

            center.Flush();

            Assert.That(center.LastPruneInspectionCount, Is.EqualTo(1));
            Assert.That(center.NodeCount, Is.EqualTo(10000));
        }

        static RedDotCenter CreateCenter(RedDotKey[] keys, RedDotEdgeDefinition[] edges = null)
        {
            var definitions = new RedDotNodeDefinition[keys.Length];
            for (var index = 0; index < keys.Length; index++) definitions[index] = new RedDotNodeDefinition(keys[index], keys[index].Value);
            var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(definitions, edges));
            return center;
        }

        static void TryDispose(IDisposable registration, List<Exception> exceptions)
        {
            try
            {
                registration.Dispose();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        sealed class MutableRule : IRedDotRule
        {
            IRedDotInvalidator invalidator;
            int count;

            public MutableRule(int count = 0) => this.count = count;
            public void Start(IRedDotInvalidator value) => invalidator = value;
            public RedDotValue Evaluate() => new RedDotValue(count);
            public void Stop() => invalidator = null;
            public void Set(int value)
            {
                count = value;
                invalidator.Invalidate();
            }
        }

        sealed class ReentrantRule : IRedDotRule
        {
            IRedDotInvalidator invalidator;
            public int EvaluationCount { get; private set; }
            public void Start(IRedDotInvalidator value) => invalidator = value;
            public RedDotValue Evaluate()
            {
                EvaluationCount++;
                if (EvaluationCount == 1) invalidator.Invalidate();
                return new RedDotValue(EvaluationCount);
            }
            public void Stop() => invalidator = null;
        }

        sealed class MutableSource : IRedDotSource
        {
            IRedDotPublisher publisher;
            public void Start(IRedDotPublisher value) => publisher = value;
            public void Stop() => publisher = null;
            public void Publish(RedDotKey key, int count) => publisher.Publish(key, new RedDotValue(count));
        }

        sealed class ThrowingAggregator : IRedDotAggregator
        {
            readonly SumAggregator sum = new SumAggregator();
            public bool Throw { get; set; }
            public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
            {
                if (Throw) throw new InvalidOperationException("Expected test failure.");
                return sum.Aggregate(inputs);
            }
        }

        sealed class DirectOnlyAggregator : IRedDotAggregator
        {
            public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
            {
                var count = 0;
                for (var index = 0; index < inputs.Length; index++)
                {
                    if (inputs[index].Kind == RedDotInputKind.Direct) count += inputs[index].Value.Count;
                }
                return new RedDotValue(count);
            }
        }

        sealed class ThrowingRule : IRedDotRule
        {
            IRedDotInvalidator invalidator;
            int count;
            public bool Throw { get; set; }
            public ThrowingRule(int count) => this.count = count;
            public void Start(IRedDotInvalidator owner) => invalidator = owner;
            public RedDotValue Evaluate()
            {
                if (Throw) throw new InvalidOperationException("Expected rule failure.");
                return new RedDotValue(count);
            }
            public void Stop() => invalidator = null;
            public void Invalidate() => invalidator.Invalidate();
            public void Set(int value)
            {
                count = value;
                invalidator.Invalidate();
            }
        }

        sealed class CountingAggregator : IRedDotAggregator
        {
            readonly SumAggregator sum = new SumAggregator();
            public int CallCount { get; private set; }
            public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
            {
                CallCount++;
                return sum.Aggregate(inputs);
            }
            public void Reset() => CallCount = 0;
        }

        sealed class FailingStartRule : IRedDotRule
        {
            public int StopCount { get; private set; }
            public void Start(IRedDotInvalidator invalidator)
            {
                invalidator.Invalidate();
                throw new InvalidOperationException("Expected start failure.");
            }
            public RedDotValue Evaluate() => default;
            public void Stop() => StopCount++;
        }

        sealed class FailingStartSource : IRedDotSource
        {
            readonly RedDotKey key;
            public FailingStartSource(RedDotKey key) => this.key = key;
            public void Start(IRedDotPublisher publisher)
            {
                publisher.Publish(key, new RedDotValue(1));
                throw new InvalidOperationException("Expected start failure.");
            }
            public void Stop() { }
        }

        sealed class ReentrantStopSource : IRedDotSource
        {
            readonly RedDotKey key;
            IRedDotPublisher publisher;
            public bool PublishWasRejected { get; private set; }

            public ReentrantStopSource(RedDotKey key) { this.key = key; }
            public void Start(IRedDotPublisher value) { publisher = value; }
            public void Stop()
            {
                try { publisher.Publish(key, 1); }
                catch (ObjectDisposedException) { PublishWasRejected = true; }
                publisher = null;
            }
        }
    }
}
