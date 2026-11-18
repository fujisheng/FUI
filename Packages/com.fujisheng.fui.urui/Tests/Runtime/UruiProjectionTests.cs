using System.Linq;
using System.Collections;
using System.Threading;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Profiling;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiProjectionTests
    {
        const string FixtureTemplate = "/URUI/Templates/ReviewDynamic.html";
        static UruiView FixtureItem(UruiScope scope,System.Func<global::URUI.Elements.Element,string,UruiElement> create=null)=>new UruiView("ReviewDynamic",scope,
            new[]{UruiElementBinding.For("label", ".fixture-label",create??((e,k)=>new UruiTextElement(e,k)))});
        static UruiListElement FixtureList(global::URUI.Elements.Element element,string name,System.Func<global::URUI.Elements.Element,string,UruiElement> create=null)=>
            new UruiListElement(element,name,FixtureTemplate,scope=>FixtureItem(scope,create));
        sealed class FixtureDynamicElement:UruiDynamicViewElement
        {
            internal FixtureDynamicElement(global::URUI.Elements.Element element,string name,System.Func<global::URUI.Elements.Element,string,UruiElement> create)
                :base(element,name,_=>FixtureTemplate,(_,scope)=>FixtureItem(scope,create)) { }
            protected override string ResolveTemplate(string source)=>string.IsNullOrEmpty(source)&&ViewModel.Value!=null?"ReviewDynamic":source;
        }
        static UruiDynamicViewElement FixtureDynamic(global::URUI.Elements.Element element,string name,System.Func<global::URUI.Elements.Element,string,UruiElement> create=null)=>new FixtureDynamicElement(element,name,create);
        static UruiSlotListElement FixtureSlots(global::URUI.Elements.Element element,string name,System.Func<global::URUI.Elements.Element,string,UruiElement> create=null)=>
            new UruiSlotListElement(element,name,element.Hierarchy.Children().Where(child=>child.ClassListContains("fixture-slot")).ToArray(),(scope,index)=>FixtureItem(scope,create));

        GameObject root;
        GameObject cameraObject;
        UruiScope scope;

        sealed class FirstModel : ObservableObject { }
        sealed class SecondModel : ObservableObject { }
        sealed class ReentrantModel : ObservableObject { }
        sealed class ConfiguredModelElement : UruiDynamicViewElement
        {
            public ConfiguredModelElement(global::URUI.Elements.Element element, string name) : base(element,name,_=>FixtureTemplate,(_,scope)=>FixtureItem(scope)) { }
            protected override string ResolveTemplate(string source) => ViewModel.Value == null ? base.ResolveTemplate(source) : "ReviewDynamic";
        }
        sealed class PerItemTemplateList : UruiListElement
        {
            public PerItemTemplateList(global::URUI.Elements.Element element, string name) : base(element,name,FixtureTemplate,scope=>FixtureItem(scope)) { }
            protected override void InitializeItem(UruiScope scope, ObservableObject item) =>
                scope.Root.SetAttribute("data-owner", item.GetType().Name);
        }
        sealed class ProbePresenter : Presenter<ObservableObject> { }
        sealed class OpeningPresenter : Presenter<ObservableObject>
        {
            readonly System.Action opening, closing, destroying;
            public OpeningPresenter(System.Action opening, System.Action closing = null, System.Action destroying = null) { this.opening = opening; this.closing = closing; this.destroying = destroying; }
            protected override void OnOpen(object param) => opening();
            protected override void OnClose() => closing?.Invoke();
            protected override void OnDestroy() => destroying?.Invoke();
        }
        sealed class CallbackPresenter : Presenter<ObservableObject>
        {
            readonly System.Action creating, destroying;
            public CallbackPresenter(System.Action creating, System.Action destroying) { this.creating = creating; this.destroying = destroying; }
            protected override void OnCreate() => creating?.Invoke();
            protected override void OnDestroy() => destroying?.Invoke();
        }
        sealed class EmptyProbeContext : BindingContext<ObservableObject>
        {
            readonly System.Action binding;
            public EmptyProbeContext(IView view, ObservableObject model, System.Action binding) : base(view, model) { this.binding = binding; }
            protected override void OnBinding() => binding?.Invoke();
            protected override void OnSynchronize() { }
        }
        sealed class CountingTextElement : UruiTextElement
        {
            readonly System.Action disposing;
            public CountingTextElement(global::URUI.Elements.Element element, string name, System.Action disposing) : base(element, name) { this.disposing = disposing; }
            public override void Dispose() { try { base.Dispose(); } finally { disposing(); } }
        }
        sealed class ThrowingTextElement : UruiTextElement
        {
            public ThrowingTextElement(global::URUI.Elements.Element element, string name) : base(element, name) { }
            public override void Dispose() => throw new System.InvalidOperationException("nested cleanup failure");
        }
        sealed class ProbeContext : BindingContext<ObservableObject>
        {
            public ProbeContext(IView view, ObservableObject model) : base(view, model) { }
            protected override void OnSynchronize() => View.GetElement<ITextElement>("label").Text.Value = ViewModel.GetType().Name;
        }

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            root = new GameObject("URUI Projection Stage");
            var stage = root.AddComponent<UruiStage>();
            FuiHtmlFixtureAssets.SetStyles(stage, new[]{global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection.styles"),global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/FixtureLayouts")});
            var page = FuiHtmlFixtureAssets.OpenPage(stage, "<div id='host' class='fixture-host'></div>", Layer.Panel);
            scope = new UruiScope(page.Root.Q("host"));
            GeneratedBindingRegistry.Register<FirstModel>((v, m) => new ProbeContext(v, m), () => new ProbePresenter());
            GeneratedBindingRegistry.Register<SecondModel>((v, m) => new ProbeContext(v, m), () => new ProbePresenter());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void FirstSpatialReadObservesChangesMadeAfterAdapterConstruction()
        {
            var host = scope.Root;
            host.Style.SetWidth(100); scope.Document.SynchronizeGeometry();
            using var adapter = new UruiElement(host, "lazy spatial");
            host.Style.SetWidth(220); scope.Document.SynchronizeGeometry();
            Assert.That(adapter.SizeValue.Value.x, Is.EqualTo(220).Within(.01));
        }

        [Test]
        public void ListChangesRetainUnchangedItemProjectionsAndAuthoredOrder()
        {
            using var list = FixtureList(scope.Root, "rows");
            var first = new FirstModel(); var second = new SecondModel();
            var items = new ObservableList<ObservableObject> { first, second };
            list.Items.Value = items;
            var firstView = list.GetItemView(0); var secondView = list.GetItemView(1);
            items.Insert(0, new FirstModel()); list.OnAdd(items, 0, items[0]);
            Assert.AreSame(firstView, list.GetItemView(1)); Assert.AreSame(secondView, list.GetItemView(2));
            items.RemoveAt(0); list.OnRemove(items, 0, null);
            Assert.AreSame(firstView, list.GetItemView(0)); Assert.AreSame(secondView, list.GetItemView(1));
            items[0] = second; items[1] = first; list.OnUpdate(items);
            Assert.AreSame(secondView, list.GetItemView(0)); Assert.AreSame(firstView, list.GetItemView(1));
            Assert.AreSame(((UruiView)secondView).Root, scope.Root[0]);
        }

        sealed class FailingBatchElement : global::URUI.Elements.Element
        {
            public bool Fail;
            public System.Action Updating;
            public FailingBatchElement(global::URUI.Elements.ElementContext context) : base(context) { }
            protected override void OnUpdate() { var callback = Updating; Updating = null; callback?.Invoke(); if (Fail) throw new System.InvalidOperationException("batch layout failure"); }
        }
        sealed class FailingLayoutList : UruiListElement
        {
            public FailingLayoutList(global::URUI.Elements.Element host) : base(host, "rows", FixtureTemplate, child => FixtureItem(child)) { }
            protected override void InitializeItem(UruiScope child, ObservableObject item)
            {
                var probe = child.Document.Create<FailingBatchElement>(); probe.Fail = true; child.Root.Add(probe);
            }
        }
        void RegisterFailingBatchElement() => scope.Document.RegisterElementType("urui-failing-batch", context => new FailingBatchElement(context));

        [Test]
        public void FixedSlotAddAndReplaceRetainUnaffectedProjections()
        {
            scope.LoadFragment("<div class='fixture-slot'><span class='fixture-label'></span></div><div class='fixture-slot'><span class='fixture-label'></span></div>");
            using var list = FixtureSlots(scope.Root, "slots");
            var first = new FirstModel();
            var items = new ObservableList<ObservableObject> { first };
            list.Items.Value = items;
            var original = list.GetItemView(0);
            items.Add(new SecondModel());
            list.OnAdd(items, 1, items[1]);
            Assert.AreSame(original, list.GetItemView(0), "追加不能销毁已有槽位的绑定和局部状态。");
            var second = list.GetItemView(1);
            items[0] = new SecondModel();
            list.OnReplace(items, 0, first, items[0]);
            Assert.AreNotSame(original, list.GetItemView(0));
            Assert.AreSame(second, list.GetItemView(1));
            Assert.AreEqual("SecondModel", scope.Root[0][0].Text);
        }

        [Test]
        public void FixedSlotDuplicatesRemainIndependentAcrossShrinkAndRegrowth()
        {
            scope.LoadFragment("<div class='fixture-slot'><span class='fixture-label'></span></div><div class='fixture-slot'><span class='fixture-label'></span></div>");
            using var list = FixtureSlots(scope.Root, "slots");
            var model = new FirstModel();
            var items = new ObservableList<ObservableObject> { model, model };
            list.Items.Value = items;
            var first = list.GetItemView(0);
            var second = list.GetItemView(1);
            Assert.AreNotSame(first, second);
            items.RemoveAt(1);
            list.OnRemove(items, 1, model);
            Assert.AreSame(first, list.GetItemView(0));
            Assert.IsNull(list.GetItemView(1));
            items.Add(model);
            list.OnAdd(items, 1, model);
            Assert.AreSame(first, list.GetItemView(0));
            Assert.AreNotSame(second, list.GetItemView(1));
            Assert.AreSame(scope.Root[1], ((UruiView)list.GetItemView(1)).Root);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstImagePropertyReadHandlesGeometryCallbackReentry(bool dispose)
        {
            scope.LoadFragment("<img id='image' style='object-fit:cover;'>");
            var node = scope.Root.Q("image");
            using var adapter = new UruiImageElement(node, "reentrant image");
            BindableProperty<bool> fromCallback = null;
            using var subscription = node.RegisterCallback<global::URUI.Events.GeometryChangedEvent>(_ =>
            {
                if (dispose) adapter.Dispose();
                else fromCallback = adapter.PreserveAspect;
            });
            using (scope.Document.BeginUpdate())
            {
                node.Style.SetWidth(77);
                if (dispose)
                    Assert.Throws<System.ObjectDisposedException>(() => { _ = adapter.PreserveAspect; });
                else
                {
                    var property = adapter.PreserveAspect;
                    Assert.That(fromCallback, Is.Not.Null);
                    Assert.That(property, Is.SameAs(fromCallback), "回调中发布的属性不能被外层初始化覆盖。");
                    Assert.That(((global::URUI.Controls.Image)node).Fit, Is.EqualTo(global::URUI.Controls.ImageFit.Cover));
                }
            }
        }

        [Test]
        public void ConstructingAnImageAdapterDoesNotFlushAnUpdateBatch()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<img id='image'><urui-failing-batch></urui-failing-batch>");
            var probe = scope.Root.Q<FailingBatchElement>();
            var updates = 0;
            using (scope.Document.BeginUpdate())
            {
                scope.Root.Style.SetWidth(121);
                probe.Updating = () => updates++;
                using var image = new UruiImageElement(scope.Root.Q("image"), "image");
                Assert.AreEqual(0, updates, "可选图片属性未使用时不能强制提交布局。");
            }
            Assert.AreEqual(1, updates, "批次结束仍必须完成待处理几何。");
        }

        [Test]
        public void EnablingAViewMergesItsPresentersListUpdates()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<div class='rows'></div><urui-failing-batch></urui-failing-batch>");
            var view = new UruiView("opening", scope, new[] { UruiElementBinding.For("rows", ".rows", (element, key) => FixtureList(element, key)) });
            view.Initialize();
            var probe = scope.Root.Q<FailingBatchElement>();
            var updates = 0;
            System.Action updating = null;
            updating = () => { updates++; probe.Updating = updating; };
            probe.Updating = updating;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () => new OpeningPresenter(() =>
            {
                var list = view.GetElement<IListView>("rows");
                var items = new ObservableList<ObservableObject>();
                list.Items.Value = items;
                for (var index = 0; index < 3; index++)
                {
                    items.Add(new FirstModel());
                    list.OnAdd(items, index, items[index]);
                }
            }));
            var type = typeof(GeneratedBindingRegistry).Assembly.GetType("FUI.Infrastructure.ViewInstance");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
            var instance = type.GetMethod("Create", flags).Invoke(null, new object[] { view, new ReentrantModel() });
            try
            {
                type.GetMethod("Enable", flags).Invoke(instance, new object[] { null });
                Assert.AreEqual(1, updates, "启用事务应在返回前一次性同步未被主动读取的布局。");
                Assert.AreEqual(3, scope.Root.Q(className: "rows").ChildCount);
            }
            finally { type.GetMethod("DestroyProjection", flags).Invoke(instance, null); view.Destroy(); }
        }

        [Test]
        public void DestructionDuringOnOpenCancelsBeforeChangingVisibility()
        {
            var view = new UruiView("cancelled opening", scope);
            view.Initialize();
            var type = typeof(GeneratedBindingRegistry).Assembly.GetType("FUI.Infrastructure.ViewInstance");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
            object instance = null;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () => new OpeningPresenter(() =>
                type.GetMethod("DestroyProjection", flags).Invoke(instance, null)));
            instance = type.GetMethod("Create", flags).Invoke(null, new object[] { view, new ReentrantModel() });
            try
            {
                var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => type.GetMethod("Enable", flags).Invoke(instance, new object[] { null }));
                Assert.IsInstanceOf<System.OperationCanceledException>(error.InnerException);
            }
            finally { type.GetMethod("DestroyProjection", flags).Invoke(instance, null); view.Destroy(); }
        }

        [Test]
        public void FinalOpeningGeometryFailureRollsBackActivationAndAllowsRetry()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<urui-failing-batch></urui-failing-batch>");
            var view = new UruiView("failed opening", scope);
            view.Initialize();
            var probe = scope.Root.Q<FailingBatchElement>();
            var fail = true; var activeSubscriptions = 0;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () => new OpeningPresenter(() =>
            {
                activeSubscriptions++;
                probe.Fail = fail;
                scope.Root.Style.SetWidth(188);
            }, () => activeSubscriptions--));
            var type = typeof(GeneratedBindingRegistry).Assembly.GetType("FUI.Infrastructure.ViewInstance");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
            var instance = type.GetMethod("Create", flags).Invoke(null, new object[] { view, new ReentrantModel() });
            try
            {
                Assert.Throws<System.Reflection.TargetInvocationException>(() => type.GetMethod("Enable", flags).Invoke(instance, new object[] { null }));
                Assert.IsFalse(view.Visible);
                Assert.Zero(activeSubscriptions, "OnOpen 已成功，最终布局失败必须调用 OnClose 释放本次注册。");
                fail = false;
                Assert.DoesNotThrow(() => type.GetMethod("Enable", flags).Invoke(instance, new object[] { null }));
                Assert.IsTrue(view.Visible);
                Assert.AreEqual(1, activeSubscriptions);
            }
            finally { type.GetMethod("DestroyProjection", flags).Invoke(instance, null); view.Destroy(); }
            Assert.Zero(activeSubscriptions);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestructionDuringFinalOpeningGeometryClosesBeforeDestroy(bool closingThrows)
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<urui-failing-batch></urui-failing-batch>");
            var view = new UruiView("destroyed opening", scope);
            view.Initialize();
            var probe = scope.Root.Q<FailingBatchElement>();
            var active = 0; var closes = 0; var activeAtDestroy = -1;
            ProjectionHandle<ReentrantModel> projection = null;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () => new OpeningPresenter(() =>
            {
                active++;
                probe.Updating = () => projection.Dispose();
                scope.Root.Style.SetWidth(199);
            }, () =>
            {
                active--; closes++;
                projection.Dispose();
                if (closingThrows) throw new System.InvalidOperationException("opening rollback close failed");
            }, () => activeAtDestroy = active));
            projection = Projection.Create(view, new ReentrantModel());
            try
            {
                Assert.Catch<System.Exception>(() => projection.Activate());
                Assert.That(projection.IsAlive, Is.False);
                Assert.That(active, Is.Zero);
                Assert.That(closes, Is.EqualTo(1));
                Assert.That(activeAtDestroy, Is.Zero, "OnDestroy 前必须消费已成功 OnOpen 的清理所有权。");
                projection.Deactivate(); projection.Dispose();
                Assert.That(closes, Is.EqualTo(1));
            }
            finally { projection.Dispose(); view.Destroy(); }
        }

        [Test]
        public void RequestedPainterGeometryIsPreparedOnceAtTheOuterBatchEnd()
        {
            scope.LoadFragment("<div class='painted' style='width:12px;height:13px;--urui-decorator:shader(batch-painter);'></div>");
            var node = scope.Root.Q(className: "painted");
            var paints = 0;
            using var painter = scope.Document.AttachPainter(node, "batch-painter", _ => paints++);
            using (scope.Document.BeginUpdate())
            {
                scope.Document.RequestGeometry(node);
                scope.Document.RequestGeometry(node);
                Assert.AreEqual(0, paints, "内部请求应合并；公开 Initialize 的即时绘制保证另有测试保护。");
            }
            Assert.Greater(paints, 0);
        }

        [Test]
        public void PublicInitializePreparesItsPainterInsideAnOuterBatch()
        {
            scope.LoadFragment("<div class='painted' style='width:12px;height:13px;--urui-decorator:shader(batch-painter);'></div>");
            var node = scope.Root.Q(className: "painted"); var paints = 0;
            using var painter = scope.Document.AttachPainter(node, "batch-painter", _ => paints++);
            var view = new UruiView("nested painter", scope, new[] { UruiElementBinding.For("painted", ".painted", (element, key) => new UruiElement(element, key)) });
            try
            {
                using (scope.Document.BeginUpdate())
                {
                    view.Initialize();
                    Assert.Greater(paints, 0, "公开初始化返回时自己的 painter 必须已准备。");
                }
            }
            finally { view.Destroy(); }
        }

        [Test]
        public void BatchedGeometryReadDoesNotPrepareAnUnrelatedPainter()
        {
            var outside = scope.Document.CreateElement(); scope.Document.Body.Add(outside);
            outside.Style.Set("--urui-decorator", "shader(batch-outside)"); outside.Style.SetWidth(10); outside.Style.SetHeight(10);
            var paints = 0;
            using var painter = scope.Document.AttachPainter(outside, "batch-outside", _ => paints++);
            using (scope.Document.BeginUpdate())
            {
                scope.Root.Style.SetWidth(123);
                Assert.That(scope.Root.Layout.width, Is.EqualTo(123).Within(.01));
                Assert.Zero(paints, "几何读取只同步几何，不能准备无关绘制器。");
            }
        }

        [Test]
        public void ListFinalLayoutFailureReleasesAllStagedProjections()
        {
            RegisterFailingBatchElement();
            using var list = new FailingLayoutList(scope.Root);
            Assert.Throws<System.InvalidOperationException>(() => list.Items.Value = new ObservableList<ObservableObject> { new FirstModel() });
            Assert.IsNull(list.GetItemView(0)); Assert.Zero(scope.Root.ChildCount);
        }

        [Test]
        public void DynamicFinalLayoutFailurePreservesThePreviousProjection()
        {
            RegisterFailingBatchElement(); var fail = false;
            using var adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) =>
            {
                if (fail) { var probe = child.Document.Create<FailingBatchElement>(); probe.Fail = true; child.Root.Add(probe); }
                return FixtureItem(child);
            });
            adapter.Source.Value = "first"; var previous = scope.Root[0]; fail = true;
            Assert.Throws<System.InvalidOperationException>(() => adapter.Source.Value = "second");
            Assert.IsTrue(previous.IsValid); Assert.AreSame(previous, scope.Root[0]); Assert.AreEqual(1, scope.Root.ChildCount);
        }

        [Test]
        public void FixedSlotFinalLayoutFailureReleasesTheStagedProjection()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<div class='fixture-slot'><span class='fixture-label'></span></div>");
            using var list = new UruiSlotListElement(scope.Root, "slots", new[] { scope.Root[0] }, (child, index) =>
            {
                var probe = child.Document.Create<FailingBatchElement>(); probe.Fail = true; child.Root.Add(probe);
                return FixtureItem(child);
            });
            Assert.Throws<System.InvalidOperationException>(() => list.Items.Value = new ObservableList<ObservableObject> { new FirstModel() });
            Assert.IsNull(list.GetItemView(0));
        }

        sealed class CallbackLayoutList : UruiListElement
        {
            readonly System.Action callback;
            public CallbackLayoutList(global::URUI.Elements.Element host, System.Action callback) : base(host, "rows", FixtureTemplate, child => FixtureItem(child)) { this.callback = callback; }
            protected override void InitializeItem(UruiScope child, ObservableObject item)
            {
                var probe = child.Document.Create<FailingBatchElement>(); probe.Updating = callback; child.Root.Add(probe);
            }
        }

        [Test]
        public void ListFinalGeometryCallbackReconcilesItsNewItemsBeforeReturning()
        {
            RegisterFailingBatchElement();
            var items = new ObservableList<ObservableObject> { new FirstModel() }; CallbackLayoutList list = null; var changed = false;
            using (list = new CallbackLayoutList(scope.Root, () =>
            {
                if (changed) return; changed = true; items.Add(new SecondModel()); list.OnAdd(items, 1, items[1]);
            }))
            {
                list.Items.Value = items;
                Assert.IsTrue(changed); Assert.IsNotNull(list.GetItemView(1)); Assert.AreEqual(2, scope.Root.ChildCount);
            }
        }

        [Test]
        public void FixedSlotsFinalGeometryCallbackReconcilesItsNewItemsBeforeReturning()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<div class='fixture-slot'><span class='fixture-label'></span></div><div class='fixture-slot'><span class='fixture-label'></span></div>");
            var items = new ObservableList<ObservableObject> { new FirstModel() }; UruiSlotListElement list = null; var changed = false;
            using (list = new UruiSlotListElement(scope.Root, "slots", new[] { scope.Root[0], scope.Root[1] }, (child, index) =>
            {
                var probe = child.Document.Create<FailingBatchElement>(); child.Root.Add(probe);
                probe.Updating = () => { if (changed) return; changed = true; items.Add(new SecondModel()); list.OnAdd(items, 1, items[1]); };
                return FixtureItem(child);
            }))
            {
                list.Items.Value = items;
                Assert.IsTrue(changed); Assert.IsNotNull(list.GetItemView(1));
            }
        }

        [Test]
        public void PublicInitializationAfterPreparationIgnoresItsOwnLayoutReentry()
        {
            RegisterFailingBatchElement();
            scope.LoadFragment("<urui-failing-batch class='probe'></urui-failing-batch>");
            var probe = scope.Root.Q<FailingBatchElement>(className: "probe");
            var view = new UruiView("prepared reentry", scope, new[] { UruiElementBinding.For("probe", ".probe", (element, key) => new UruiElement(element, key)) });
            try
            {
                ((IViewPreparation)view).PrepareForBinding();
                probe.Updating = view.Initialize;
                Assert.DoesNotThrow(view.Initialize); Assert.AreEqual(1, view.Elements.Count); Assert.IsTrue(view.IsContentAvailable);
            }
            finally { view.Destroy(); }
        }

        static object spatialAllocationProbe;
        static long SpatialAllocations(System.Action action)
        {
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            action(); recorder.Stop();
            var valid = recorder.Valid; var wrapped = recorder.WrappedAround; long count = 0;
            for (var i = 0; i < recorder.Count; i++) { count += recorder.GetSample(i).Count; }
            Assert.That(valid, Is.True); Assert.That(wrapped, Is.False); return count;
        }

        [Test]
        public void RepeatedVisibilityWritesPreserveAuthorStylesWithoutAllocations()
        {
            var host=scope.Root;host.Style.Set("display","flex");
            using var adapter=new UruiElement(host,"visible");
            System.Action cycle=()=>{for(var i=0;i<32;i++){adapter.Visible.Value=false;adapter.Visible.Value=true;}};
            for(var i=0;i<10;i++)cycle();
            Assert.That(SpatialAllocations(()=>spatialAllocationProbe=new byte[16]),Is.GreaterThan(0));
            var allocations=SpatialAllocations(cycle);
            Assert.That(host.Style.Get("display"),Is.EqualTo("flex"));
            host.Style.Set("display","block");adapter.Visible.Value=false;adapter.Visible.Value=true;
            Assert.That(host.Style.Get("display"),Is.EqualTo("block"),"外部作者写入必须参与下一次显隐恢复。");
            Assert.That(allocations,Is.Zero);spatialAllocationProbe=null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedSpatialPositionWritesDoNotAllocateAfterCapacityWarmup(bool world)
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var parent = scope.Root.Q(className: "projection-parent");
            parent.AddToClassList("projection-pixel-origin");
            using var spatial = new UruiElement(parent[0], "moving");
            var property = world ? spatial.WorldPositionValue : spatial.LocalPositionValue;
            var notifyMethod = typeof(global::URUI.Documents.Document).GetMethod("NotifyLayoutUpdated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var notify = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), scope.Root.Document, notifyMethod);
            SpatialValue Expected(int i) => new SpatialValue(140 + (i & 7), 120 + (i & 3), 0);
            void Step(int i)
            {
                property.Value = Expected(i);
                scope.Root.Document.SynchronizeLayout(); notify();
                scope.Root.Document.SynchronizeLayout(); notify();
                scope.Root.Document.SynchronizeLayout(); notify();
                spatial.SynchronizeExternalValues();
            }
            System.Action write = () => { for (var i = 300; i < 332; i++) { Step(i); } };
            for (var i = 0; i < 10; i++) { write(); }
            Assert.That(SpatialAllocations(() => spatialAllocationProbe = new byte[16]), Is.GreaterThan(0));
            var allocations = SpatialAllocations(write);
            var actual = property.Value; var expected = Expected(331);
            var point = parent[0].LocalToWorld(new Vector2(10, 6));
            if (world)
            {
                Assert.That(point.x, Is.EqualTo(expected.x).Within(.1f));
                Assert.That(Screen.height - point.y, Is.EqualTo(expected.y).Within(.1f));
            }
            else
            {
                var local = parent.WorldToLocal(point);
                Assert.That(local.x - 200, Is.EqualTo(expected.x).Within(.1f));
                Assert.That(100 - local.y, Is.EqualTo(expected.y).Within(.1f));
            }
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.1f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(.1f));
            Assert.That(allocations, Is.Zero, "稳定的局部/世界位置更新不能反复创建事件、集合或 CSS 数值字符串。");
            spatialAllocationProbe = null;
        }

        [UnityTest]
        public IEnumerator DynamicFactoryReceivesTheLoadedAuthoredHierarchy()
        {
            var host=scope.Find("host");var calls=0;
            using(var adapter=new UruiDynamicViewElement(host,"host",
                _=>"/URUI/Tests/AuthoredDynamic.html",(_,child)=>
                {
                    calls++;Assert.IsNotNull(child.Root.Q(className:"dynamic-label"),"子视图工厂必须能够组合已经加载的作者节点。");
                    return new UruiView("child",child,new[]{UruiElementBinding.For("label", ".dynamic-label", (element,name)=>new UruiTextElement(element,name))});
                }))
            {
                adapter.Source.Value="child";Assert.AreEqual(1,calls);
                Assert.AreEqual("authored",host.Q(className:"dynamic-label").Text);
            }
            Assert.AreEqual(0,host.ChildCount);yield return null;
        }
        [UnityTest]
        public IEnumerator DynamicFactoryFailureKeepsThePreviousProjectionAndRemovesStagedNodes()
        {
            var host=scope.Find("host");var fail=false;
            using(var adapter=new UruiDynamicViewElement(host,"host",
                _=>"/URUI/Tests/AuthoredDynamic.html",(_,child)=>
                {
                    if(fail){throw new System.InvalidOperationException("factory failure");}
                    return new UruiView("child",child,new[]{UruiElementBinding.For("label", ".dynamic-label", (element,name)=>new UruiTextElement(element,name))});
                }))
            {
                adapter.Source.Value="first";var previous=host[0];fail=true;
                Assert.Throws<System.InvalidOperationException>(()=>adapter.Source.Value="second");
                Assert.AreEqual(1,host.ChildCount,"工厂失败不能遗留半构造的子视图节点。");Assert.AreSame(previous,host[0]);
                Assert.AreEqual("authored",previous.Q(className:"dynamic-label").Text);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedSlotsPreserveAuthoredGeometryAndScopesAcrossReplacementAndClear()
        {
            scope.LoadFragment("<div style='position:absolute;left:0;top:0;width:400px;height:200px;' id='slots' class='fixture-1'><div style='position:absolute;left:20px;top:30px;width:80px;height:24px;transform:rotate(20deg);' class='fixture-slot'><span id='label' class='fixture-3 fixture-label'></span></div><div style='position:absolute;left:210px;top:80px;width:120px;height:50px;' class='fixture-slot'><span id='label' class='fixture-5 fixture-label'></span></div></div>");
            var panel = new UruiView("fixed-slots-page", scope,new UruiElementBinding[]{UruiElementBinding.For("slots", ".fixture-1", (e,k)=>FixtureSlots(e,k))});
            panel.Initialize();
            var list = panel.GetElement<IListView>("slots");
            Assert.IsNotNull(list);
            Assert.IsNull(panel.GetElement<ITextElement>("label"));
            var host = scope.Find("slots");
            var first = host[0]; var second = host[1];
            list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new SecondModel() };
            yield return null; yield return null;
            Assert.AreEqual("FirstModel", first[0].Text);
            Assert.AreEqual("SecondModel", second[0].Text);
            Assert.That(first.Layout.x, Is.EqualTo(20).Within(.1)); Assert.That(second.Layout.width, Is.EqualTo(120).Within(.1));
            Assert.AreEqual("rotate(20deg)", first.Style.Get("transform"));
            list.Items.Value = new ObservableList<ObservableObject> { new SecondModel() };
            Assert.AreEqual("SecondModel", first[0].Text);
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.None, second.Style.Display);
            list.Items.Value = null;
            Assert.AreEqual(2, host.ChildCount); Assert.IsTrue(first.IsValid); Assert.IsTrue(second.IsValid);
            list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new FirstModel() };
            Assert.AreEqual("FirstModel", first[0].Text); Assert.AreEqual("FirstModel", second[0].Text);
            Assert.AreNotEqual(global::URUI.Elements.DisplayStyle.None, second.Style.Display);
            panel.Destroy();
        }

        [UnityTest]
        public IEnumerator FixedSlotBindingFailureKeepsAuthorNodesAndRecoversWithoutLeakedProjection()
        {
            scope.LoadFragment("<div id='slots' class='fixture-1'><div class='fixture-slot'><span id='label' class='fixture-3 fixture-label'></span></div><div class='fixture-slot'><span id='label' class='fixture-5 fixture-label'></span></div></div>");
            var panel = new UruiView("fixed-slots-failure-page", scope,new UruiElementBinding[]{UruiElementBinding.For("slots", ".fixture-1", (e,k)=>FixtureSlots(e,k))});
            panel.Initialize();
            var list = panel.GetElement<IListView>("slots");
            var host = scope.Find("slots"); var first = host[0]; var second = host[1];
            Assert.Catch<System.Exception>(() => list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new UnregisteredModel() });
            Assert.AreEqual(2, host.ChildCount); Assert.IsTrue(first.IsValid); Assert.IsTrue(second.IsValid);
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.None, first.Style.Display);
            list.Items.Value = new ObservableList<ObservableObject> { new SecondModel(), new FirstModel() };
            Assert.AreEqual("SecondModel", first[0].Text); Assert.AreEqual("FirstModel", second[0].Text);
            ((System.IDisposable)list).Dispose();
            Assert.AreEqual(2, host.ChildCount); Assert.IsTrue(first.IsValid); Assert.IsTrue(second.IsValid);
            Assert.DoesNotThrow(() => panel.Destroy());
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedSlotDisposalDuringPresenterCreationCannotReenableOrLeakInstance()
        {
            scope.LoadFragment("<div id='slots' class='fixture-1'><div class='fixture-slot'><span id='label' class='fixture-3 fixture-label'></span></div></div>");
            var panel = new UruiView("fixed-slots-create-disposal", scope,new UruiElementBinding[]{UruiElementBinding.For("slots", ".fixture-1", (e,k)=>FixtureSlots(e,k))});
            panel.Initialize();
            var list = panel.GetElement<IListView>("slots");
            var bindings = 0; var destroyed = 0;
            GeneratedBindingRegistry.Register<FirstModel>((v, m) => new EmptyProbeContext(v, m, () => bindings++),
                () => new CallbackPresenter(() => ((System.IDisposable)list).Dispose(), () => destroyed++));
            list.Items.Value = new ObservableList<ObservableObject> { new FirstModel() };
            Assert.AreEqual(0, bindings);
            Assert.AreEqual(1, destroyed);
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.None, scope.Find("slots")[0].Style.Display);
            panel.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedSlotDisposalDuringAdapterConstructionReleasesUnregisteredAdapter()
        {
            scope.LoadFragment("<div id='slots' class='fixture-1'><div class='fixture-slot'><span id='label' class='fixture-3 fixture-label'></span></div></div>");
            System.Func<global::URUI.Elements.Element,string,UruiElement> create=null;
            var panel = new UruiView("fixed-slots-factory-disposal", scope,new UruiElementBinding[]{UruiElementBinding.For("slots", ".fixture-1", (e,k)=>FixtureSlots(e,k,(node,key)=>create(node,key)))});
            panel.Initialize();
            var list = panel.GetElement<IListView>("slots");
            var disposedAdapters = 0; var bindings = 0;
            GeneratedBindingRegistry.Register<FirstModel>((v, m) => new EmptyProbeContext(v, m, () => bindings++), () => new ProbePresenter());
            System.Func<global::URUI.Elements.Element, string, UruiElement> factory = (element, name) =>
            {
                if (name != "label") { return null; }
                var adapter = new CountingTextElement(element, name, () => disposedAdapters++);
                ((System.IDisposable)list).Dispose();
                return adapter;
            };
            create=factory;
            try
            {
                list.Items.Value = new ObservableList<ObservableObject> { new FirstModel() };
                Assert.AreEqual(1, disposedAdapters);
                Assert.AreEqual(0, bindings);
                Assert.IsTrue(scope.Find("slots")[0].IsValid);
            }
            finally { panel.Destroy(); }
            yield return null;
        }

        UruiView ProjectionPanel(bool throwing)
        {
            return new UruiView("prototype-page", scope, new[]{UruiElementBinding.For("prototype", ".projection-author", (element,name)=>new UruiProjectionTemplateElement(element,name,child=>new UruiView("projection",child,
                    new[]{UruiElementBinding.For("label", ".projection-label", (node,key)=>throwing?new ThrowingTextElement(node,key):new UruiTextElement(node,key))})))});
        }

        [UnityTest]
        public IEnumerator DisposingOwnerInsideFactoryInitializationOrBindingCannotOrphanProjection()
        {
            for(var phase=0;phase<4;phase++)
            {
                scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
                UruiProjectionTemplateElement owner=null;
                var point=phase;
                GeneratedBindingRegistry.Register<ReentrantModel>((view,model)=>
                {
                    if(point==2){owner.Dispose();}
                    return new EmptyProbeContext(view,model,null);
                },()=>new CallbackPresenter(()=>{if(point==3){owner.Dispose();}},null));
                var parent=scope.Root.Q(className:"projection-parent");
                owner=new UruiProjectionTemplateElement(parent[0],"projection",child=>
                {
                    if(point==0){owner.Dispose();}
                    return new UruiView("projection",child,new[]{UruiElementBinding.For("label", ".projection-label", (node,key)=>
                    {
                        if(point==1){owner.Dispose();}
                        return new UruiTextElement(node,key);
                    })});
                });
                var template=owner.Template.Value;
                Assert.Catch<System.ObjectDisposedException>(()=>template.Create(new ReentrantModel()),"销毁阶段："+phase);
                Assert.AreEqual(1,parent.ChildCount,"同步销毁不能遗留未登记的投影子树，阶段："+phase);
                Assert.DoesNotThrow(owner.Dispose);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ControlledTemplateFailureAndOwnerCleanupDoNotLeakClones()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var panel = ProjectionPanel(true);
            panel.Initialize();
            var owner = panel.GetElement<IProjectionTemplateElement>("prototype");
            var template = owner.Template.Value;
            var parent = scope.Root.Q(className:"projection-parent");
            Assert.Catch<System.Exception>(() => template.Create(new UnregisteredModel()));
            Assert.AreEqual(1, parent.ChildCount);
            try
            {
                var first = template.Create(new FirstModel());
                var second = template.Create(new SecondModel());
                first.Activate();
                second.Activate();
                Assert.Catch<System.Exception>(() => ((System.IDisposable)owner).Dispose());
                Assert.IsFalse(first.IsAlive);
                Assert.IsFalse(second.IsAlive);
                Assert.AreEqual(1, parent.ChildCount);
                Assert.DoesNotThrow(() => first.Dispose());
                Assert.DoesNotThrow(() => second.Dispose());
            }
            finally { panel.Destroy(); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CssPivotAndHiddenPrototypePreserveTheAuthoredCoordinateFrame()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var parent=scope.Root.Q(className:"projection-parent");
            parent.AddToClassList("projection-corner");
            parent.AddToClassList("projection-centered");
            var panel=ProjectionPanel(false);panel.Initialize();
            var template=panel.GetElement<IProjectionTemplateElement>("prototype").Template.Value;
            yield return null;yield return null;
            Assert.That(template.Layout.Width,Is.EqualTo(80).Within(.1));
            Assert.That(template.Layout.Height,Is.EqualTo(24).Within(.1));
            Assert.That(template.Layout.ParentPivotX,Is.EqualTo(1));
            Assert.That(template.Layout.ParentPivotY,Is.EqualTo(1));
            Assert.IsTrue(template.TryScreenToLocal(440,Screen.height-60,out var x,out var y));
            Assert.That(x,Is.Zero.Within(.1));Assert.That(y,Is.Zero.Within(.1));
            using var handle=template.Create(new FirstModel());handle.Activate();
            yield return null;yield return null;
            using var spatial=new UruiElement(parent[1],"point");
            spatial.LocalPositionValue.Value=new SpatialValue(-25,-15,0);
            yield return null;yield return null;
            var center=parent[1].WorldBound.center;
            Assert.That(center.x,Is.EqualTo(415).Within(.1));
            Assert.That(center.y,Is.EqualTo(75).Within(.1));
            parent.Style.SetWidth(600);parent.Style.SetHeight(300);
            yield return null;yield return null;yield return null;
            center=parent[1].WorldBound.center;
            Assert.That(center.x,Is.EqualTo(515).Within(.1));
            Assert.That(center.y,Is.EqualTo(125).Within(.1));
            panel.Destroy();
        }

        [UnityTest]
        public IEnumerator PointerContactKeepsBothCoordinatesAndSeparatesCancelFromRelease()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredPointerContact").text);
            var type=typeof(UruiElement).Assembly.GetType("FUI.Rendering.URUI.UruiPointerElement");
            Assert.IsNotNull(type,"缺少通用指针接触适配。");
            var plane=scope.Root.Q(className:"contact-plane");var surface=plane[0];
            var owner=(IPointerContactElement)System.Activator.CreateInstance(type,surface,"contact",plane);
            using var lifetime=(System.IDisposable)owner;
            var pressed=0;var moved=0;var released=0;var cancelled=0;PointerContact last=default;
            owner.PointerPressed.AddListener(value=>{pressed++;last=value;});owner.PointerMoved.AddListener(value=>{moved++;last=value;});
            owner.PointerReleased.AddListener(value=>released++);owner.PointerCancelled.AddListener(value=>cancelled++);
            yield return null;yield return null;
            var document=surface.Document;document.Surface.Input.UseUnityInput=false;
            var point=plane.LocalToWorld(new Vector2(160,80));
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down,point,71,true,pressedButtons:1);
            Assert.AreEqual(1,pressed);Assert.That(last.local.x,Is.EqualTo(-40).Within(.1));Assert.That(last.local.y,Is.EqualTo(20).Within(.1));
            Assert.That(last.screen.x,Is.EqualTo(point.x).Within(.1));Assert.That(last.screen.y,Is.EqualTo(Screen.height-point.y).Within(.1));
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down,point,72,true,pressedButtons:1);
            Assert.AreEqual(1,pressed);
            point=plane.LocalToWorld(new Vector2(190,60));document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Move,point,71,true,pressedButtons:1);
            Assert.AreEqual(1,moved);Assert.That(last.local.x,Is.EqualTo(-10).Within(.1));Assert.That(last.local.y,Is.EqualTo(40).Within(.1));
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Cancel,point,71,true);
            Assert.AreEqual(1,cancelled);Assert.AreEqual(0,released);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Cancel,point,72,true);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down,point,73,true,pressedButtons:1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up,point,73,true);
            Assert.AreEqual(1,released);Assert.AreEqual(1,cancelled);
        }

        [UnityTest]
        public IEnumerator AuthorGeometryIsReadyForInitialBindingWithoutWaitingForAFrame()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var view=new UruiView("initial-values",scope,new[]{UruiElementBinding.For("pose", ".projection-author", (node,key)=>new UruiElement(node,key))});
            try
            {
                view.Initialize();
                var size=view.GetElement<ISizeElement>("pose").SizeValue.Value;
                var position=view.GetElement<ILocalPositionElement>("pose").LocalPositionValue.Value;
                Assert.That(size.x,Is.EqualTo(80).Within(.01),"hasLayout="+scope.Root.Q(className:"projection-author").HasLayout+" nativeWidth="+scope.Root.Q(className:"projection-author").Layout.width);Assert.That(size.y,Is.EqualTo(24).Within(.01));
                Assert.That(position.x,Is.EqualTo(-150).Within(.01));Assert.That(position.y,Is.EqualTo(68).Within(.01));
            }
            finally{view.Destroy();}
            yield return null;
        }
        [TestCase(false)]
        [TestCase(true)]
        public void InitialGeometryDoesNotRecordDrawingForAnOrdinaryView(bool unrelatedPainter)
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            System.IDisposable painter = null; var paints = 0;
            if (unrelatedPainter)
            {
                var outside = scope.Document.CreateElement(); scope.Document.Body.Add(outside);
                outside.Style.Set("width", "10px"); outside.Style.Set("height", "10px");
                outside.Style.Set("--urui-decorator", "shader(unrelated-initial-painter)");
                painter = scope.Document.AttachPainter(outside, "unrelated-initial-painter", _ => { paints++; });
            }
            var view = new UruiView("initial-geometry-only", scope,
                new[] { UruiElementBinding.For("pose", ".projection-author", (node, key) => new UruiElement(node, key)) });
            try
            {
                using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "URUI.Render.Record", 64,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                view.Initialize(); recorder.Stop();
                Assert.That(recorder.Valid && !recorder.WrappedAround, Is.True);
                var size = view.GetElement<ISizeElement>("pose").SizeValue.Value;
                var position = view.GetElement<ILocalPositionElement>("pose").LocalPositionValue.Value;
                Assert.That(size.x, Is.EqualTo(80).Within(.01)); Assert.That(size.y, Is.EqualTo(24).Within(.01));
                Assert.That(position.x, Is.EqualTo(-150).Within(.01)); Assert.That(position.y, Is.EqualTo(68).Within(.01));
                long records = 0;
                for (var i = 0; i < recorder.Count; i++) { records += recorder.GetSample(i).Count; }
                Assert.That(records, Is.Zero, "初始化只需几何就绪，不应为每个子视图生成完整绘制计划。");
                Assert.That(paints, Is.Zero, "同文档其他子树的绘制器不应迫使普通视图完整录制。");
                if (unrelatedPainter)
                {
                    scope.Document.SynchronizeGeometry();
                    Assert.That(paints, Is.GreaterThan(0), "整份文档的同步入口仍保证所有绘制器就绪。");
                }
            }
            finally { painter?.Dispose(); view.Destroy(); }
        }

        [UnityTest]
        public IEnumerator InitialLayoutWithAnAttachedPainterHasReadyGeometry()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var node=scope.Root.Q(className:"projection-author");node.AddToClassList("initial-painter");var paints=0;
            using var painter=node.Document.AttachPainter(node,"initial-geometry",context=>
            {paints++;Assert.That(context.Size.x,Is.EqualTo(80).Within(.01));});
            var view=new UruiView("initial-painter",scope,new[]{UruiElementBinding.For("pose", ".projection-author", (e,k)=>new UruiElement(e,k))});
            try{Assert.DoesNotThrow(()=>view.Initialize());Assert.That(paints,Is.GreaterThan(0));}
            finally{view.Destroy();}
            yield return null;
        }
        [UnityTest]
        public IEnumerator InitialGeometryNotificationCanDestroyItsOwner()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            UruiView view=null;var notified=false;
            view=new UruiView("initial-disposal",scope,new[]{UruiElementBinding.For("pose", ".projection-author", (node,key)=>
            {
                var adapter=new UruiElement(node,key);
                adapter.SizeValue.OnValueChanged+=(_,value)=>{if(value.x>0){notified=true;view.Destroy();}};
                return adapter;
            })});
            try{Assert.DoesNotThrow(()=>view.Initialize());Assert.IsTrue(notified);}
            finally{view.Destroy();}
            yield return null;
        }
        [UnityTest]
        public IEnumerator AdapterConstructionReadsAnAlreadyLaidOutAuthorsValues()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var author=scope.Root.Q(className:"projection-author");
            var parent=author.Parent;
            // URUI tracks completion from the layout after a managed handle is first acquired.
            scope.Root.Document.SynchronizeLayout();
            Assert.IsTrue(author.HasLayout);Assert.IsTrue(parent.HasLayout);
            Assert.That(author.Layout.width,Is.EqualTo(80).Within(.01));
            using var adapter=new UruiElement(author,"pose");
            Assert.That(adapter.SizeValue.Value.x,Is.EqualTo(80).Within(.01));
            Assert.That(adapter.LocalPositionValue.Value.x,Is.EqualTo(-150).Within(.01));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScrollAxisNotificationCanDisposeItsOwner()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AxisScroll").text);
            var adapter=new UruiScrollElement(scope.Root.Q(className:"axis-scroll"),"scroll");
            var notified=false;
            adapter.Horizontal.OnValueChanged+=(_,value)=>{if(value){notified=true;adapter.Dispose();}};
            try{yield return null;yield return null;Assert.IsTrue(notified);}
            finally{adapter.Dispose();}
        }
        [UnityTest]
        public IEnumerator ScrollCallbackCanDisposeItsOwner()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AxisScroll").text);
            var node=scope.Root.Q(className:"axis-scroll");
            var adapter=new UruiScrollElement(node,"scroll");
            yield return null;yield return null;
            var notified=false;
            using var callback=node.RegisterCallback<global::URUI.Events.ScrollEvent>(_=>{notified=true;adapter.Dispose();});
            try
            {
                node.ScrollOffset=new Vector2(40,50);
                // Programmatic offsets do not promise a user event; exercise the notification boundary explicitly.
                node.SendEvent(new global::URUI.Events.ScrollEvent());
                yield return null;yield return null;
                Assert.IsTrue(notified);
            }
            finally{adapter.Dispose();}
        }
        [UnityTest]
        public IEnumerator DynamicFactoryCannotTransferACandidateAfterOwnerDisposal()
        {
            var host=scope.Find("host");UruiDynamicViewElement adapter=null;var destroyed=0;
            adapter=new UruiDynamicViewElement(host,"dynamic",_=>FixtureTemplate,(_,child)=>
            {
                adapter.Dispose();var candidate=FixtureItem(child);
                candidate.Own(new CleanupAction(()=>destroyed++));return candidate;
            });
            try
            {
                Assert.Throws<System.ObjectDisposedException>(()=>adapter.Source.Value="first");
                Assert.AreEqual(0,host.ChildCount,"释放的 owner 不能重新接管候选子树。");
                Assert.AreEqual(1,destroyed);
            }
            finally{adapter.Dispose();}
            yield return null;
        }
        [UnityTest]
        public IEnumerator SpatialPublicationDoesNotTurnAnEqualBindingRoundTripIntoAWrite()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var view=new UruiView("round-trip",scope,new[]{UruiElementBinding.For("pose", ".projection-author", (node,key)=>new UruiElement(node,key))});
            view.Initialize();var adapter=(UruiElement)view.GetElement<ILocalPositionElement>("pose");var node=adapter.Element;
            adapter.LocalPositionValue.OnValueChanged+=(_,value)=>adapter.LocalPositionValue.Value=value;
            var before=node.Style.Get("margin-left");
            node.Style.SetPixels("left",50);node.Document.SynchronizeLayout();adapter.SynchronizeExternalValues();
            yield return null;yield return null;
            Assert.AreEqual(before,node.Style.Get("margin-left"),"读取作者几何的双向回传不能产生新的布局写入。");
            view.Destroy();
        }
        sealed class CleanupAction:System.IDisposable
        {
            readonly System.Action action;
            public CleanupAction(System.Action action){this.action=action;}
            public void Dispose()=>action();
        }
        [UnityTest]
        public IEnumerator ScreenPositionConstructionPreservesAuthorUntilExplicitInputIncludingZero()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var item=scope.Root.Q(className:"projection-author");
            yield return null;yield return null;
            var original=item.WorldBound;
            using var adapter=new UruiScreenPositionElement(item,"screen");
            yield return null;yield return null;
            Assert.That(Vector2.Distance(item.WorldBound.center,original.center),Is.LessThan(.01f),"构造不得当成屏幕坐标输入。");
            adapter.ScreenPositionValue.Value=new PointerPosition(0,0);
            yield return null;yield return null;
            Assert.That(Vector2.Distance(item.WorldBound.center,new Vector2(0,Screen.height)),Is.LessThan(.1f),"首次显式零坐标也必须投影。");
        }
        [UnityTest]
        public IEnumerator InlineModelActivationPreservesOwnerVisibilityAndInteractivity()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var host=scope.Root.Q(className:"projection-author");
            host.SetAttribute("hidden","");
            host.SetEnabled(false);
            GeneratedBindingRegistry.Register<ReentrantModel>((view,model)=>new EmptyProbeContext(view,model,null),()=>new ProbePresenter());
            using var owner=new UruiModelElement(host,"inline",child=>new UruiView("inline",child));
            owner.ViewModel.Value=new ReentrantModel();
            yield return null;
            Assert.IsTrue(host.HasAttribute("hidden"),"内联子模型激活不能显示由父控件隐藏的作者节点。");
            Assert.IsFalse(host.EnabledInHierarchy,"子视图初始化不能重新启用父控件禁用的节点。");
            Assert.IsFalse(owner.Visible.Value);
            owner.Visible.Value=true;
            yield return null;
            Assert.IsFalse(host.HasAttribute("hidden"));
            Assert.AreNotEqual("none",host.ResolvedStyle.Get("display"));
            owner.Visible.Value=false;
            owner.ViewModel.Value=null;
            owner.ViewModel.Value=new ReentrantModel();
            yield return null;
            Assert.AreEqual("none",host.ResolvedStyle.Get("display"),"更换子模型同样必须保留父控件的隐藏状态。");
            Assert.IsFalse(host.EnabledInHierarchy);
        }

        [UnityTest]
        public IEnumerator InlineModelOwnerDisposalAtEveryConstructionBoundaryRollsBackControllers()
        {
            for(var phase=0;phase<4;phase++)
            {
                scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
                var host=scope.Root.Q(className:"projection-author");
                UruiModelElement owner=null;var disposed=0;var point=phase;
                GeneratedBindingRegistry.Register<ReentrantModel>((view,model)=>
                {
                    if(point==2){owner.Dispose();}
                    return new EmptyProbeContext(view,model,null);
                },()=>new CallbackPresenter(()=>{if(point==3){owner.Dispose();}},null));
                owner=new UruiModelElement(host,"inline",child=>
                {
                    var candidate=new UruiView("inline",child,new[]{UruiElementBinding.For("label", ".projection-label", (node,key)=>
                    {
                        if(point==1){owner.Dispose();}
                        return new UruiTextElement(node,key);
                    })});
                    candidate.Own(new CleanupAction(()=>disposed++));
                    if(point==0){owner.Dispose();}
                    return candidate;
                });
                Assert.Catch<System.ObjectDisposedException>(()=>owner.ViewModel.Value=new ReentrantModel(),"销毁阶段："+phase);
                Assert.AreEqual(1,disposed,"候选控制器必须释放一次，阶段："+phase);
                Assert.IsTrue(host.IsValid,"内联模型不拥有作者节点。");
                Assert.DoesNotThrow(owner.Dispose);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ScreenProjectionAndLocalRotationRemainIndependentOfParentRotation()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var parent=scope.Root.Q(className:"projection-parent");parent.AddToClassList("projection-rotated");var item=parent[0];
            var type=typeof(UruiElement).Assembly.GetType("FUI.Rendering.URUI.UruiScreenPositionElement");
            Assert.IsNotNull(type,"缺少通用屏幕位置投影适配。");
            var projection=(IScreenPositionElement)System.Activator.CreateInstance(type,item,"position");
            using var lifetime=(System.IDisposable)projection;
            using var spatial=new UruiElement(item,"pose");var local=spatial as ILocalQuaternionRotationElement;Assert.IsNotNull(local);
            yield return null;yield return null;
            projection.ScreenPositionValue.Value=new PointerPosition(240,Screen.height-180);
            var expected=Quaternion.Euler(0,0,45);local.LocalRotationValue.Value=new RotationValue(expected.x,expected.y,expected.z,expected.w);
            yield return null;yield return null;yield return null;
            Assert.That(Vector2.Distance(item.WorldBound.center,new Vector2(240,180)),Is.LessThan(.1));
            var value=local.LocalRotationValue.Value;Assert.That(Quaternion.Angle(expected,new Quaternion(value.x,value.y,value.z,value.w)),Is.LessThan(.1));
        }

        [UnityTest]
        public IEnumerator PixelTransformOriginRemainsAuthoredAfterBindingsAndResize()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var parent=scope.Root.Q(className:"projection-parent");parent.AddToClassList("projection-pixel-origin");
            var item=parent.Q(className:"projection-author");
            using var spatial=new UruiElement(item,"pixel-origin");
            yield return null;yield return null;
            spatial.LocalScaleValue.Value=new SpatialValue(2,2,1);
            var rotation=Quaternion.Euler(0,0,30);
            spatial.RotationValue.Value=new RotationValue(rotation.x,rotation.y,rotation.z,rotation.w);
            yield return null;yield return null;
            Assert.AreEqual("10px",item.ResolvedStyle.Get("--urui-transform-origin-x"));
            Assert.AreEqual("6px",item.ResolvedStyle.Get("--urui-transform-origin-y"));
            item.Style.SetWidth(160);item.Style.SetHeight(48);
            yield return null;yield return null;
            var origin=item.LocalToWorld(new Vector2(10,6));
            Assert.That(origin.x,Is.EqualTo(60).Within(.1));Assert.That(origin.y,Is.EqualTo(86).Within(.1));
        }

        [UnityTest]
        public IEnumerator RowSizeFitsFlowChildrenWithoutGrowingFromPositionedDecorations()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredRowSize").text);
            var row=scope.Root.Q(className:"measurement-row");
            using var sizing=new global::URUI.Controls.RowContentSize(row);
            for(var frame=0;frame<12;frame++){yield return null;}
            Assert.That(row.Layout.width,Is.EqualTo(681.0885).Within(.1));
            Assert.That(row.Q(className:"measurement-icon").Layout.x,Is.EqualTo(20).Within(.1));
            Assert.That(row.Q(className:"measurement-content").Layout.x,Is.EqualTo(159.08).Within(.1));
            for(var frame=0;frame<6;frame++){yield return null;}
            Assert.That(row.Layout.width,Is.EqualTo(681.0885).Within(.1));
        }

        [UnityTest]
        public IEnumerator ControlledTemplateCreatesIndependentInstancesAndOwnsTheirRelease()
        {
            scope.LoadFragment(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredProjection").text);
            var panel = ProjectionPanel(false);
            panel.Initialize();
            var owner = panel.GetElement<IProjectionTemplateElement>("prototype");
            Assert.IsNotNull(owner);
            var template = owner.Template.Value;
            yield return null;
            yield return null;
            Assert.That(template.Layout.Width, Is.EqualTo(80).Within(.1));
            Assert.That(template.Layout.ParentWidth, Is.EqualTo(400).Within(.1));
            var first = template.Create(new FirstModel());
            var second = template.Create(new SecondModel());
            first.Activate();
            second.Activate();
            yield return null;
            yield return null;
            var parent = scope.Root.Q(className:"projection-parent");
            Assert.AreEqual(3, parent.ChildCount);
            Assert.AreEqual("FirstModel", parent[1][0].Text);
            Assert.AreEqual("SecondModel", parent[2][0].Text);
            Assert.IsTrue(first.IsAlive);
            Assert.IsTrue(second.IsAlive);
            first.Dispose();
            Assert.IsFalse(first.IsAlive);
            Assert.IsTrue(second.IsAlive);
            Assert.AreEqual(2, parent.ChildCount);
            parent.Style.Width = 600;
            yield return null;
            yield return null;
            Assert.That(template.Layout.ParentWidth, Is.EqualTo(600).Within(.1));
            Assert.IsTrue(template.TryScreenToLocal(340, Screen.height - 160, out var x, out var y));
            Assert.That(x, Is.Zero.Within(.1));
            Assert.That(y, Is.Zero.Within(.1));
            panel.Destroy();
            Assert.IsFalse(second.IsAlive);
            Assert.DoesNotThrow(() => second.Dispose());
            Assert.Throws<System.ObjectDisposedException>(() => template.Create(new FirstModel()));
        }

        [UnityTest]
        public IEnumerator ApplicationTemplateResolver_RebuildsOnModelTypeChange()
        {
            var host = new ConfiguredModelElement(scope.Find("host"), "host");
            host.ViewModel.Value = new FirstModel();
            Assert.AreEqual("FirstModel", Label(host));
            host.ViewModel.Value = new SecondModel();
            Assert.AreEqual("SecondModel", Label(host));
            host.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlayList_PlacesIndependentPanelsAtSameOrigin()
        {
            var stage = root.GetComponent<UruiStage>();
            var panel = FuiHtmlFixtureAssets.OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;' id='panels' class='fixture-1'></div>", Layer.Panel);
            var view = new UruiView("overlay-list", panel,new UruiElementBinding[]{UruiElementBinding.For("panels", ".fixture-1", (e,k)=>FixtureList(e,k))});
            view.Initialize();
            view.GetElement<IListView>("panels").Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new SecondModel() };
            yield return null;
            yield return null;
            var host = panel.Find("panels");
            Assert.AreEqual(host[0].WorldBound, host[1].WorldBound);
            Assert.AreEqual(200, host[1].WorldBound.width);
            host[0][0].SetAttribute("id", "active-overlay-panel");
            host[0][0].Style.Set("position", "absolute");
            host[0][0].Style.Set("width", "100%");
            host[0][0].Style.Set("height", "100%");
            host[1][0].Style.Display = global::URUI.Elements.DisplayStyle.None;
            yield return null;
            yield return null;
            Assert.AreEqual("active-overlay-panel", panel.Root.Document.Pick(new Vector2(50, 50))?.Name);

            view.Destroy();
        }

        [UnityTest]
        public IEnumerator ItemTemplateExtensionReceivesEachIndependentModel()
        {
            var panel = FuiHtmlFixtureAssets.OpenPage(root.GetComponent<UruiStage>(), "<div id='list' class='fixture-1'></div>", Layer.Panel);
            var list = new PerItemTemplateList(panel.Find("list"), "list");
            list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new SecondModel() };
            Assert.AreEqual("FirstModel", list.Element[0].GetAttribute("data-owner"));
            Assert.AreEqual("SecondModel", list.Element[1].GetAttribute("data-owner"));
            list.Dispose();
            panel.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicModel_UsesDeclaredTemplateWithoutSourceBinding()
        {
            var host = FixtureDynamic(scope.Find("host"),"host");
            host.ViewModel.Value = new FirstModel();
            Assert.AreEqual("FirstModel", Label(host));
            host.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicModel_ReplacesProjectionWhenModelTypeChanges()
        {
            var host = FixtureDynamic(scope.Find("host"),"host");
            host.Source.Value = "ReviewDynamic";
            host.ViewModel.Value = new FirstModel();
            Assert.AreEqual("FirstModel", Label(host));
            host.ViewModel.Value = new SecondModel();
            Assert.AreEqual("SecondModel", Label(host));
            host.Dispose();
            yield return null;
        }

        [Test]
        public void DynamicFirstModelReusesThePreparedAuthoredProjection()
        {
            var calls = 0;
            using var adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) =>
            {
                calls++; return FixtureItem(child);
            });
            adapter.Source.Value = "ReviewDynamic";
            var previous = scope.Root[0];
            adapter.ViewModel.Value = new FirstModel();
            Assert.AreEqual(1, calls, "首次模型绑定不应再次实例化已经准备好的同一模板。");
            Assert.AreSame(previous, scope.Root[0]);
            Assert.AreEqual("FirstModel", Label(adapter));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DynamicFirstModelCanDisposeItsOwnerDuringPresenterCallbacks(bool onOpen)
        {
            UruiDynamicViewElement adapter = null; var destroyed = 0;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () =>
                onOpen ? (object)new OpeningPresenter(() => adapter.Dispose(), destroying: () => destroyed++) :
                new CallbackPresenter(() => adapter.Dispose(), () => destroyed++));
            adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) => FixtureItem(child));
            try
            {
                adapter.Source.Value = "ReviewDynamic";
                Assert.Throws<System.ObjectDisposedException>(() => adapter.ViewModel.Value = new ReentrantModel());
                Assert.Zero(scope.Root.ChildCount);
                Assert.AreEqual(1, destroyed);
            }
            finally { adapter.Dispose(); }
        }

        [Test]
        public void DynamicFirstModelRebuildsWhenModelSelectsAnotherSource()
        {
            using var adapter = new ConfiguredModelElement(scope.Root, "dynamic");
            adapter.Source.Value = "before-model"; var previous = scope.Root[0];
            adapter.ViewModel.Value = new FirstModel();
            Assert.AreNotSame(previous, scope.Root[0]); Assert.IsFalse(previous.IsValid);
            Assert.AreEqual("FirstModel", Label(adapter));
        }

        [Test]
        public void DynamicFirstModelCancellationKeepsItsSourceOnlyView()
        {
            UruiDynamicViewElement adapter = null;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () =>
                new OpeningPresenter(() => adapter.ViewModel.Value = null));
            using (adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) => FixtureItem(child)))
            {
                adapter.Source.Value = "ReviewDynamic"; var previous = scope.Root[0];
                Assert.Throws<System.OperationCanceledException>(() => adapter.ViewModel.Value = new ReentrantModel());
                Assert.AreSame(previous, scope.Root[0]); Assert.IsTrue(previous.IsValid);
                Assert.AreNotEqual(global::URUI.Elements.DisplayStyle.None, previous.Style.Display);
            }
        }

        [Test]
        public void DynamicFirstModelFailureRetainsThePreparedProjectionForRetry()
        {
            var fail = true; var destroyed = 0;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () =>
                new OpeningPresenter(() => { if (fail) throw new System.InvalidOperationException("first binding failure"); }, destroying: () => destroyed++));
            using var adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) => FixtureItem(child));
            adapter.Source.Value = "ReviewDynamic"; var previous = scope.Root[0];
            Assert.Throws<System.InvalidOperationException>(() => adapter.ViewModel.Value = new ReentrantModel());
            Assert.AreSame(previous, scope.Root[0]); Assert.IsTrue(previous.IsValid); Assert.AreEqual(1, destroyed);
            fail = false; adapter.ViewModel.Value = null; adapter.ViewModel.Value = new ReentrantModel();
            Assert.AreSame(previous, scope.Root[0]);
        }

        [Test]
        public void DynamicFirstModelCannotReplaceANewerSourceCreatedDuringOnOpen()
        {
            UruiDynamicViewElement adapter = null; var changed = false; var released = 0;
            GeneratedBindingRegistry.Register<ReentrantModel>((v, m) => new EmptyProbeContext(v, m, null), () =>
                new OpeningPresenter(() => { if (!changed) { changed = true; adapter.Source.Value = "replacement"; } }));
            using (adapter = new UruiDynamicViewElement(scope.Root, "dynamic", _ => FixtureTemplate, (_, child) =>
            {
                var view = FixtureItem(child); view.Own(new CleanupAction(() => released++)); return view;
            }))
            {
                adapter.Source.Value = "first"; var previous = scope.Root[0];
                Assert.Throws<System.OperationCanceledException>(() => adapter.ViewModel.Value = new ReentrantModel());
                Assert.AreEqual(1, scope.Root.ChildCount); Assert.AreNotSame(previous, scope.Root[0]);
                Assert.IsTrue(scope.Root[0].IsValid); Assert.IsFalse(previous.IsValid);
                Assert.AreEqual(1, released, "旧事务只能清理自己的子树。");
            }
            Assert.AreEqual(2, released);
        }

        [UnityTest]
        public IEnumerator DynamicSource_RendersStaticTemplateBeforeModelArrives()
        {
            var host = FixtureDynamic(scope.Find("host"),"host");
            host.Source.Value = "ReviewDynamic";
            Assert.AreEqual("static", Label(host));
            host.Dispose();
            yield return null;
        }

        static string Label(UruiDynamicViewElement host)
        {
            var nested = new UruiScope(host.Element);
            
            return nested.Find("label")?.Text;
        }

        [UnityTest]
        public IEnumerator DynamicReplacement_ReleasesCandidateWhenOldCleanupFails()
        {
            System.Func<global::URUI.Elements.Element, string, UruiElement> factory = (element, name) =>
                name == "label" ? new ThrowingTextElement(element, name) : null;
            var host = FixtureDynamic(scope.Find("host"),"host",factory);
            try
            {
                host.ViewModel.Value = new FirstModel();
                Assert.Catch<System.Exception>(() => host.ViewModel.Value = new SecondModel());
                Assert.AreEqual(0, host.Element.ChildCount, "neither failed old projection nor unadopted candidate may remain attached");
            }
            finally
            {
                try { host.Dispose(); } catch (System.InvalidOperationException) { }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListContent_ShrinkClampsScrollWithoutUserCommand()
        {
            var page = FuiHtmlFixtureAssets.OpenPage(root.GetComponent<UruiStage>(), "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:hidden;overflow-y:auto;' id='scroll' class='fixture-1'><div style='position:relative;display:flex;flex-direction:row;flex-wrap:wrap;width:180px;height:auto;' id='rows' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("fit-list", page,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k)), UruiElementBinding.For("rows", ".fixture-2", (e,k)=>FixtureList(e,k))});
            view.Initialize();
            var list = view.GetElement<IListView>("rows");
            var items = new ObservableList<ObservableObject>();
            for (var i = 0; i < 8; i++) items.Add(new FirstModel());
            list.Items.Value = items;
            var scroll = view.GetElement<IScrollViewElement>("scroll");
            for (var i = 0; i < 3; i++) yield return null;
            scroll.NormalizedScroll.Value = new ScrollPoint(0, 0);
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(page.Find("scroll").ScrollOffset.y, Is.EqualTo(100).Within(2));
            var userEvents = 0;
            scroll.ScrollChanged.AddListener(_ => userEvents++);
            list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new FirstModel() };
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(page.Find("scroll").ScrollOffset.y, Is.EqualTo(0).Within(2));
            Assert.AreEqual(new ScrollPoint(0, 1), scroll.NormalizedScroll.Value, "the binding must follow the post-layout range");
            Assert.AreEqual(0, userEvents, "layout clamping is not a user gesture");
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator ListDisposal_ReleasesAllRowsWhenOneAdapterThrows()
        {
            System.Func<global::URUI.Elements.Element, string, UruiElement> factory = (element, name) =>
                name == "label" ? new ThrowingTextElement(element, name) : null;
            var page = FuiHtmlFixtureAssets.OpenPage(root.GetComponent<UruiStage>(), "<div id='rows' class='fixture-1'></div>", Layer.Panel);
            var element = page.Find("rows");
            var list = FixtureList(element,"rows",factory);
            {
                list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new FirstModel() };
                Assert.Catch<System.Exception>(() => list.Dispose());
                Assert.AreEqual(0, element.ChildCount);
                Assert.IsNull(list.Element);
            }
            yield return null;
        }

        sealed class UnregisteredModel : ObservableObject { }

        [UnityTest]
        public IEnumerator ListBuildFailure_ReleasesPartialRows()
        {
            var page = FuiHtmlFixtureAssets.OpenPage(root.GetComponent<UruiStage>(), "<div id='rows' class='fixture-1'></div>", Layer.Panel);
            var element = page.Find("rows");
            var list = FixtureList(element,"rows");
            Assert.Catch<System.Exception>(() => list.Items.Value = new ObservableList<ObservableObject> { new FirstModel(), new UnregisteredModel() });
            Assert.AreEqual(0, element.ChildCount);
            list.Dispose();
            yield return null;
        }
    }
}
