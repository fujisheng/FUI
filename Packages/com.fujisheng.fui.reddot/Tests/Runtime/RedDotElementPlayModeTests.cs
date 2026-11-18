using System.Collections.Generic;
using System.Text.RegularExpressions;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.RedDot.UGUI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FUI.RedDot.Tests
{
    public sealed class RedDotElementPlayModeTests
    {
        readonly List<GameObject> roots = new List<GameObject>();
        RedDotCenter center;

        [SetUp]
        public void SetUp()
        {
            center = new RedDotCenter();
            RedDotRuntime.Configure(center);
        }

        [TearDown]
        public void TearDown()
        {
            RedDotRuntime.Shutdown();
            center.Dispose();
            for (var index = 0; index < roots.Count; index++)
            {
                if (roots[index] != null) Object.DestroyImmediate(roots[index]);
            }
            roots.Clear();
        }

        [Test]
        public void TwoViewsSwitchKeysHideRestoreFormatAndReleaseIndependently()
        {
            var first = RedDotKey.Root("First");
            var second = RedDotKey.Root("Second");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[]
            {
                new RedDotNodeDefinition(first, "First"),
                new RedDotNodeDefinition(second, "Second"),
            }));
            var firstRule = new MutableRule();
            var secondRule = new MutableRule();
            using var firstRegistration = center.RegisterRule(first, firstRule);
            using var secondRegistration = center.RegisterRule(second, secondRule);
            var left = CreateView("Left", RedDotVisualMode.Auto);
            var right = CreateView("Right", RedDotVisualMode.Count);
            left.RedDotElement.Key.Value = first;
            right.RedDotElement.Key.Value = first;

            firstRule.Set(120);
            center.Flush();
            Assert.That(left.Visual.activeSelf, Is.True);
            Assert.That(right.Visual.activeSelf, Is.True);
            Assert.That(left.Label.gameObject.activeSelf, Is.True);
            Assert.That(left.Label.text, Is.EqualTo("99+"));
            Assert.That(right.Label.text, Is.EqualTo("99+"));

            right.RedDotElement.Key.Value = second;
            Assert.That(right.Visual.activeSelf, Is.False);
            secondRule.Set(1);
            center.Flush();
            Assert.That(right.Visual.activeSelf, Is.True);
            Assert.That(right.Label.gameObject.activeSelf, Is.True);
            Assert.That(right.Label.text, Is.EqualTo("1"));
            Assert.That(left.Label.text, Is.EqualTo("99+"));

            left.Root.SetActive(false);
            firstRule.Set(1);
            center.Flush();
            left.Root.SetActive(true);
            Assert.That(left.Visual.activeSelf, Is.True);
            Assert.That(left.Label.gameObject.activeSelf, Is.False);

            ((IView)right.View).Destroy();
            var secondSnapshot = FindSnapshot(second);
            Assert.That(secondSnapshot.SubscriberCount, Is.Zero);
            Assert.That(FindSnapshot(first).SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void ElementCreatedBeforeRuntimeConfigurationRecoversAutomatically()
        {
            RedDotRuntime.Shutdown();
            var key = RedDotKey.Root("Late");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "Late") }));
            var view = CreateView("LateView", RedDotVisualMode.Dot);
            view.RedDotElement.Key.Value = key;
            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.Zero);

            RedDotRuntime.Configure(center);

            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void ShutdownAndReconfigureMoveActiveSubscription()
        {
            var key = RedDotKey.Root("Switch");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "Switch") }));
            using var replacement = new RedDotCenter();
            replacement.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "Switch") }));
            var view = CreateView("SwitchView", RedDotVisualMode.Dot);
            view.RedDotElement.Key.Value = key;
            var rule = new MutableRule();
            using var registration = center.RegisterRule(key, rule);
            rule.Set(3);
            center.Flush();
            Assert.That(view.Visual.activeSelf, Is.True);
            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.EqualTo(1));

            RedDotRuntime.Shutdown();
            Assert.That(view.Visual.activeSelf, Is.False, "Disconnected contexts must not show the previous account.");
            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.Zero);
            RedDotRuntime.Configure(replacement);

            Assert.That(FindSnapshot(replacement, key).SubscriberCount, Is.EqualTo(1));
            RedDotRuntime.Shutdown();
        }

        [Test]
        public void InvalidElementDoesNotBlockOtherElementsDuringRuntimeConfiguration()
        {
            RedDotRuntime.Shutdown();
            var missing = RedDotKey.Root("Missing");
            var valid = RedDotKey.Root("Valid");
            var invalidView = CreateView("InvalidView", RedDotVisualMode.Dot);
            var validView = CreateView("ValidView", RedDotVisualMode.Dot);
            invalidView.RedDotElement.Key.Value = missing;
            validView.RedDotElement.Key.Value = valid;
            using var replacement = new RedDotCenter();
            replacement.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(valid, "Valid") }));

            LogAssert.Expect(LogType.Exception, new Regex("not registered by a catalog"));
            Assert.DoesNotThrow(() => RedDotRuntime.Configure(replacement));

            Assert.That(FindSnapshot(replacement, valid).SubscriberCount, Is.EqualTo(1));
            RedDotRuntime.Shutdown();
        }

        [Test]
        public void InvalidElementDoesNotBlockOtherElementsDuringScopeConfiguration()
        {
            var missing = RedDotKey.Root("ScopedMissing");
            var valid = RedDotKey.Root("ScopedValid");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[]
            {
                new RedDotNodeDefinition(missing, "ScopedMissing"),
                new RedDotNodeDefinition(valid, "ScopedValid"),
            }));
            var scopeRoot = new GameObject("Scope", typeof(RectTransform), typeof(RedDotScope));
            roots.Add(scopeRoot);
            var invalidView = CreateView("InvalidScopedView", RedDotVisualMode.Dot, scopeRoot.transform);
            var validView = CreateView("ValidScopedView", RedDotVisualMode.Dot, scopeRoot.transform);
            invalidView.RedDotElement.Key.Value = missing;
            validView.RedDotElement.Key.Value = valid;
            using var scopedCenter = new RedDotCenter();
            scopedCenter.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(valid, "ScopedValid") }));

            LogAssert.Expect(LogType.Exception, new Regex("not registered by a catalog"));
            Assert.DoesNotThrow(() => scopeRoot.GetComponent<RedDotScope>().Configure(scopedCenter));

            Assert.That(FindSnapshot(scopedCenter, valid).SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void NearestScopeIsAnExplicitIsolationBoundaryAndReparentingRebinds()
        {
            var key = RedDotKey.Root("Scoped");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "Scoped") }));
            using var outerCenter = CreateScopedCenter(key);
            using var innerCenter = CreateScopedCenter(key);

            var outerRoot = new GameObject("OuterScope", typeof(RectTransform), typeof(RedDotScope));
            roots.Add(outerRoot);
            outerRoot.GetComponent<RedDotScope>().Configure(outerCenter);
            var innerRoot = new GameObject("InnerScope", typeof(RectTransform), typeof(RedDotScope));
            roots.Add(innerRoot);
            innerRoot.transform.SetParent(outerRoot.transform, false);
            var innerScope = innerRoot.GetComponent<RedDotScope>();
            var view = CreateView("ScopedView", RedDotVisualMode.Dot, innerRoot.transform);
            view.RedDotElement.Key.Value = key;

            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.Zero);
            Assert.That(FindSnapshot(outerCenter, key).SubscriberCount, Is.Zero);
            innerScope.Configure(innerCenter);
            Assert.That(FindSnapshot(innerCenter, key).SubscriberCount, Is.EqualTo(1));

            var rule = new MutableRule();
            using var ruleRegistration = innerCenter.RegisterRule(key, rule);
            rule.Set(2);
            innerCenter.Flush();
            Assert.That(view.Visual.activeSelf, Is.True);

            innerScope.Clear();
            Assert.That(view.Visual.activeSelf, Is.False, "An empty scope must not keep the previous reader visual.");
            Assert.That(FindSnapshot(innerCenter, key).SubscriberCount, Is.Zero);
            Assert.That(FindSnapshot(outerCenter, key).SubscriberCount, Is.Zero);

            view.Root.transform.SetParent(outerRoot.transform, false);
            Assert.That(FindSnapshot(outerCenter, key).SubscriberCount, Is.EqualTo(1));
            view.Root.transform.SetParent(null, false);
            Assert.That(FindSnapshot(outerCenter, key).SubscriberCount, Is.Zero);
            Assert.That(FindSnapshot(center, key).SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyingNearestScopeRebindsElementToOuterScope()
        {
            var key = RedDotKey.Root("DestroyedScope");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "DestroyedScope") }));
            using var outerCenter = CreateScopedCenter(key);
            using var innerCenter = CreateScopedCenter(key);
            var outerRoot = new GameObject("OuterScope", typeof(RectTransform), typeof(RedDotScope));
            roots.Add(outerRoot);
            outerRoot.GetComponent<RedDotScope>().Configure(outerCenter);
            var innerRoot = new GameObject("InnerScope", typeof(RectTransform), typeof(RedDotScope));
            roots.Add(innerRoot);
            innerRoot.transform.SetParent(outerRoot.transform, false);
            var innerScope = innerRoot.GetComponent<RedDotScope>();
            innerScope.Configure(innerCenter);
            var view = CreateView("ScopedView", RedDotVisualMode.Dot, innerRoot.transform);
            view.RedDotElement.Key.Value = key;
            Assert.That(FindSnapshot(innerCenter, key).SubscriberCount, Is.EqualTo(1));

            Object.DestroyImmediate(innerScope);

            Assert.That(FindSnapshot(innerCenter, key).SubscriberCount, Is.Zero);
            Assert.That(FindSnapshot(outerCenter, key).SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void DotModeNeverEnablesCountLabel()
        {
            var key = RedDotKey.Root("DotOnly");
            center.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, "DotOnly") }));
            var rule = new MutableRule();
            using var registration = center.RegisterRule(key, rule);
            var view = CreateView("DotView", RedDotVisualMode.Dot);
            view.RedDotElement.Key.Value = key;

            rule.Set(120);
            center.Flush();

            Assert.That(view.Visual.activeSelf, Is.True);
            Assert.That(view.Label.gameObject.activeSelf, Is.False);
        }

        ViewFixture CreateView(string name, RedDotVisualMode mode, Transform parent = null)
        {
            var root = new GameObject(name, typeof(RectTransform));
            roots.Add(root);
            if (parent != null) root.transform.SetParent(parent, false);
            var view = root.AddComponent<View>();
            var elementObject = new GameObject("Notification", typeof(RectTransform));
            elementObject.transform.SetParent(root.transform, false);
            var element = elementObject.AddComponent<ProbeElement>();
            var visual = new GameObject("Visual", typeof(RectTransform));
            visual.transform.SetParent(elementObject.transform, false);
            var labelObject = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(visual.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            element.Setup(visual, label, mode);
            ((IView)view).Initialize();
            return new ViewFixture(root, view, element, visual, label);
        }

        RedDotNodeSnapshot FindSnapshot(RedDotKey key)
        {
            return FindSnapshot(center, key);
        }

        static RedDotNodeSnapshot FindSnapshot(RedDotCenter source, RedDotKey key)
        {
            var snapshots = source.GetDiagnosticsSnapshot();
            for (var index = 0; index < snapshots.Length; index++)
            {
                if (snapshots[index].Key == key) return snapshots[index];
            }
            return null;
        }

        static RedDotCenter CreateScopedCenter(RedDotKey key)
        {
            var scopedCenter = new RedDotCenter();
            scopedCenter.RegisterCatalog(new RedDotCatalogDefinition(new[] { new RedDotNodeDefinition(key, key.Value) }));
            return scopedCenter;
        }

        sealed class ProbeElement : RedDotElement
        {
            public void Setup(GameObject root, TMP_Text label, RedDotVisualMode visualMode)
            {
                visualRoot = root;
                countLabel = label;
                mode = visualMode;
                countLimit = 99;
            }
        }

        sealed class MutableRule : IRedDotRule
        {
            IRedDotInvalidator invalidator;
            int count;
            public void Start(IRedDotInvalidator owner) => invalidator = owner;
            public RedDotValue Evaluate() => new RedDotValue(count);
            public void Stop() => invalidator = null;
            public void Set(int value)
            {
                count = value;
                invalidator.Invalidate();
            }
        }

        readonly struct ViewFixture
        {
            public GameObject Root { get; }
            public View View { get; }
            public ProbeElement RedDotElement { get; }
            public GameObject Visual { get; }
            public TMP_Text Label { get; }

            public ViewFixture(GameObject root, View view, ProbeElement element, GameObject visual, TMP_Text label)
            {
                Root = root;
                View = view;
                RedDotElement = element;
                Visual = visual;
                Label = label;
            }
        }
    }
}
