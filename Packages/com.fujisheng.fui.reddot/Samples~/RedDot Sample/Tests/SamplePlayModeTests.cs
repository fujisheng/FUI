using System.Collections;
using System.Linq;
using FUI.RedDot;
using FUI.RedDot.UGUI;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Samples.RedDot.Tests
{
    /// <summary>独立Prefab与真实FUI绑定测试；最终验收另使用CLI屏幕射线点击。</summary>
    public sealed class SamplePlayModeTests
    {
        static readonly string[] ViewNames = { "GameHomeView", "BagView", "QuestView", "HeroView", "MailView", "SeasonView", "TacticalView", "TrackedStatusView" };
        GameObject host;
        GameObject events;
        GameObject home;
        Bootstrap bootstrap;
        [SetUp]
        public void SetUp()
        {
            RedDotRuntime.Shutdown();
            events = new GameObject("Sample Test EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            host = new GameObject("RedDot Sample Test Host");
            bootstrap = host.AddComponent<Bootstrap>();
            home = GameObject.Find("GameHomeView");
            Assert.That(home, Is.Not.Null);
        }
        [TearDown]
        public void TearDown()
        {
            if (host != null) { Object.DestroyImmediate(host); }
            if (events != null) { Object.DestroyImmediate(events); }
            foreach (var view in Object.FindObjectsByType<View>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ViewNames.Contains(view.name)) { Object.DestroyImmediate(view.gameObject); }
            }
            RedDotRuntime.Shutdown();
        }
        [Test]
        public void InitialAnyAndLateRuntimeConfigurationProjectToHome()
        {
            AssertDot(home, "HomeBadge", true, null);
            AssertDot(home, "BagNavBadge", true, "7");
            AssertDot(home, "QuestNavBadge", true, "3");
            AssertDot(home, "SeasonNavBadge", true, "2");
            Assert.That(Find(home, "DetailsPanel").gameObject.activeSelf, Is.False);
        }
        [Test]
        public void AllSevenPagesAreIndependentAndBackRevealsCachedHome()
        {
            var pages = new[] { "Bag", "Quest", "Hero", "Mail", "Season", "Tactical" };
            foreach (var page in pages)
            {
                Click(home, page + "Button");
                var opened = GameObject.Find(page + "View");
                Assert.That(opened, Is.Not.Null);
                Assert.That(home.activeInHierarchy, Is.False);
                Click(opened, "BackButton");
                Assert.That(home.activeInHierarchy, Is.True);
                Assert.That(opened.activeInHierarchy, Is.False);
            }
        }
        [Test]
        public void CompletingWatchMakesAnyZeroWithoutDisconnectingAndNewMailRestoresIt()
        {
            Click(home, "CompleteWatchButton"); bootstrap.FlushNow();
            Assert.That(bootstrap.Context.DefaultReader, Is.Not.Null);
            Assert.That(bootstrap.Context.Game.Center.Get(Nodes.Home).Count, Is.Zero);
            AssertDot(home, "HomeBadge", false, null);
            AssertDot(home, "SeasonNavBadge", true, "2");
            var mail = Open("Mail"); Click(mail, "ReceiveButton"); bootstrap.FlushNow();
            Click(mail, "BackButton");
            Assert.That(bootstrap.Context.Game.Center.Get(Nodes.Home).Count, Is.EqualTo(1));
            AssertDot(home, "HomeBadge", true, null);
        }
        [Test]
        public void CargoIsPendingUntilCommitAndCachedHomeRestoresLatestState()
        {
            var bag = Open("Bag");
            AssertDot(bag, "BagItemBadge", true, "5");
            Click(bag, "WithdrawButton");
            AssertDot(bag, "BagItemBadge", true, "5");
            bootstrap.FlushNow();
            AssertDot(bag, "BagItemBadge", true, "3");
            Click(bag, "StopTransportButton"); Click(bag, "UseButton"); bootstrap.FlushNow();
            AssertDot(bag, "BagItemBadge", false, null);
            Assert.That(Find(bag, "BagItemBadge").gameObject.activeInHierarchy, Is.True);
            Click(bag, "BackButton");
            AssertDot(home, "BagNavBadge", true, null);
        }
        [Test]
        public void AllVisualModesAndLimitsUseTheSameCommittedKey()
        {
            var bag = Open("Bag");
            var values = new[] { 0, 1, 2, 99, 100 };
            foreach (var count in values)
            {
                Click(bag, "PresetButton"); bootstrap.FlushNow();
                AssertDot(bag, "BagDotBadge", count > 0, null);
                AssertDot(bag, "BagCountBadge", count > 0, count == 0 ? null : count > 99 ? "99+" : count.ToString());
                AssertDot(bag, "BagItemBadge", count > 0, count <= 1 ? null : count > 99 ? "99+" : count.ToString());
                AssertDot(bag, "BagCompactBadge", count > 0, count == 0 ? null : count > 9 ? "9+" : count.ToString());
            }
            Assert.That(bootstrap.Context.Game.Center.Get(Nodes.Item("alloy")).Count, Is.EqualTo(100));
        }
        [Test]
        public void TerminalAndPageHaveIndependentSubscriptionsAndCloseReleasesOnlyTerminal()
        {
            var bag = Open("Bag");
            var key = Nodes.Item("alloy");
            var before = Subscribers(key);
            Click(bag, "TrackButton");
            var terminal = GameObject.Find("TrackedStatusView");
            Assert.That(bag.activeInHierarchy, Is.True);
            Assert.That(Subscribers(key), Is.EqualTo(before + 1));
            AssertDot(terminal, "TrackedBadge", true, "5");
            Click(bag, "WithdrawButton"); bootstrap.FlushNow();
            AssertDot(bag, "BagItemBadge", true, "3");
            AssertDot(terminal, "TrackedBadge", true, "3");
            Click(terminal, "BackButton");
            Assert.That(Subscribers(key), Is.EqualTo(before));
        }
        [Test]
        public void HotUnitKeySwitchAndDischargeRespectPinnedTerminalLifetime()
        {
            var hero = Open("Hero");
            Click(hero, "Unit2Button");
            AssertDot(hero, "HeroUpgradeBadge", false, null);
            Click(hero, "TrainButton"); bootstrap.FlushNow();
            AssertDot(hero, "HeroUpgradeBadge", false, null);
            Click(hero, "SupplyButton"); bootstrap.FlushNow();
            AssertDot(hero, "HeroUpgradeBadge", true, null);
            Click(hero, "TrackButton");
            var terminal = GameObject.Find("TrackedStatusView");
            Click(hero, "DischargeButton"); bootstrap.FlushNow();
            var key = Nodes.HeroUpgrade(1002);
            Assert.That(Subscribers(key), Is.EqualTo(1));
            AssertDot(hero, "HeroUpgradeBadge", false, null);
            AssertDot(terminal, "TrackedBadge", false, null);
            Click(terminal, "BackButton"); bootstrap.FlushNow();
            Assert.That(bootstrap.Context.Game.Center.GetDiagnosticsSnapshot().Any(x => x.Key == key), Is.False);
        }
        [Test]
        public void ShutdownHidesOldAccountAndReconnectRestoresEnabledElements()
        {
            Click(home, "DisconnectButton");
            AssertDot(home, "HomeBadge", false, null);
            AssertDot(home, "SeasonNavBadge", true, "2");
            Click(home, "ProfileButton");
            AssertDot(home, "HomeBadge", true, null);
            AssertDot(home, "BagNavBadge", true, "8");
            Click(home, "ConnectButton");
            AssertDot(home, "BagNavBadge", true, "8");
        }
        [UnityTest]
        public IEnumerator FleetScopeClearNestingReparentAndDestroyAreReal()
        {
            var page = Open("Season");
            AssertDot(page, "SeasonRewardBadge", true, "2");
            AssertDot(page, "WingBadge", true, "7");
            AssertDot(page, "DefaultFleetBadge", true, "3");
            Click(page, "FleetButton");
            AssertDot(page, "SeasonRewardBadge", true, "5");
            AssertDot(page, "WingBadge", true, "7");
            Click(page, "FleetConnectionButton");
            AssertDot(page, "SeasonRewardBadge", false, null);
            AssertDot(page, "WingBadge", true, "7");
            Click(page, "WingScopeButton"); yield return null;
            AssertDot(page, "WingBadge", false, null);
            Click(page, "WingMoveButton"); yield return null;
            AssertDot(page, "WingBadge", true, "3");
            Click(page, "WingScopeButton"); yield return null;
            AssertDot(page, "WingBadge", true, "7");
        }
        [Test]
        public void SignalDetailsAreCollapsedAndDoNotCreateObservingSubscriptions()
        {
            var hero = Open("Hero"); var count = Subscribers(Nodes.HeroUpgrade(1001));
            Click(hero, "DetailsButton");
            Assert.That(Find(hero, "DetailsPanel").gameObject.activeSelf, Is.True);
            Assert.That(Find(hero, "DetailsText").GetComponent<Text>().text, Does.Contain("Batch"));
            Assert.That(Subscribers(Nodes.HeroUpgrade(1001)), Is.EqualTo(count));
            Click(hero, "DetailsButton");
            Assert.That(Find(hero, "DetailsPanel").gameObject.activeSelf, Is.False);
        }
        [Test]
        public void CachedHomeFleetEntryRebindsAfterFleetSwitchAndDisconnect()
        {
            var page = Open("Season");
            Click(page, "FleetButton"); Click(page, "BackButton");
            AssertDot(home, "SeasonNavBadge", true, "5");
            Click(home, "SeasonButton"); Click(page, "FleetConnectionButton"); Click(page, "BackButton");
            AssertDot(home, "SeasonNavBadge", false, null);
        }
        [UnityTest]
        public IEnumerator SignalDetailsStartInsideViewportAndGrowToRealTextHeight()
        {
            var page = Open("Quest"); Click(page, "DetailsButton"); yield return null;
            var text = Find(page, "DetailsText").GetComponent<Text>();
            var scroll = Find(page, "DetailsScroll").GetComponent<ScrollRect>();
            Assert.That(text.alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(text.rectTransform.anchoredPosition.y, Is.EqualTo(-12));
            Assert.That(scroll.content.rect.height, Is.GreaterThanOrEqualTo(text.preferredHeight));
            // 内容刚好装入视口时ScrollRect归一化位置为0，不意味着内容滚到了底部。
            if (scroll.content.rect.height > scroll.viewport.rect.height + 0.5F)
            {
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.01));
            }
        }
        [Test]
        public void CargoSelectionExposesSingleKeyRemovalAndWholeSourceDisposal()
        {
            var bag = Open("Bag");
            Click(bag, "WithdrawButton"); bootstrap.FlushNow();
            Click(bag, "CargoSlot1");
            AssertDot(bag, "BagItemBadge", true, null);
            Click(bag, "StopTransportButton"); bootstrap.FlushNow();
            AssertDot(bag, "BagItemBadge", false, null);
            Click(bag, "CargoSlot0");
            AssertDot(bag, "BagItemBadge", true, "3");
        }
        [Test]
        public void GeneratedPrefabsContainFuiControlsFontsAndNoWholeScreenMaster()
        {
            foreach (var name in ViewNames)
            {
                var prefab = UnityEngine.Resources.Load<GameObject>("UI/Prefabs/" + name);
                Assert.That(prefab, Is.Not.Null, name);
                Assert.That(prefab.GetComponent<View>(), Is.Not.Null, name);
                Assert.That(prefab.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                Assert.That(prefab.GetComponentsInChildren<ButtonElement>(true).Length, Is.GreaterThan(0));
                foreach (var label in prefab.GetComponentsInChildren<Text>(true)) { Assert.That(label.font, Is.Not.Null, label.name); }
                foreach (var image in prefab.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite != null) { Assert.That(image.sprite.name.ToLowerInvariant(), Does.Not.Contain("design-master")); }
                }
            }
        }
        int Subscribers(RedDotKey key) => bootstrap.Context.Game.Center.GetDiagnosticsSnapshot().Single(x => x.Key == key).SubscriberCount;
        GameObject Open(string page) { Click(home, page + "Button"); return GameObject.Find(page + "View"); }
        void Click(GameObject root, string name)
        {
            var target = Find(root, name).gameObject;
            var pointer = new PointerEventData(events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
        static Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(x => x.name == name);
        static void AssertDot(GameObject root, string name, bool visible, string count)
        {
            var visual = Find(root, name + "Visual").gameObject;
            var label = Find(root, name + "Count").GetComponent<Text>();
            Assert.That(visual.activeInHierarchy, Is.EqualTo(visible), name);
            Assert.That(label.gameObject.activeInHierarchy, Is.EqualTo(visible && count != null), name);
            if (count != null) { Assert.That(label.text, Is.EqualTo(count), name); }
        }
    }

#if UNITY_EDITOR
    /// <summary>验证 Quick Start 场景具备独立运行所需的 Unity 基础设施。</summary>
    public sealed class QuickStartScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneProvidesCameraAndEventSystem()
        {
            RedDotRuntime.Shutdown();
            var scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath("d31b345cda084458929f80c0a5b3fb88");
            var scene = SceneManager.GetSceneByPath(scenePath);
            var loadedByTest = !scene.IsValid() || !scene.isLoaded;

            if (loadedByTest)
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    scenePath,
                    new LoadSceneParameters(LoadSceneMode.Additive));
                yield return null;
            }

            var cameraCount = 0;
            var eventSystemCount = 0;
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                cameraCount += sceneRoot.GetComponentsInChildren<Camera>(true).Length;
                eventSystemCount += sceneRoot.GetComponentsInChildren<EventSystem>(true).Length;
            }

            if (loadedByTest)
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(scene);
                while (!unloadOperation.isDone)
                {
                    yield return null;
                }
            }

            RedDotRuntime.Shutdown();
            Assert.That(cameraCount, Is.EqualTo(1));
            Assert.That(eventSystemCount, Is.EqualTo(1));
        }
    }
#endif
}
