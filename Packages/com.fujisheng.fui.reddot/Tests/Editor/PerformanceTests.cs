using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FUI.RedDot.Tests
{
    public sealed class PerformanceTests
    {
        [Test]
        public void TenThousandNodesAndOneThousandInvalidationsAllocateNothingAfterWarmup()
        {
            const int rootCount = 100;
            const int leavesPerRoot = 99;
            const int invalidationCount = 1000;
            var nodes = new RedDotNodeDefinition[rootCount + rootCount * leavesPerRoot];
            var edges = new RedDotEdgeDefinition[rootCount * leavesPerRoot];
            var rules = new MutablePerformanceRule[invalidationCount];
            var registrations = new IDisposable[invalidationCount];
            var aggregator = new CountingPerformanceAggregator();
            var nodeIndex = 0;
            var edgeIndex = 0;
            var ruleIndex = 0;
            for (var rootIndex = 0; rootIndex < rootCount; rootIndex++)
            {
                var root = RedDotKey.Root("Root" + rootIndex);
                nodes[nodeIndex++] = new RedDotNodeDefinition(root, root.Value, aggregator);
                for (var leafIndex = 0; leafIndex < leavesPerRoot; leafIndex++)
                {
                    var leaf = RedDotKey.Child(root, "Leaf" + leafIndex);
                    nodes[nodeIndex++] = new RedDotNodeDefinition(leaf, leaf.Value, aggregator);
                    edges[edgeIndex++] = new RedDotEdgeDefinition(root, leaf);
                }
            }

            using var center = new RedDotCenter();
            center.RegisterCatalog(new RedDotCatalogDefinition(nodes, edges));
            var affectedKeys = new HashSet<RedDotKey>();
            for (var rootIndex = 0; ruleIndex < invalidationCount; rootIndex++)
            {
                var root = RedDotKey.Root("Root" + rootIndex);
                affectedKeys.Add(root);
                for (var leafIndex = 0; leafIndex < leavesPerRoot && ruleIndex < invalidationCount; leafIndex++)
                {
                    var leaf = RedDotKey.Child(root, "Leaf" + leafIndex);
                    var rule = new MutablePerformanceRule();
                    rules[ruleIndex] = rule;
                    registrations[ruleIndex] = center.RegisterRule(leaf, rule);
                    affectedKeys.Add(leaf);
                    ruleIndex++;
                }
            }
            var notifications = new Dictionary<RedDotKey, int>();
            var subscriptions = new List<IDisposable>(affectedKeys.Count);
            foreach (var key in affectedKeys)
            {
                notifications.Add(key, 0);
                subscriptions.Add(center.Subscribe(key, _ => notifications[key] = notifications[key] + 1));
            }
            center.Flush();
            InvalidateAll(rules, 1);
            center.Flush();

            foreach (var key in affectedKeys) notifications[key] = 0;
            aggregator.Reset();
            InvalidateAll(rules, 2);
            var before = GC.GetAllocatedBytesForCurrentThread();
            center.Flush();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
            Assert.That(aggregator.CallCount, Is.EqualTo(affectedKeys.Count));
            foreach (var key in affectedKeys) Assert.That(notifications[key], Is.EqualTo(1), key.Value);
            for (var index = 0; index < rules.Length; index++)
            {
                Assert.That(rules[index].LastBatchEvaluationCount, Is.EqualTo(1));
                registrations[index].Dispose();
            }
            for (var index = 0; index < subscriptions.Count; index++) subscriptions[index].Dispose();
        }

        static void InvalidateAll(MutablePerformanceRule[] rules, int value)
        {
            for (var index = 0; index < rules.Length; index++) rules[index].Set(value);
        }

        sealed class MutablePerformanceRule : IRedDotRule
        {
            IRedDotInvalidator invalidator;
            int value;
            int lastValue;
            public int LastBatchEvaluationCount { get; private set; }
            public void Start(IRedDotInvalidator owner) => invalidator = owner;
            public RedDotValue Evaluate()
            {
                LastBatchEvaluationCount = value == lastValue ? LastBatchEvaluationCount + 1 : 1;
                lastValue = value;
                return new RedDotValue(value);
            }
            public void Stop() => invalidator = null;
            public void Set(int count)
            {
                value = count;
                invalidator.Invalidate();
            }
        }

        sealed class CountingPerformanceAggregator : IRedDotAggregator
        {
            readonly SumAggregator sum = new SumAggregator();
            public int CallCount { get; private set; }

            public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
            {
                CallCount++;
                return sum.Aggregate(inputs);
            }

            public void Reset()
            {
                CallCount = 0;
            }
        }
    }
}
