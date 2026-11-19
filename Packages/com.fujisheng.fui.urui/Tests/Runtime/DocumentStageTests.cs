using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Resources;
using NUnit.Framework;
using UnityEngine;
using global::URUI.Elements;
using global::URUI.Events;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class DocumentStageTests
    {
        GameObject root;
        GameObject camera;
        Stage stage;
        static string Page(string body, string css = "") => "<!doctype html><html><head><style>body{margin:0;}button{width:80px;height:30px;}" + css + "</style></head><body>" + body + "</body></html>";
        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("Stage camera", typeof(Camera));
            camera.tag = "MainCamera";
            root = new GameObject("Independent document Stage");
            stage = root.AddComponent<Stage>();
            stage.Surface.Input.UseUnityInput = false;
            stage.Surface.Viewport = new Vector2Int(400, 300);
        }
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(camera);
        }
        [Test]
        public void PagesAndPopupOwnIndependentDocumentsWithSameIds()
        {
            var home = stage.OpenPage(Page("<button id='same'>Home</button>"), "ui/home.html", Layer.Panel);
            var settings = stage.OpenPage(Page("<button id='same'>Settings</button>"), "ui/settings.html", Layer.Panel);
            var popup = stage.OpenPage(Page("<button id='same'>Confirm</button>"), "ui/confirm.html", Layer.Popup);
            var snapshot = stage.Pages;
            CollectionAssert.AreEquivalent(new[] { home, settings, popup }, snapshot);
            Assert.IsTrue(((IList<ViewScope>)snapshot).IsReadOnly);
            Assert.AreNotSame(home.Document, settings.Document);
            Assert.AreNotSame(settings.Document, popup.Document);
            Assert.AreSame(home.Document.Body, home.Root);
            Assert.AreNotSame(home.Document.DocumentElement, home.Root);
            Assert.AreEqual("Home", home.Find("same").Text);
            Assert.AreEqual("Settings", settings.Find("same").Text);
            Assert.AreEqual("Confirm", popup.Find("same").Text);
            stage.ClosePage(popup);
            stage.ClosePage(settings);
            Assert.IsTrue(home.Document.IsLoaded && home.Root.IsValid);
            Assert.AreEqual(1, stage.PageCount);
            CollectionAssert.AreEquivalent(new[] { home }, stage.Pages);
            Assert.AreEqual(3, snapshot.Count);
        }
        [Test]
        public void ModalInputBlocksLowerDocumentAndClosingRestoresFocus()
        {
            var home = stage.OpenPage(Page("<input id='focus' type='text' value='home'><button id='same'>Home</button>"), "ui/home.html", Layer.Panel);
            var focus = home.Find("focus");
            Assert.IsTrue(focus.Focus());
            var popup = stage.OpenPage(Page("<button id='same'>Confirm</button>"), "ui/confirm.html", Layer.Popup);
            home.Document.SynchronizeLayout();
            popup.Document.SynchronizeLayout();
            var homeClicks = 0;
            var popupClicks = 0;
            home.Find("same").RegisterCallback<ClickEvent>(_ => homeClicks++);
            popup.Find("same").RegisterCallback<ClickEvent>(_ => popupClicks++);
            Debug.Log($"T10 modal button bounds {popup.Find("same").WorldBound}");
            Click(popup.Find("same").WorldBound.center);
            Assert.AreEqual(0, homeClicks);
            Assert.AreEqual(1, popupClicks);
            stage.ClosePage(popup);
            Assert.IsTrue(focus.Focused);
            Assert.IsTrue(home.Root.IsValid && home.Document.IsLoaded);
        }
        void Click(Vector2 point)
        {
            stage.Surface.Input.SendPointer(PointerPhase.Down, point, pressedButtons: 1);
            stage.Surface.Input.SendPointer(PointerPhase.Up, point);
        }

        sealed class FocusModel : ObservableObject
        {
        }
        sealed class FocusPresenter : Presenter<FocusModel>
        {
        }
        sealed class FocusContext : BindingContext<FocusModel>
        {
            internal FocusContext(IView view, FocusModel model) : base(view, model) { }
            protected override void OnSynchronize()
            {
            }
        }
        static Route<FocusModel> FocusRoute(string name, RoutePolicy policy = null) => GeneratedRouteFactory.Create<FocusModel>(
            name, typeof(FocusPresenter), () => new FocusModel(), (view, model) => new FocusContext(view, model),
            () => new FocusPresenter(), policy ?? RoutePolicy.Default);
        sealed class FocusProvider : IViewProvider
        {
            readonly Stage stage;
            internal readonly Dictionary<string, ViewScope> pages = new();
            internal FocusProvider(Stage stage)
            {
                this.stage = stage;
            }
            public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
            {
                var scope = stage.OpenPage(Page("<input id='first'><input id='second'>"), "ui/" + route.AssetKey + ".html", route.Settings.Layer);
                pages.Add(route.AssetKey, scope);
                return new ViewLease(new View(route.AssetKey, scope), scope);
            }
            public ValueTask<IViewLease> CreateAsync(RouteDescriptor route, IPreloadLease preload, CancellationToken token)
                => new(Create(route, preload));
            public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
                => throw new NotSupportedException();
        }
        [Test]
        public void NavigationMergesProjectionCreationAndEnableLayout()
        {
            var provider = new FocusProvider(stage);
            var navigation = new Navigator(provider);
            navigation.Initialize();
            try
            {
                using var recorder = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,
                    "URUI.Native.Update", 64, Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                var handle = navigation.Open(FocusRoute("batched-opening"));
                recorder.Stop();
                Assert.That(recorder.Valid && !recorder.WrappedAround, Is.True);
                long updates = 0;
                for (var i = 0; i < recorder.Count; i++)
                {
                    updates += recorder.GetSample(i).Count;
                }
                Assert.That(updates, Is.EqualTo(1), "结构准备和首次绑定应在同一次布局中提交。");
                Assert.That(navigation.GetState(handle), Is.EqualTo(ViewState.Open));
                Assert.That(provider.pages["batched-opening"].Find("first").HasLayout, Is.True);
            }
            finally
            {
                navigation.Shutdown();
            }
        }

        [Test]
        public void NavigationFocusRoutesInitialTabPastNonFocusingOverlayAndRestoresCoveredInput()
        {
            var provider = new FocusProvider(stage);
            var navigation = new Navigator(provider);
            navigation.Initialize();
            try
            {
                navigation.Open(FocusRoute("home"));
                navigation.Open(FocusRoute("overlay", new RoutePolicy(historyMode: HistoryMode.Transient).WithFocus(false)));
                foreach (var page in provider.pages.Values)
                {
                    page.Document.SynchronizeLayout();
                }
                stage.Surface.Input.SendKey(KeyCode.Tab, true);
                Assert.IsTrue(provider.pages["home"].Find("first").Focused, "首次 Tab 应进入导航页，而非较高的普通覆盖页。");
                stage.Surface.Input.SendKey(KeyCode.Tab, true);
                Assert.IsTrue(provider.pages["home"].Find("second").Focused);
                var settings = navigation.Open(FocusRoute("settings"));
                provider.pages["settings"].Document.SynchronizeLayout();
                stage.Surface.Input.SendKey(KeyCode.Tab, true);
                Assert.IsTrue(provider.pages["settings"].Find("first").Focused);
                navigation.Close(settings);
                Assert.IsTrue(provider.pages["home"].Find("second").Focused, "关闭普通页面应恢复原输入节点。");
                stage.Surface.Input.SendText("restored");
                Assert.AreEqual("restored", ((global::URUI.Controls.TextField)provider.pages["home"].Find("second")).Value);
            }
            finally
            {
                navigation.Shutdown();
            }
        }

        [Test]
        public void DocumentFocusHonorsAutofocusVisibilityAndModalWithoutChangingAuthorAttributes()
        {
            var home = stage.OpenPage(Page("<input id='first'><input id='automatic' autofocus>"), "ui/focus.html", Layer.Panel);
            var before = home.Document.DocumentElement.GetInnerHtml();
            Assert.IsTrue(home.Document.Focus());
            Assert.IsTrue(home.Find("automatic").Focused);
            Assert.AreEqual(before, home.Document.DocumentElement.GetInnerHtml());
            var modal = stage.OpenPage(Page("<input id='modal' autofocus>"), "ui/focus-modal.html", Layer.Popup);
            Assert.IsTrue(modal.Find("modal").Focused);
            Assert.IsFalse(home.Document.Focus());
            Assert.IsTrue(modal.Find("modal").Focused);
            stage.ClosePage(modal);
            home.SetVisible(false);
            Assert.IsFalse(home.Document.Focus());
            home.SetVisible(true);
            Assert.IsTrue(home.Document.Focus());
            Assert.IsTrue(home.Find("automatic").Focused);
        }
        [Test]
        public void ViewOrderChangesDocumentPresentationAndChildScopeOnlyOwnsNodes()
        {
            var first = stage.OpenPage(Page("<button id='same'>First</button><div id='child'></div>"), "ui/first.html", Layer.Panel);
            var second = stage.OpenPage(Page("<button id='same'>Second</button>"), "ui/second.html", Layer.Panel);
            var view = new View("first", first)
            {
                Layer = (int)Layer.Panel,
                Order = 100
            };
            view.Initialize();
            var child = new ViewScope(first.Find("child"));
            child.Destroy();
            Assert.IsTrue(first.Document.IsLoaded && first.Root.IsValid);
            Assert.IsNull(first.Find("child"));
            var firstClicks = 0;
            var secondClicks = 0;
            first.Find("same").RegisterCallback<ClickEvent>(_ => firstClicks++);
            second.Find("same").RegisterCallback<ClickEvent>(_ => secondClicks++);
            first.Document.SynchronizeLayout();
            second.Document.SynchronizeLayout();
            Debug.Log($"T10 order button bounds A={first.Find("same").WorldBound} B={second.Find("same").WorldBound}");
            Click(first.Find("same").WorldBound.center);
            Assert.AreEqual(1, firstClicks);
            Assert.AreEqual(0, secondClicks);
            view.Destroy();
            Assert.IsTrue(second.Document.IsLoaded);
            Assert.AreEqual(1, stage.PageCount);
        }
        [Test]
        public void InvalidNewPageKeepsOtherDocumentsAndDoesNotConsumeScope()
        {
            var home = stage.OpenPage(Page("<button id='same'>Home</button>"), "ui/home.html", Layer.Panel);
            Assert.Throws<InvalidOperationException>(() => stage.OpenPage("<html><body><div></body></html>", "ui/invalid.html", Layer.Panel));
            Assert.IsTrue(home.Root.IsValid);
            Assert.AreEqual("Home", home.Find("same").Text);
            Assert.AreEqual(1, stage.PageCount);
        }

        sealed class Release : IDisposable
        {
            Action action;
            internal Release(Action action)
            {
                this.action = action;
            }
            public void Dispose()
            {
                var previous = action;
                action = null;
                previous?.Invoke();
            }
        }
        [Test]
        public void ConfigurationLeasesReleasePerPageAndForFailedCandidates()
        {
            var released = new List<string>();
            stage.ConfigureDocument = document =>
            {
                var uri = document.SourceUrl;
                return new Release(() => released.Add(uri));
            };
            var first = stage.OpenPage(Page("<div></div>"), "ui/first.html", Layer.Panel);
            var second = stage.OpenPage(Page("<div></div>"), "ui/second.html", Layer.Panel);
            stage.ClosePage(first);
            CollectionAssert.AreEqual(new[] { "ui/first.html" }, released);
            Assert.IsTrue(second.Document.IsLoaded);
            Assert.Throws<InvalidOperationException>(() => stage.OpenPage("<html><body></html>", "ui/failed.html", Layer.Popup));
            CollectionAssert.AreEqual(new[] { "ui/first.html", "ui/failed.html" }, released);
            stage.ClosePage(second);
            CollectionAssert.AreEqual(new[] { "ui/first.html", "ui/failed.html", "ui/second.html" }, released);
            stage.ClosePage(second);
            Assert.AreEqual(3, released.Count);
        }
        [Test]
        public void SurfaceSchedulesCompletePageCreationAfterNativeCallbackReturns()
        {
            var home = stage.OpenPage(Page("<button id='same'>Open</button>"), "ui/home.html", Layer.Panel);
            home.Document.SynchronizeLayout();
            ViewScope popup = null;
            using var click = home.Find("same").RegisterCallback<ClickEvent>(_ =>
            {
                stage.Surface.RunAfterEvent(() => popup = stage.OpenPage(Page("<button id='same'>Confirm</button>"), "ui/popup.html", Layer.Popup));
                Assert.IsNull(popup);
            });
            Click(home.Find("same").WorldBound.center);
            Assert.IsNotNull(popup);
            Assert.IsTrue(popup.Document.IsLoaded);
            Assert.AreEqual(2, stage.PageCount);
        }
        [Test]
        public void CallbackCloseRetiresDocumentBeforeReleasingConfigurationExactlyOnce()
        {
            var releases = 0;
            stage.ConfigureDocument = document => new Release(() =>
            {
                Assert.IsFalse(document.IsLoaded);
                releases++;
            });
            var page = stage.OpenPage(Page("<button id='close'>Close</button>"), "ui/close.html", Layer.Panel);
            page.Find("close").RegisterCallback<ClickEvent>(_ =>
            {
                stage.ClosePage(page);
                stage.ClosePage(page);
                Assert.AreEqual(0, releases);
                Assert.AreEqual(1, stage.PageCount);
            });
            page.Document.SynchronizeLayout();
            Click(page.Find("close").WorldBound.center);
            Assert.AreEqual(1, releases);
            Assert.AreEqual(0, stage.PageCount);
        }
    }
}
