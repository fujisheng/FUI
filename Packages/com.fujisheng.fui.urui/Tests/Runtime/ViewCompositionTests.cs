using System;
using System.Collections.Generic;
using FUI.Navigation;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;
using global::URUI.Resources;
using global::URUI.Animation;
using System.Threading;
using System.Threading.Tasks;
using global::URUI.Events;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class ViewCompositionTests
    {
        GameObject root;
        GameObject camera;
        Stage stage;
        static string Html => global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/Composition").text;
        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("Composition camera", typeof(Camera));
            camera.tag = "MainCamera";
            root = new GameObject("View composition");
            stage = root.AddComponent<Stage>();
        }
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(camera);
        }

        [Test]
        public void OwnedResourcesReleaseInReverseOrderBeforeScope()
        {
            var scope = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var calls = new List<int>();
            var view = new View("owned", scope);
            view.Own(new Release(() =>
            {
                Assert.IsTrue(scope.Root.IsValid);
                calls.Add(1);
            }));
            view.Own(new Release(() =>
            {
                calls.Add(2);
                throw new InvalidOperationException("release");
            }));
            view.Own(new Release(() => calls.Add(3)));
            Assert.Throws<InvalidOperationException>(() => view.Destroy());
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, calls);
            Assert.IsFalse(scope.Root.IsValid);
            view.Destroy();
        }

        [Test]
        public void ExplicitIdAndClassSelectorsStayWithinScope()
        {
            var first = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var second = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var view = new View("local", first, new[] {
                ElementBinding.For("Title", "#title", (e, n) => new TextElement(e, n)),
                ElementBinding.For("Captions", ".caption", (e, n) => new TextElement(e, n)) });
            try
            {
                view.Initialize();
                Assert.AreEqual(3, view.Elements.Count);
                view.GetElement<TextElement>("Title").Content.Value = "changed";
                Assert.AreEqual("changed", first.Root.Q("title").Text);
                Assert.AreEqual("first", second.Root.Q("title").Text);
            }
            finally
            {
                view.Destroy();
                second.Destroy();
            }
        }

        [Test]
        public void NamedControlsResolveById()
        {
            var scope = stage.OpenPage(Html, "/composition.html", Layer.Panel);
            var definition = new ViewDefinition("standard", "/composition.html", _ => Array.Empty<ElementBinding>());
            var view = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            try
            {
                view.Initialize();
                var button = view.GetElement<IButtonElement>("open");
                Assert.That(button, Is.Not.Null);
                Assert.That(view.GetElement<IElement>("open"), Is.SameAs(button));
                Assert.That(view.GetElement<ITextElement>("open"), Is.Null);
                Assert.That(view.GetElement<ITextElement>("title").Text.Value, Is.EqualTo("first"));
                Assert.That(view.GetElement<IElement>("Open"), Is.Null);
            }
            finally
            {
                view.Destroy();
            }
        }

        [Test]
        public void ExplicitBindingOwnsItsName()
        {
            var scope = stage.OpenPage(Html, "/composition.html", Layer.Panel);
            var definition = new ViewDefinition("explicit", "/composition.html", _ => new[]
            {
                ElementBinding.For("open", "#title", (element, name) => new TextElement(element, name))
            });
            var view = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            try
            {
                view.Initialize();
                Assert.That(view.GetElement<ITextElement>("open").Text.Value, Is.EqualTo("first"));
                Assert.That(view.GetElement<IButtonElement>("open"), Is.Null);
            }
            finally
            {
                view.Destroy();
            }
        }

        [Test]
        public void ItemScopesDoNotShareBindings()
        {
            var scope = stage.OpenPage(Html, "/composition.html", Layer.Panel);
            var definition = new ViewDefinition("scoped", "/composition.html", _ => Array.Empty<ElementBinding>());
            var parent = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            var child = definition.Compose(new ViewScope(scope.Root.Q(className: "composition")), new Assets(), UnityUIClock.Unscaled);
            try
            {
                parent.Initialize();
                child.Initialize();
                Assert.That(child.GetElement<ITextElement>("title"), Is.Not.Null);
                Assert.That(parent.GetElement<ITextElement>("title"), Is.Null);
                Assert.That(child.GetElement<IButtonElement>("open"), Is.Null);
                Assert.That(parent.GetElement<IButtonElement>("open"), Is.Not.Null);
            }
            finally
            {
                child.Destroy();
                parent.Destroy();
            }
        }

        [Test]
        public void ReloadClearsCachedAdapters()
        {
            var scope = stage.OpenPage(Html, "/composition.html", Layer.Panel);
            var definition = new ViewDefinition("reload", "/composition.html", _ => Array.Empty<ElementBinding>());
            var view = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            try
            {
                view.Initialize();
                var oldNode = scope.Find("title");
                var oldAdapter = view.GetElement<ITextElement>("title");
                Assert.That(oldAdapter, Is.Not.Null);

                scope.Document.LoadHtml(Html.Replace(">first<", ">reloaded<"), "/composition.html");

                Assert.That(oldNode.IsValid, Is.False);
                var current = view.GetElement<ITextElement>("title");
                Assert.That(current, Is.Not.Null.And.Not.SameAs(oldAdapter));
                Assert.That(current.Text.Value, Is.EqualTo("reloaded"));
            }
            finally
            {
                view.Destroy();
            }
        }

        sealed class Model : ObservableObject
        {
        }
        sealed class ReloadModel : ObservableObject
        {
            string title = "initial model";
            public string Title
            {
                get => title;
                set
                {
                    title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }
        sealed class ReloadPresenter : Presenter<ReloadModel>
        {
        }
        sealed class ReloadContext : BindingContext<ReloadModel>
        {
            public ReloadContext(IView view, ReloadModel model) : base(view, model) { }
            protected override void OnPropertyChanged(object sender, string propertyName) => OnSynchronize();
            protected override void OnSynchronize() => View.GetElement<ITextElement>("Title").Text.Value = ViewModel.Title;
        }
        sealed class Presenter : Presenter<Model>
        {
        }
        sealed class Context : BindingContext<Model>
        {
            public Context(IView view, Model model) : base(view, model) { }
            protected override void OnSynchronize()
            {
            }
        }
        static Route<Model> CreateRoute(RoutePolicy policy = null)
            => GeneratedRouteFactory.Create("composition-page", typeof(Presenter), () => new Model(),
                (v, m) => new Context(v, m), () => new Presenter(), policy ?? RoutePolicy.Default);

        internal static RouteDescriptor Route(RoutePolicy policy = null)
        {
            var route = CreateRoute(policy);
            return (RouteDescriptor)typeof(FUI.Navigation.Route).GetProperty("Descriptor",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(route);
        }
        internal sealed class Assets : IViewAssets
        {
            readonly TextAsset text;
            readonly Dictionary<string, CompiledDocumentAsset> compiled = new Dictionary<string, CompiledDocumentAsset>();
            internal Assets(TextAsset text = null)
            {
                this.text = text;
            }
            public TextAsset LoadText(string uri) => text ? text : global::UnityEngine.Resources.Load<TextAsset>(uri == "URUI/Tests/Composition.html" ? "URUI/Tests/Composition" : uri);
            public CompiledDocumentAsset LoadCompiled(string uri)
            {
                if (!compiled.TryGetValue(uri, out var asset))
                {
                    asset = CompiledFixtureAssets.Create(LoadText(uri), uri);
                    compiled.Add(uri, asset);
                }
                return asset;
            }
            public IReadOnlyList<TextResource> LoadDependencies(string htmlUri) => Array.Empty<TextResource>();
            public ImageLease AcquireImage(string uri) => null;
            public IReadOnlyList<ImageAsset> LoadImageGroup(string key) => Array.Empty<ImageAsset>();
        }
        internal static ViewDefinition Definition(string uri = "URUI/Tests/Composition.html",
            Action<ViewContext, View> configure = null, IEnumerable<IViewBehavior> behaviors = null)
            => new ViewDefinition("composition", uri, _ => new[] {
                ElementBinding.For("Title", "#title", (e, n) => new TextElement(e, n)) }, behaviors: behaviors, configure: configure);
        internal static ViewCatalog Catalog(RouteDescriptor route, ViewDefinition definition)
            => new ViewCatalog(new[] { new KeyValuePair<Type, ViewDefinition>(route.ViewModelType, definition) });

        [TestCase(Layer.Panel, true, false)]
        [TestCase(Layer.Panel, false, false)]
        [TestCase(Layer.Popup, true, true)]
        [TestCase(Layer.Popup, false, false)]
        public void NavigationFocusPolicyDoesNotMakeEveryDocumentModal(Layer layer, bool takeFocus, bool blocked)
        {
            stage.Surface.Input.UseUnityInput = false;
            stage.Surface.Viewport = new Vector2Int(400, 300);
            var home = stage.OpenPage("<!doctype html><html><head><style>body{margin:0;}button{position:absolute;left:20px;top:20px;width:60px;height:40px;}</style></head><body><button id='under'></button></body></html>", "/home.html", Layer.Panel);
            var text = new TextAsset("<!doctype html><html><head><style>html,body{margin:0;width:100%;height:100%;pointer-events:none;}button{position:absolute;left:300px;top:20px;width:60px;height:40px;pointer-events:auto;}</style></head><body><button id='upper'></button></body></html>");
            try
            {
                var policy = new RoutePolicy(layer).WithFocus(takeFocus);
                var definition = new ViewDefinition("overlay", "/overlay.html", _ => Array.Empty<ElementBinding>());
                var provider = new ViewProvider(stage, Catalog(Route(policy), definition), new Assets(text), UnityUIClock.Unscaled);
                var navigator = new Navigator(provider);
                navigator.Initialize();
                var handle = navigator.Open(CreateRoute(policy));
                var upper = stage.Pages.Single(page => page != home);
                home.Document.SynchronizeLayout();
                upper.Document.SynchronizeLayout();
                var lowerClicks = 0;
                var upperClicks = 0;
                home.Find("under").RegisterCallback<ClickEvent>(_ => lowerClicks++);
                upper.Root.Q("upper").RegisterCallback<ClickEvent>(_ => upperClicks++);
                var point = home.Find("under").WorldBound.center;
                Assert.AreEqual(blocked, home.Document.Pick(point) == null, "焦点配置与 Popup 模态阻断必须分开。");
                stage.Surface.Input.SendPointer(PointerPhase.Down, point, pressedButtons: 1);
                stage.Surface.Input.SendPointer(PointerPhase.Up, point);
                point = upper.Root.Q("upper").WorldBound.center;
                stage.Surface.Input.SendPointer(PointerPhase.Down, point, pressedButtons: 1);
                stage.Surface.Input.SendPointer(PointerPhase.Up, point);
                Assert.AreEqual(blocked ? 0 : 1, lowerClicks);
                Assert.AreEqual(1, upperClicks);
                Assert.IsTrue(navigator.Close(handle));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(text);
            }
        }

        [Test]
        public void MissingDefinitionReportsVmBeforeMountingPage()
        {
            var route = Route();
            var provider = new ViewProvider(stage, new ViewCatalog(Array.Empty<KeyValuePair<Type, ViewDefinition>>()), new Assets(), UnityUIClock.Unscaled);
            StringAssert.Contains(route.ViewModelType.FullName, Assert.Throws<InvalidOperationException>(() => provider.Create(route)).Message);
            Assert.AreEqual(0, stage.PageCount);
        }
        [Test]
        public void DuplicateDefinitionIsRejected()
        {
            var registration = new KeyValuePair<Type, ViewDefinition>(Route().ViewModelType, Definition());
            Assert.Throws<InvalidOperationException>(() => new ViewCatalog(new[] { registration, registration }));
        }
        [Test]
        public void WrongAdapterCapabilityAndComplexSelectorsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => ElementBinding.For<IElement>("Title", "#title", (e, n) => new TextElement(e, n)));
            Assert.Throws<ArgumentException>(() => ElementBinding.For("Title", ".composition > .caption", (e, n) => new TextElement(e, n)));
        }
        sealed class ManualContextClock : IUIClock
        {
            public double Now => 0;
            public float Delta => 0;
        }
        [Test]
        public void ProviderClockIsTheDocumentClockAndCannotChangeAfterLoad()
        {
            var clock = new ManualContextClock();
            var route = Route();
            var definition = Definition();
            var provider = new ViewProvider(stage, Catalog(route, definition), new Assets(), clock);
            using var lease = provider.Create(route);
            Assert.AreSame(clock, stage.Surface.UIClock);
            Assert.Throws<InvalidOperationException>(() => new ViewProvider(stage, Catalog(route, definition), new Assets(), new ManualContextClock()));
        }
        [Test]
        public void MissingResourceFailsBeforeMountingPage()
        {
            var route = Route();
            var provider = new ViewProvider(stage, Catalog(route, Definition("missing/composition.html")), new Assets(), UnityUIClock.Unscaled);
            StringAssert.Contains("missing/composition.html", Assert.Throws<InvalidOperationException>(() => provider.Create(route)).Message);
            Assert.AreEqual(0, stage.PageCount);
        }
        [Test]
        public void PartialConstructionRollsBackBehaviorsAndScope()
        {
            var existing = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var before = stage.PageCount;
            var calls = new List<int>();
            var route = Route();
            var definition = Definition(behaviors: new[] { new Behavior(1, calls), new Behavior(2, calls), new Behavior(3, calls, true) });
            var provider = new ViewProvider(stage, Catalog(route, definition), new Assets(), UnityUIClock.Unscaled);
            Assert.Throws<InvalidOperationException>(() => provider.Create(route));
            CollectionAssert.AreEqual(new[] { 2, 1 }, calls);
            Assert.AreEqual(before, stage.PageCount);
            Assert.IsTrue(existing.Root.IsValid);
        }
        [Test]
        public void DefinitionsStayLazyAndPreloadUsesSameResourceMapping()
        {
            var route = Route();
            var created = 0;
            var definition = new ViewDefinition("composition", "URUI/Tests/Composition.html", _ => new[] {
                ElementBinding.For("Title", "#title", (e, n) => { created++;
return new TextElement(e, n);
}) });
            var provider = new ViewProvider(stage, Catalog(route, definition), new Assets(), UnityUIClock.Unscaled);
            using var preload = provider.PreloadAsync(route, CancellationToken.None).Result;
            Assert.AreEqual(0, created);
            using var lease = provider.Create(route, preload);
            Assert.IsNotNull(((View)lease.View).Root.Document.CompiledSource, "正式页面必须使用编译资产。");
            Assert.AreEqual(0, created);
            lease.View.Initialize();
            Assert.AreEqual(1, created);
            Assert.IsNotNull(lease.View.GetElement<ITextElement>("Title"));
        }
        [Test]
        public void ConfigureFailureAndCancellationReleaseCompletedCandidate()
        {
            var assets = new Assets();
            var source = new CancellationTokenSource();
            var route = Route();
            var disposed = 0;
            var definition = Definition(configure: (context, view) =>
            {
                context.Own(new Release(() => disposed++));
                source.Cancel();
            });
            var provider = new ViewProvider(stage, Catalog(route, definition), assets, UnityUIClock.Unscaled);
            Assert.Throws<OperationCanceledException>(() => provider.CreateAsync(route, null, source.Token));
            Assert.AreEqual(1, disposed);
            Assert.AreEqual(0, stage.PageCount);
            definition = Definition(configure: (context, view) =>
            {
                context.Own(new Release(() => disposed++));
                throw new InvalidOperationException("configure");
            });
            provider = new ViewProvider(stage, Catalog(route, definition), assets, UnityUIClock.Unscaled);
            Assert.Throws<InvalidOperationException>(() => provider.Create(route));
            Assert.AreEqual(2, disposed);
            Assert.AreEqual(0, stage.PageCount);
            source.Dispose();
        }
        [Test]
        public void AsyncProviderCreatesPageAfterNativeEventPropagation()
        {
            var existing = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var route = Route();
            var provider = new ViewProvider(stage, Catalog(route, Definition()), new Assets(), UnityUIClock.Unscaled);
            Task<FUI.Resources.IViewLease> creation = null;
            using var click = existing.Find("open").RegisterCallback<ClickEvent>(_ =>
            {
                creation = provider.CreateAsync(route, null, CancellationToken.None).AsTask();
                Assert.IsFalse(creation.IsCompleted);
                Assert.AreEqual(1, stage.PageCount);
            });
            existing.Document.SynchronizeLayout();
            var point = existing.Find("open").WorldBound.center;
            stage.Surface.Input.SendPointer(PointerPhase.Down, point, pressedButtons: 1);
            stage.Surface.Input.SendPointer(PointerPhase.Up, point);
            Assert.IsNotNull(creation);
            Assert.IsTrue(creation.IsCompletedSuccessfully);
            using var lease = creation.Result;
            lease.View.Initialize();
            Assert.AreEqual(2, stage.PageCount);
            Assert.IsTrue(existing.Root.IsValid);
        }
        [Test]
        public void DocumentRestorationRecreatesCompositionAndResumesExistingModelBindings()
        {
            GeneratedBindingRegistry.Register<ReloadModel>((v, m) => new ReloadContext(v, m), () => new ReloadPresenter());
            var scope = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var attached = 0;
            var released = 0;
            var definition = Definition(configure: (context, view) =>
            {
                attached++;
                context.Own(new Release(() => released++));
            });
            var view = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            var model = new ReloadModel();
            using var projection = Projection.Create(view, model);
            projection.Activate();
            var oldRoot = scope.Root;
            var oldAdapter = view.GetElement<TextElement>("Title");
            stage.Surface.enabled = false;
            model.Title = "while suspended";
            stage.Surface.enabled = true;
            Assert.IsFalse(oldRoot.IsValid);
            Assert.IsTrue(scope.Root.IsValid);
            Assert.AreNotSame(oldAdapter, view.GetElement<TextElement>("Title"));
            Assert.AreEqual("while suspended", scope.Find("title").Text);
            Assert.AreEqual(2, attached);
            Assert.AreEqual(1, released);
            model.Title = "restored binding";
            Assert.AreEqual(model.Title, scope.Find("title").Text);
            view.Visible = false;
            Assert.IsFalse(scope.Find("open").Focus());
            scope.Document.LoadHtml(Html, "URUI/Tests/Composition.html");
            Assert.IsFalse(scope.Find("open").Focus());
            Assert.AreEqual(model.Title, scope.Find("title").Text);
            Assert.AreEqual(3, attached);
            Assert.AreEqual(2, released);
            view.Visible = true;
            Assert.IsTrue(scope.Find("open").Focus());
            var restoredAdapter = view.GetElement<TextElement>("Title");
            Assert.Throws<InvalidOperationException>(() => scope.Document.LoadHtml("<html><body></html>", "bad.html"));
            Assert.AreSame(restoredAdapter, view.GetElement<TextElement>("Title"));
            model.Title = "after failed reload";
            Assert.AreEqual(model.Title, scope.Find("title").Text);
            projection.Dispose();
            Assert.AreEqual(3, released);
            Assert.AreEqual(0, stage.PageCount);
        }
        [Test]
        public void DeactivationDuringSuspensionDoesNotResumeBindingsUntilActivated()
        {
            GeneratedBindingRegistry.Register<ReloadModel>((v, m) => new ReloadContext(v, m), () => new ReloadPresenter());
            var scope = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var view = Definition().Compose(scope, new Assets(), UnityUIClock.Unscaled);
            var model = new ReloadModel();
            using var projection = Projection.Create(view, model);
            projection.Activate();
            stage.Surface.enabled = false;
            projection.Deactivate();
            model.Title = "inactive model";
            stage.Surface.enabled = true;
            Assert.AreEqual("first", scope.Find("title").Text);
            projection.Activate();
            Assert.AreEqual(model.Title, scope.Find("title").Text);
            model.Title = "active model";
            Assert.AreEqual(model.Title, scope.Find("title").Text);
        }
        [Test]
        public void ReplacingModelDuringSuspensionRetainsThePendingBinding()
        {
            ReloadContext context = null;
            GeneratedBindingRegistry.Register<ReloadModel>((v, m) => context = new ReloadContext(v, m), () => new ReloadPresenter());
            var scope = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var view = Definition().Compose(scope, new Assets(), UnityUIClock.Unscaled);
            var previous = new ReloadModel();
            using var projection = Projection.Create(view, previous);
            projection.Activate();
            stage.Surface.enabled = false;
            var replacement = new ReloadModel { Title = "replacement" };
            var contract = typeof(BindingContext<ReloadModel>).GetInterface("FUI.Infrastructure.Binding.IBindingContext");
            var update = contract.GetMethod("UpdateViewModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual(true, update.Invoke(context, new object[] { replacement }));
            stage.Surface.enabled = true;
            Assert.AreEqual(replacement.Title, scope.Find("title").Text);
            previous.Title = "retired model";
            Assert.AreEqual(replacement.Title, scope.Find("title").Text);
            replacement.Title = "new update";
            Assert.AreEqual(replacement.Title, scope.Find("title").Text);
        }
        sealed class CallbackBehavior : IViewBehavior
        {
            readonly Func<ViewContext, IDisposable> attach;
            public CallbackBehavior(Func<ViewContext, IDisposable> attach)
            {
                this.attach = attach;
            }
            public IDisposable Attach(ViewContext context) => attach(context);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void FailedRestorationReleasesPartialGenerationAndCanRecover(bool failInBehavior)
        {
            var scope = stage.OpenPage(Html, "URUI/Tests/Composition.html", Layer.Panel);
            var generation = 0;
            var released = new List<int>();
            var definition = new ViewDefinition("recovery", "URUI/Tests/Composition.html", _ => new[] {
                ElementBinding.For("Title", "#title", (e, n) => new TextElement(e, n)),
                ElementBinding.For("Open", "#open", (e, n) => {
                    if (!failInBehavior && generation == 2) {
throw new InvalidOperationException("expected recovery failure");
}
                    return new ButtonElement(e, n);
                }) }, behaviors: new IViewBehavior[] {
                    new CallbackBehavior(_ => { var current = ++generation;
return new Release(() => released.Add(current));
}),
                    new CallbackBehavior(_ => {
                        if (failInBehavior && generation == 2) {
throw new InvalidOperationException("expected recovery failure");
}
                        return new Release(() => { });
                    }) });
            var view = definition.Compose(scope, new Assets(), UnityUIClock.Unscaled);
            view.Initialize();
            var available = 0;
            stage.Surface.enabled = false;
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException: expected recovery failure"));
            stage.Surface.enabled = true;
            Assert.IsFalse(((IViewContentLifecycle)view).IsContentAvailable);
            Assert.AreEqual(0, view.Elements.Count);
            Assert.IsNull(view.GetElement<TextElement>("Title"));
            CollectionAssert.AreEqual(new[] { 1, 2 }, released);
            ((IViewContentLifecycle)view).ContentAvailable += () => available++;
            scope.Document.LoadHtml(Html, "URUI/Tests/Composition.html");
            Assert.IsTrue(((IViewContentLifecycle)view).IsContentAvailable);
            Assert.AreEqual(2, view.Elements.Count);
            Assert.AreEqual(1, available);
            view.Destroy();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, released);
        }

        sealed class Behavior : IViewBehavior
        {
            readonly int id;
            readonly List<int> calls;
            readonly bool fail;
            public Behavior(int id, List<int> calls, bool fail = false)
            {
                this.id = id;
                this.calls = calls;
                this.fail = fail;
            }
            public IDisposable Attach(ViewContext context)
            {
                if (fail)
                {
                    throw new InvalidOperationException("attach");
                }
                return new Release(() =>
                {
                    Assert.IsTrue(context.Scope.Root.IsValid);
                    calls.Add(id);
                });
            }
        }

        sealed class Release : IDisposable
        {
            readonly Action release;
            public Release(Action release)
            {
                this.release = release;
            }
            public void Dispose()
            {
                release();
            }
        }
    }
}
