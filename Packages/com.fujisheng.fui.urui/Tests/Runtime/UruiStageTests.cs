using System.Linq;
using System.Collections;
using FUI.Navigation;
using FUI.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiStageTests
    {
        sealed class ProbeElement : UruiElement
        {
            public ProbeElement(global::URUI.Elements.Element element, string name) : base(element, name) { }
        }
        sealed class ThrowingElement : UruiElement
        {
            public ThrowingElement(global::URUI.Elements.Element element, string name) : base(element, name) { }
            public override void Dispose() => throw new System.InvalidOperationException("review cleanup failure");
        }
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

        static UruiScope OpenPage(UruiStage stage, string body, Layer layer, string css = "")
        {
            stage.Surface.Input.UseUnityInput = false;
            stage.TextResources = new[] { new global::URUI.Resources.TextResource("/urui/behaviors.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/behaviors")) };
            var html = "<!doctype html><html><head><link rel='stylesheet' href='/urui/behaviors.css'><style>html,body{pointer-events:none;}body{margin:0;font-family:Arial;}body>*{pointer-events:auto;}" + css + "</style></head><body>" + body + "</body></html>";
            var text = new TextAsset(html);
            try { return stage.OpenPage(CompiledFixtureAssets.Create(text, "ui/stage-test.html", stage.TextResources), layer); }
            finally { Object.DestroyImmediate(text); }
        }
        static global::URUI.Elements.Element Hit(UruiStage stage, Vector2 point, params UruiScope[] pages)
        {
            global::URUI.Elements.Element target = null;
            var subscriptions = pages.Select(page => page.Root.RegisterCallback<global::URUI.Events.PointerDownEvent>(evt => target = evt.Target)).ToArray();
            try
            {
                stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, point, pressedButtons: 1);
                stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, point);
                return target;
            }
            finally { foreach (var subscription in subscriptions) subscription.Dispose(); }
        }

        GameObject root;
        GameObject cameraObject;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }
            if (cameraObject != null)
            {
                Object.Destroy(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator HidingDraggedElementOrAncestorCancelsWithoutCommit()
        {
            root = new GameObject("URUI Hidden Drag Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:0;top:0;width:300px;height:200px;' id='parent' class='fixture-1'><div style='position:absolute;left:20px;top:30px;width:160px;height:80px;background-color:#fff;' id='drag' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("hidden-drag", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("drag", ".fixture-2", (e,k)=>new UruiDragTextElement(e,k))});
            view.Initialize();
            var drag = view.GetElement<IPointerDragElement>("drag");
            var cancel = view.GetElement<IPointerDragCancellationElement>("drag");
            var cancelled = 0; var ended = 0; var moves = 0;
            cancel.DragCancelled.AddListener(value => cancelled++);
            drag.DragEnded.AddListener(value => ended++);
            drag.DragMoved.AddListener(value => moves++);
            yield return null; yield return null;
            var input = stage.Surface.Input;
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(80, 60), pressedButtons:1);
            view.GetElement<IVisibilityElement>("drag").Visible.Value = false;
            Assert.AreEqual(1, cancelled);
            input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(80, 60));
            Assert.AreEqual(0, ended);
            view.GetElement<IVisibilityElement>("drag").Visible.Value = true;
            yield return null; yield return null;
            drag.DragStarted.AddListener(value => view.GetElement<IVisibilityElement>("parent").Visible.Value = false);
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(80, 60), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(80, 60));
            Assert.AreEqual(2, cancelled); Assert.AreEqual(0, ended); Assert.AreEqual(1, moves);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator DragCapturesOutsideSuppressesChildClickAndKeepsNormalClick()
        {
            root = new GameObject("URUI Drag Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:20px;top:30px;width:160px;height:80px;' id='drag' class='fixture-1'><button style='position:absolute;left:0;top:0;width:160px;height:80px;' id='button' class='fixture-2'>item</button></div>", Layer.Panel);
            var view = new UruiView("drag", scope,new UruiElementBinding[]{UruiElementBinding.For("drag", ".fixture-1", (e,k)=>new UruiDragElement(e,k)), UruiElementBinding.For("button", ".fixture-2", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();
            var drag = view.GetElement<IPointerDragElement>("drag");
            Assert.IsNotNull(drag);
            var starts = 0; var moves = 0; var ends = 0; var clicks = 0;
            var position = default(PointerPosition);
            drag.DragStarted.AddListener(value => starts++);
            drag.DragMoved.AddListener(value => { moves++; position = value; });
            drag.DragEnded.AddListener(value => ends++);
            view.GetElement<IButtonElement>("button").Click.AddListener(() => clicks++);
            yield return null; yield return null;
            var input = stage.Surface.Input;
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(43, 52), pressedButtons:1);
            Assert.AreEqual(0, starts);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(260, 200), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(260, 200));
            Assert.AreEqual(1, starts); Assert.AreEqual(1, moves); Assert.AreEqual(1, ends); Assert.AreEqual(0, clicks);
            Assert.That(position.x, Is.EqualTo(260));
            Assert.That(position.y, Is.EqualTo(Screen.height - 200));
            Assert.IsFalse(scope.Find("button").HasPointerCapture(0));
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(40, 50));
            Assert.AreEqual(1, clicks);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator DragCancellationIsDistinctAndDisposeReleasesCapture()
        {
            root = new GameObject("URUI Drag Cancel Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:20px;top:30px;width:160px;height:80px;background-color:#fff;' id='drag' class='fixture-1'></div>", Layer.Panel);
            var view = new UruiView("drag-cancel", scope,new UruiElementBinding[]{UruiElementBinding.For("drag", ".fixture-1", (e,k)=>new UruiDragTextElement(e,k))});
            view.Initialize();
            var drag = view.GetElement<IPointerDragElement>("drag");
            Assert.IsNotNull(drag);
            var cancel = view.GetElement<IPointerDragCancellationElement>("drag");
            Assert.IsNotNull(cancel);
            var cancelled = 0; var ended = 0;
            var clones = 0;
            drag.DragStarted.AddListener(value =>
            {
                // 与实际拖拽预览相同：开始命令必须能同步克隆作者节点，不能仍停留在原生事件栈中。
                var clone=scope.Find("drag").Clone();scope.Root.Add(clone);clones++;clone.Destroy();
            });
            cancel.DragCancelled.AddListener(value => cancelled++);
            drag.DragEnded.AddListener(value => ended++);
            yield return null; yield return null;
            var input = stage.Surface.Input;
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(80, 60), pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Cancel, new Vector2(80, 60));
            Assert.AreEqual(1, cancelled); Assert.AreEqual(0, ended);
            input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(40, 50), pointerId:5, isTouch:true, pressedButtons:1);
            input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(80, 60), pointerId:5, isTouch:true, pressedButtons:1);
            var node = scope.Find("drag");
            view.Destroy();
            Assert.AreEqual(2, cancelled); Assert.AreEqual(0, ended);
            Assert.AreEqual(2, clones);
            Assert.IsFalse(node.IsValid);
        }

        [UnityTest]
        public IEnumerator WorldQuaternionCompensatesRotatedScaledParent()
        {
            root = new GameObject("URUI World Rotation Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:100px;top:100px;width:300px;height:200px;transform:rotate(-30deg) scale(2,1);'><div style='position:absolute;left:40px;top:40px;width:80px;height:20px;' id='item' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("world-rotation", scope,new UruiElementBinding[]{UruiElementBinding.For("item", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var parentRotation = Quaternion.Euler(0, 0, 30);
            var requested = Quaternion.Euler(0, 0, 90);
            view.GetElement<IQuaternionRotationElement>("item").RotationValue.Value = new RotationValue(requested.x, requested.y, requested.z, requested.w);
            for (var i = 0; i < 5; i++) { yield return null; }
            var item = scope.Find("item");
            var actual = item.LocalToWorld(new Vector2(80, 10)) - item.LocalToWorld(new Vector2(40, 10));
            var expected = Matrix4x4.TRS(Vector3.zero, parentRotation, new Vector3(2, 1, 1)).MultiplyVector((Quaternion.Inverse(parentRotation) * requested) * new Vector3(40, 0, 0));
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.1));
            Assert.That(actual.y, Is.EqualTo(-expected.y).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator WorldQuaternionSupportsThreeAxesNegativeParentScaleAndPreservesChildScale()
        {
            root = new GameObject("URUI 3D Rotation Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:100px;top:100px;width:300px;height:200px;transform:rotateX(-20deg) rotateY(30deg) rotateZ(-40deg) scale3d(-2,3,1);'><div style='position:absolute;left:40px;top:40px;width:80px;height:20px;transform:rotate(-15deg) scale3d(1.5,.75,2);' id='item' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("three-axis-rotation", scope,new UruiElementBinding[]{UruiElementBinding.For("item", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var requested = Quaternion.Euler(50, -20, 70);
            view.GetElement<IQuaternionRotationElement>("item").RotationValue.Value = new RotationValue(requested.x, requested.y, requested.z, requested.w);
            for (var i = 0; i < 5; i++) { yield return null; }
            var parentRotation = Quaternion.AngleAxis(20, Vector3.right) * Quaternion.AngleAxis(30, Vector3.up) * Quaternion.AngleAxis(40, Vector3.forward);
            var parentMatrix = Matrix4x4.TRS(Vector3.zero, parentRotation, new Vector3(-2, 3, 1));
            var localMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(parentRotation) * requested, new Vector3(1.5f, .75f, 2));
            var item = scope.Find("item");
            var origin = item.LocalToWorld(new Vector2(40, 10));
            foreach (var vector in new[] { new Vector2(40, 0), new Vector2(0, -10) })
            {
                var actual = item.LocalToWorld(new Vector2(40, 10) + vector) - origin;
                var expected = parentMatrix.MultiplyVector(localMatrix.MultiplyVector(new Vector3(vector.x, -vector.y, 0)));
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(.1));
                Assert.That(actual.y, Is.EqualTo(-expected.y).Within(.1));
            }
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator WorldQuaternionWaitsForAncestorWritesAndPublishesAncestorChanges()
        {
            root = new GameObject("URUI Pending Rotation Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:100px;top:100px;width:300px;height:200px;' id='parent' class='fixture-1'><div style='position:absolute;left:40px;top:40px;width:80px;height:20px;' id='item' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("pending-rotation", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("item", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var child = view.GetElement<IQuaternionRotationElement>("item");
            var parent = view.GetElement<IQuaternionRotationElement>("parent");
            child.RotationValue.Value = new RotationValue(0, 0, .70710678f, .70710678f);
            var parentQ = Quaternion.Euler(0, 0, 30);
            parent.RotationValue.Value = new RotationValue(parentQ.x, parentQ.y, parentQ.z, parentQ.w);
            for (var i = 0; i < 6; i++) { yield return null; }
            var item = scope.Find("item");
            var delta = item.LocalToWorld(new Vector2(80, 10)) - item.LocalToWorld(new Vector2(40, 10));
            Assert.That(delta.x, Is.Zero.Within(.1));
            Assert.That(delta.y, Is.EqualTo(-40).Within(.1));
            var host = (IExternalValueSynchronizationHost)view;
            host.SetExternalValueSources(new[] { (IExternalValueSource)child });
            host.ExternalValueSynchronizationEnabled = true;
            parentQ = Quaternion.Euler(0, 0, 60);
            parent.RotationValue.Value = new RotationValue(parentQ.x, parentQ.y, parentQ.z, parentQ.w);
            for (var i = 0; i < 5; i++) { yield return null; }
            var observed = child.RotationValue.Value;
            Assert.That(Quaternion.Angle(new Quaternion(observed.x, observed.y, observed.z, observed.w), Quaternion.Euler(0, 0, 120)), Is.LessThan(.05));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator ScaleWritePreservesAuthoredRotationAndMatrixSnapshotReportsAmbiguity()
        {
            root = new GameObject("URUI Authored Transform Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:100px;top:100px;width:80px;height:20px;transform:translate(12px,23px) rotate(-30deg) scale(2,1);' id='item' class='fixture-1'></div><div style='width:20px;height:20px;transform:matrix(1,0,0,1,12,23);' id='matrix' class='fixture-2'></div>", Layer.Panel);
            var view = new UruiView("authored-transform", scope,new UruiElementBinding[]{UruiElementBinding.For("item", ".fixture-1", (e,k)=>new UruiTextElement(e,k)), UruiElementBinding.For("matrix", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            view.GetElement<ILocalScaleElement>("item").LocalScaleValue.Value = new SpatialValue(3, 2, 1);
            for (var i = 0; i < 4; i++) { yield return null; }
            var item = scope.Find("item");
            var pivot = item.LocalToWorld(new Vector2(40, 10));
            var delta = item.LocalToWorld(new Vector2(80, 10)) - pivot;
            Assert.That(pivot.x, Is.EqualTo(152).Within(.1)); Assert.That(pivot.y, Is.EqualTo(133).Within(.1));
            Assert.That(delta.x, Is.EqualTo(120 * Mathf.Cos(Mathf.PI / 6)).Within(.1));
            Assert.That(delta.y, Is.EqualTo(-60).Within(.1));
            Assert.IsFalse(scope.Find("matrix").TryGetLocalTransform(out var matrix, out _));
            Assert.That(matrix.m03, Is.EqualTo(12).Within(.001)); Assert.That(matrix.m13, Is.EqualTo(23).Within(.001));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator RejectedTransformRequestIsReleasedAndCanBeRetriedAfterCorrection()
        {
            root = new GameObject("URUI Rejected Transform Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;width:300px;height:200px;transform:matrix(1,0,0,1,0,0);' id='parent' class='fixture-1'><div style='position:absolute;width:80px;height:20px;' id='item' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("rejected-transform", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("item", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("NotSupportedException: FUI transform writes require explicit"));
            view.GetElement<ILocalScaleElement>("parent").LocalScaleValue.Value = new SpatialValue(2, 2, 1);
            for (var i = 0; i < 3; i++) { yield return null; }
            scope.Find("parent").Style.Set("transform", "none");
            var requested = Quaternion.Euler(0, 0, 90);
            view.GetElement<IQuaternionRotationElement>("parent").RotationValue.Value = new RotationValue(requested.x, requested.y, requested.z, requested.w);
            view.GetElement<IWorldPositionElement>("item").WorldPositionValue.Value = new SpatialValue(240, Screen.height - 180, 0);
            for (var i = 0; i < 8; i++) { yield return null; }
            var item = scope.Find("item");
            var pivot = item.LocalToWorld(new Vector2(40, 10));
            Assert.That(pivot.x, Is.EqualTo(240).Within(.1)); Assert.That(pivot.y, Is.EqualTo(180).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator WorldWriteWaitsThroughSingularParentAndResumesWhenScaleRestores()
        {
            root = new GameObject("URUI Singular Transform Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;width:300px;height:200px;transform:scale(0,1);' id='parent' class='fixture-1'><div style='position:absolute;width:80px;height:20px;' id='item' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("singular-transform", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("item", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            view.GetElement<IWorldPositionElement>("item").WorldPositionValue.Value = new SpatialValue(240, Screen.height - 180, 0);
            for (var i = 0; i < 3; i++) { yield return null; }
            Assert.IsFalse(scope.Find("parent").TryWorldToLocal(new Vector2(240, 180), out _));
            scope.Find("parent").Style.Set("transform", "scale(2,1)");
            for (var i = 0; i < 6; i++) { yield return null; }
            var item = scope.Find("item");
            var pivot = item.LocalToWorld(new Vector2(40, 10));
            Assert.That(pivot.x, Is.EqualTo(240).Within(.1)); Assert.That(pivot.y, Is.EqualTo(180).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator QuaternionRotationAndScaleComposeAroundDeclaredPivot()
        {
            root = new GameObject("URUI Rotation Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:100px;top:100px;width:80px;height:20px;' id='item' class='fixture-1'></div>", Layer.Panel);
            var view = new UruiView("rotation", scope,new UruiElementBinding[]{UruiElementBinding.For("item", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var rotation = view.GetElement<IQuaternionRotationElement>("item");
            Assert.IsNotNull(rotation);
            var q = Quaternion.Euler(0, 0, 90);
            rotation.RotationValue.Value = new RotationValue(q.x, q.y, q.z, q.w);
            view.GetElement<ILocalScaleElement>("item").LocalScaleValue.Value = new SpatialValue(2, 3, 1);
            yield return null;
            yield return null;
            var item = scope.Find("item");
            var pivot = item.LocalToWorld(new Vector2(40, 10));
            var right = item.LocalToWorld(new Vector2(80, 10)) - pivot;
            var top = item.LocalToWorld(new Vector2(40, 0)) - pivot;
            Assert.That(pivot.x, Is.EqualTo(140).Within(.1));
            Assert.That(pivot.y, Is.EqualTo(110).Within(.1));
            Assert.That(right.x, Is.Zero.Within(.1));
            Assert.That(right.y, Is.EqualTo(-80).Within(.1));
            Assert.That(top.x, Is.EqualTo(-30).Within(.1));
            Assert.That(top.y, Is.Zero.Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator SpatialListenerWriteIsPreservedAndPendingUnloadCanBeDisposed()
        {
            root = new GameObject("URUI Spatial Listener Write Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:40px;height:20px;' id='child' class='fixture-1'></div>", Layer.Panel);
            var view = new UruiView("spatial-listener-write", scope,new UruiElementBinding[]{UruiElementBinding.For("child", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var source = view.GetElement<IWorldPositionElement>("child");
            var size = view.GetElement<ISizeElement>("child");
            source.WorldPositionValue.OnValueChanged += (oldValue, newValue) => size.SizeValue.Value = new ScrollPoint(90, 40);
            var host = (IExternalValueSynchronizationHost)view;
            host.SetExternalValueSources(new[] { (IExternalValueSource)source });
            host.ExternalValueSynchronizationEnabled = true;
            scope.Find("child").Style.SetLeft(70);
            for (var i = 0; i < 4; i++) { yield return null; }
            Assert.That(scope.Find("child").Layout.width, Is.EqualTo(90).Within(.1));
            Assert.That(size.SizeValue.Value.x, Is.EqualTo(90).Within(.1));
            source.WorldPositionValue.Value = new SpatialValue(240, Screen.height - 220, 0);
            scope.Document.Unload();
            Assert.DoesNotThrow(() => view.Destroy());
        }

        [UnityTest]
        public IEnumerator WorldWriteWaitsForAllPendingAncestorsToSettle()
        {
            root = new GameObject("URUI Spatial Three Level Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:400px;height:300px;' id='grand' class='fixture-1'><div style='position:absolute;left:100px;top:60px;width:100px;height:100px;' id='parent' class='fixture-2'><div style='position:absolute;left:10px;top:10px;width:20px;height:20px;' id='child' class='fixture-3'></div></div></div>", Layer.Panel);
            var view = new UruiView("three-level-spatial", scope,new UruiElementBinding[]{UruiElementBinding.For("grand", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("parent", ".fixture-2", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("child", ".fixture-3", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            yield return null;
            yield return null;
            var grand = view.GetElement<ILocalPositionElement>("grand");
            ((IExternalValueSource)grand).SynchronizeExternalValues();
            var grandBefore = grand.LocalPositionValue.Value;
            var parentNode = scope.Find("parent");
            var parentBefore = parentNode.LocalToWorld(parentNode.Layout.size * .5f);
            var childNode = scope.Find("child");
            var childBefore = childNode.LocalToWorld(childNode.Layout.size * .5f);
            grand.LocalPositionValue.Value = new SpatialValue(grandBefore.x + 100, grandBefore.y, 0);
            view.GetElement<IWorldPositionElement>("parent").WorldPositionValue.Value = new SpatialValue(parentBefore.x - 100, Screen.height - parentBefore.y, 0);
            view.GetElement<IWorldPositionElement>("child").WorldPositionValue.Value = new SpatialValue(childBefore.x + 50, Screen.height - childBefore.y, 0);
            for (var i = 0; i < 7; i++) { yield return null; }
            var actual = childNode.LocalToWorld(childNode.Layout.size * .5f);
            Assert.That(actual.x, Is.EqualTo(childBefore.x + 50).Within(.1));
            Assert.That(actual.y, Is.EqualTo(childBefore.y).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator SpatialNotificationCanDestroyTheViewReentrantly()
        {
            root = new GameObject("URUI Spatial Reentry Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:40px;height:20px;' id='child' class='fixture-1'></div>", Layer.Panel);
            var view = new UruiView("spatial-reentry", scope,new UruiElementBinding[]{UruiElementBinding.For("child", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var source = view.GetElement<IWorldPositionElement>("child");
            var destroyed = false;
            source.WorldPositionValue.OnValueChanged += (oldValue, newValue) => { destroyed = true; view.Destroy(); };
            var host = (IExternalValueSynchronizationHost)view;
            host.SetExternalValueSources(new[] { (IExternalValueSource)source });
            host.ExternalValueSynchronizationEnabled = true;
            scope.Find("child").Style.SetLeft(70);
            yield return null;
            yield return null;
            Assert.IsTrue(destroyed);
            Assert.AreEqual(0, view.Elements.Count);
        }

        [UnityTest]
        public IEnumerator SimultaneousParentLocalAndChildWorldWritesConvergeToScreenTarget()
        {
            root = new GameObject("URUI Spatial Parent Child Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:300px;height:200px;' id='parent' class='fixture-1'><div style='position:absolute;left:10px;top:20px;width:40px;height:20px;' id='child' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("parent-child-spatial", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("child", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            yield return null;
            yield return null;
            view.GetElement<ILocalPositionElement>("parent").LocalPositionValue.Value = new SpatialValue(100, 0, 0);
            view.GetElement<IWorldPositionElement>("child").WorldPositionValue.Value = new SpatialValue(240, Screen.height - 220, 0);
            for (var i = 0; i < 5; i++) { yield return null; }
            var child = scope.Find("child");
            var pivot = child.LocalToWorld(child.Layout.size * .5f);
            Assert.That(pivot.x, Is.EqualTo(240).Within(.1));
            Assert.That(pivot.y, Is.EqualTo(220).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator WorldPositionBeforeLayoutResolvesThroughScaledBorderedParent()
        {
            root = new GameObject("URUI World Spatial Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:300px;height:200px;border-width:3px;border-color:#fff;' id='parent' class='fixture-1'><div style='position:absolute;left:10px;top:20px;width:40px;height:20px;margin-left:4px;' id='child' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("world-spatial", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("child", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            scope.Find("parent").SetTransform(Vector2.zero, new Vector2(1.5f, 1.5f));
            view.GetElement<IWorldPositionElement>("child").WorldPositionValue.Value = new SpatialValue(240, Screen.height - 220, 0);
            yield return null;
            yield return null;
            yield return null;
            var child = scope.Find("child");
            var pivot = child.LocalToWorld(child.Layout.size * .5f);
            Assert.That(pivot.x, Is.EqualTo(240).Within(.1));
            Assert.That(pivot.y, Is.EqualTo(220).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator LayoutCallbackUnloadStopsLaterCallbacksSafely()
        {
            root = new GameObject("URUI Layout Unload Stage");
            var stage = root.AddComponent<UruiStage>();
            var page = OpenPage(stage, "<div style='width:20px;height:20px;'>x</div>", Layer.Panel);
            var document = page.Document;
            var laterCalls = 0;
            document.LayoutUpdated += doc => doc.Unload();
            document.LayoutUpdated += doc => laterCalls++;
            yield return null;
            yield return null;
            Assert.IsFalse(document.IsLoaded);
            Assert.AreEqual(0, laterCalls);
        }

        [UnityTest]
        public IEnumerator ExternalSpatialSourcesTrackAncestorMovementAndStopWhenUnbound()
        {
            root = new GameObject("URUI Spatial Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:300px;height:200px;' id='parent' class='fixture-1'><div style='position:absolute;left:10px;top:20px;width:40px;height:20px;' id='child' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("spatial", scope,new UruiElementBinding[]{UruiElementBinding.For("parent", ".fixture-1", (e,k)=>new UruiElement(e,k)), UruiElementBinding.For("child", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var host = (object)view as IExternalValueSynchronizationHost;
            Assert.IsNotNull(host);
            var child = view.GetElement<IWorldPositionElement>("child");
            Assert.IsNotNull(child);
            host.SetExternalValueSources(new[] { (IExternalValueSource)child });
            host.ExternalValueSynchronizationEnabled = true;
            yield return null;
            yield return null;
            Assert.That(child.WorldPositionValue.Value.x, Is.EqualTo(70).Within(.1));
            Assert.That(child.WorldPositionValue.Value.y, Is.EqualTo(Screen.height - 90).Within(.1));
            scope.Find("parent").Style.Left = 90;
            yield return null;
            yield return null;
            Assert.That(child.WorldPositionValue.Value.x, Is.EqualTo(120).Within(.1));
            host.ExternalValueSynchronizationEnabled = false;
            scope.Find("parent").Style.Left = 110;
            yield return null;
            yield return null;
            Assert.That(child.WorldPositionValue.Value.x, Is.EqualTo(120).Within(.1));
            host.ClearExternalValueSources();
            host.ExternalValueSynchronizationEnabled = true;
            yield return null;
            Assert.That(child.WorldPositionValue.Value.x, Is.EqualTo(120).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator LocalZeroWriteBeforeLayoutCentersAuthoredPivot()
        {
            root = new GameObject("URUI Local Spatial Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:60px;width:300px;height:200px;'><div style='position:absolute;left:10px;top:20px;width:40px;height:20px;' id='child' class='fixture-2'></div></div>", Layer.Panel);
            var view = new UruiView("local-spatial", scope,new UruiElementBinding[]{UruiElementBinding.For("child", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            view.GetElement<ILocalPositionElement>("child").LocalPositionValue.Value = SpatialValue.zero;
            yield return null;
            yield return null;
            yield return null;
            var child = scope.Find("child");
            var pivot = child.LocalToWorld(child.Layout.size * .5f);
            Assert.That(pivot.x, Is.EqualTo(190).Within(.1));
            Assert.That(pivot.y, Is.EqualTo(160).Within(.1));
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator Stage_OpensFragment_AndBindsNamedElements()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='width:200px; height:40px;' id='title' class='fixture-1'>hello</div>",
                Layer.Panel);
            var view = new UruiView("test", scope,new UruiElementBinding[]{UruiElementBinding.For("title", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            yield return null;

            var title = view.GetElement<ITextElement>("title");
            Assert.IsNotNull(title);
            Assert.AreEqual("hello", title.Text.Value);

            title.Text.Value = "changed";
            Assert.AreEqual("changed", title.Text.Value);
        }

        [UnityTest]
        public IEnumerator Stage_ScopesResolveDuplicateNamesIndependently()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var first = OpenPage(stage, "<div id='title' class='fixture-1'>one</div>", Layer.Panel);
            var second = OpenPage(stage, "<div id='title' class='fixture-1'>two</div>", Layer.Panel);
            var firstView = new UruiView("first", first,new UruiElementBinding[]{UruiElementBinding.For("title", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            var secondView = new UruiView("second", second,new UruiElementBinding[]{UruiElementBinding.For("title", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            firstView.Initialize();
            secondView.Initialize();
            yield return null;

            Assert.AreEqual("one", firstView.GetElement<ITextElement>("title").Text.Value);
            Assert.AreEqual("two", secondView.GetElement<ITextElement>("title").Text.Value);
        }

        [UnityTest]
        public IEnumerator DeclaredFactoryBuildsOnlyItsExplicitFacet()
        {
            root=new GameObject("Explicit factory stage");var stage=root.AddComponent<UruiStage>();
            var scope=OpenPage(stage, "<div class='review-custom'></div>",Layer.Panel);
            var view=new UruiView("custom",scope,new[]{UruiElementBinding.For("review-custom", ".review-custom", (e,k)=>new ProbeElement(e,k))});
            view.Initialize();Assert.IsInstanceOf<ProbeElement>(view.GetElement<IElement>("review-custom"));
            view.Destroy();yield return null;
        }

        [UnityTest]
        public IEnumerator ParentView_DoesNotBindInsideNestedListScope()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var view = new UruiView("parent", OpenPage(stage, "<div id='items' class='fixture-1'><div id='child' class='fixture-2'>nested</div></div>", Layer.Panel),new UruiElementBinding[]{UruiElementBinding.For("items", ".fixture-1", (e,k)=>FixtureList(e,k))});
            view.Initialize();
            Assert.IsNotNull(view.GetElement<IListView>("items"));
            Assert.IsNull(view.GetElement<ITextElement>("child"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LeaseDispose_ReleasesAdaptersAsWellAsMarkup()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<button id='button' class='fixture-1'>click</button>", Layer.Panel);
            var view = new UruiView("lease", scope,new UruiElementBinding[]{UruiElementBinding.For("button", ".fixture-1", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();
            var button = (UruiElement)view.GetElement<IButtonElement>("button");
            var lease = new UruiViewLease(view, scope);
            lease.Dispose();
            Assert.IsNull(button.Element);
            lease.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ViewOrder_UpdatesNativeStackingOrder()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div>page</div>", Layer.Panel);
            var view = new UruiView("order", scope,new UruiElementBinding[]{});
            view.Order = 17;
            Assert.AreEqual(17, view.Order);
            Assert.IsNull(scope.Root.Style.Get("z-index"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitNativeButtonExposesOnlyItsCommandCapability()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<button style='position:absolute;left:10px;top:10px;width:100px;height:100px;' id='image-button' class='fixture-1'></button>", Layer.Panel);
            var view = new UruiView("image-button", scope,new UruiElementBinding[]{UruiElementBinding.For("image-button", ".fixture-1", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();
            var button = view.GetElement<IButtonElement>("image-button");
            Assert.IsNotNull(button);
            var clicks = 0;
            button.Click.AddListener(() => clicks++);
            yield return null;
            yield return null;
            var point = new Vector2(50, 50);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Move, point);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, point, pressedButtons: 1);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, point);
            Assert.AreEqual(1, clicks);
            button.Interactable.Value = false;
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, point, pressedButtons: 1);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, point);
            Assert.AreEqual(1, clicks, "disabled image must not invoke its command");
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyHigherLayers_DoNotInterceptPageInput()
        {
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<button id='hit-button' style='position:absolute;left:10px;top:10px;width:100px;height:100px;' class='fixture-1'>hit</button>", Layer.Panel);
            var view = new UruiView("hit", scope,new UruiElementBinding[]{UruiElementBinding.For("hit-button", ".fixture-1", (e,k)=>new UruiButtonElement(e,k))}) { Order = 0 }; // Navigator assigns local order zero.
            var emptyOverlay = OpenPage(stage, "", Layer.Overlay);
            yield return null; yield return null;
            Assert.AreEqual("hit-button", Hit(stage, new Vector2(50, 50), scope, emptyOverlay)?.Name);
            emptyOverlay.Destroy();
            scope.Destroy();
        }

        [UnityTest]
        public IEnumerator LayerPriority_DominatesLocalPageOrder()
        {
            root = new GameObject("URUI Layer Priority Stage");
            var stage = root.AddComponent<UruiStage>();
            var panel = OpenPage(stage, "<button id='panel-hit' style='position:absolute;left:10px;top:10px;width:100px;height:100px;' class='fixture-1'>panel</button>", Layer.Panel);
            var popup = OpenPage(stage, "<button id='popup-hit' style='position:absolute;left:10px;top:10px;width:100px;height:100px;' class='fixture-1'>popup</button>", Layer.Popup);
            var panelView = new UruiView("panel", panel,new UruiElementBinding[]{UruiElementBinding.For("panel-hit", ".fixture-1", (e,k)=>new UruiButtonElement(e,k))}) { Order = 500 };
            var popupView = new UruiView("popup", popup,new UruiElementBinding[]{UruiElementBinding.For("popup-hit", ".fixture-1", (e,k)=>new UruiButtonElement(e,k))}) { Order = 0 };
            yield return null;
            yield return null;
            Assert.AreEqual("popup-hit", Hit(stage, new Vector2(50, 50), panel, popup)?.Name);
            panelView.Destroy();
            popupView.Destroy();
        }

        [UnityTest]
        public IEnumerator ViewDestroy_CleansOtherAdaptersAndScopeAfterDisposeFailure()
        {
            System.Func<global::URUI.Elements.Element, string, UruiElement> factory = (element, name) =>
                name == "review-throw" ? new ThrowingElement(element, name) : null;
            root = new GameObject("URUI Test Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='review-throw' class='fixture-1'></div><button id='other' class='fixture-2'>other</button>", Layer.Panel);
            var view = new UruiView("cleanup", scope,new UruiElementBinding[]{UruiElementBinding.For("review-throw", ".fixture-1", (e,k)=>factory(e,k)), UruiElementBinding.For("other", ".fixture-2", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();
            var other = (UruiElement)view.GetElement<IElement>("other");
            Assert.Throws<System.InvalidOperationException>(() => view.Destroy());
            Assert.IsNull(other.Element);
            Assert.IsFalse(scope.Root.IsValid);
            Assert.DoesNotThrow(() => view.Destroy());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImageSource_PreservesAuthoredImageUntilAnExplicitDynamicSourceChanges()
        {
            root = new GameObject("URUI Image Stage"); root.SetActive(false);
            var stage = root.AddComponent<UruiStage>();
            stage.Images = new[]{new global::URUI.Resources.ImageAsset("authored.png",Texture2D.whiteTexture),new global::URUI.Resources.ImageAsset("dynamic.png",Texture2D.whiteTexture)};
            root.SetActive(true);
            var scope = OpenPage(stage, global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredImage").text, Layer.Panel);
            var view = new UruiView("image", scope,new[]{UruiElementBinding.For("image", ".authored-image", (element,name)=>new UruiImageElement(element,name))});
            view.Initialize();
            var image = view.GetElement<IImageElement>("image");
            var node=scope.Root.Q(className:"authored-image");
            Assert.IsNull(image.SpriteSource.Value);
            image.SpriteSource.Value=null;
            Assert.AreEqual("authored.png",node.GetAttribute("src"));
            image.SpriteSource.Value="dynamic.png";
            Assert.AreEqual("dynamic.png",node.GetAttribute("src"));
            image.SpriteSource.Value = null;
            Assert.IsNull(node.GetAttribute("src"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScrollView_BindsNormalizedPositionWithoutUserCommand()
        {
            root = new GameObject("URUI Scroll Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:hidden;overflow-y:auto;' id='scroll' class='fixture-1'><div style='display:block;height:600px;width:180px;'></div></div>", Layer.Panel);
            var view = new UruiView("scroll", scope,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k))});
            view.Initialize();
            var scroll = view.GetElement<IScrollViewElement>("scroll");
            Assert.IsNotNull(scroll);
            var userChanges = 0;
            scroll.ScrollChanged.AddListener(_ => userChanges++);
            scroll.NormalizedScroll.Value = new ScrollPoint(0, 0);
            for (var i = 0; i < 4; i++) yield return null;
            var native = scope.Find("scroll");
            Assert.Greater(native.ScrollOffset.y, 400, $"range={native.ScrollSize}, viewport={native.ViewportRect}, content={native.ContentContainer.Layout}");
            Assert.AreEqual(0, userChanges);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Move, new Vector2(50, 50));
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Wheel, new Vector2(50, 50), delta: new Vector2(0, -1));
            for (var i = 0; i < 3; i++) yield return null;
            Assert.Greater(userChanges, 0, "native wheel input must notify the binding");
            Assert.Less(native.ScrollOffset.y, 500);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator ScrollToIndex_WaitsForLayoutAndCentersTheItem()
        {
            root = new GameObject("URUI Indexed Scroll Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:hidden;overflow-y:auto;' id='scroll' class='fixture-1'><div style='display:block;width:180px;height:600px;' id='rows' class='fixture-2'><div style='display:block;height:100px;'></div><div style='display:block;height:100px;'></div><div style='display:block;height:100px;'></div><div style='display:block;height:100px;'></div><div style='display:block;height:100px;'></div><div style='display:block;height:100px;'></div></div></div>", Layer.Panel);
            using var adapter=new UruiScrollElement(scope.Find("scroll"),"scroll",scope.Find("rows"));adapter.RequestScrollToIndex(3);
            for (var i = 0; i < 4; i++) yield return null;
            var native = scope.Find("scroll");
            Assert.That(native.ScrollOffset.y, Is.EqualTo(300).Within(2), $"range={native.ScrollSize}, viewport={native.ViewportRect}, content={native.ContentContainer.Layout}");
            scope.Destroy();
        }

        [UnityTest]
        public IEnumerator ExplicitRootFacetsShareVisibilityWithoutImplicitAliases()
        {
            root=new GameObject("Explicit root facets");var stage=root.AddComponent<UruiStage>();
            var scope=OpenPage(stage, "<button class='card'><img class='skin'/></button>",Layer.Panel);
            var view=new UruiView("root",scope,new UruiElementBinding[]{
                UruiElementBinding.For("card", ".card", (e,k)=>new UruiImageElement(e,k,e.Q<global::URUI.Controls.Image>(className:"skin"))),
                UruiElementBinding.For("card", ".card", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();Assert.AreEqual(2,view.Elements.Count);Assert.IsNull(view.GetElement<IElement>("__Root"));
            view.GetElement<IImageElement>("card").Visible.Value=false;
            Assert.IsFalse(view.GetElement<IButtonElement>("card").Visible.Value);
            Assert.DoesNotThrow(()=>view.Destroy());yield return null;
        }

        [UnityTest]
        public IEnumerator ImageGroupKeepsButtonGeometryChildrenAndSourceOwnership()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            root = new GameObject("URUI Image Group Stage"); root.SetActive(false);
            var stage = root.AddComponent<UruiStage>();
            stage.Images = new[] { new global::URUI.Resources.ImageAsset("group.png", texture) };
            root.SetActive(true);
            var scope = OpenPage(stage, "<button style='position:absolute;left:20px;top:30px;width:160px;height:80px;padding:0;' id='group' class='fixture-1'><img style='position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;' class='fixture-image-surface'/><span id='label' class='fixture-3 fixture-label'>child</span></button>", Layer.Panel);
            var view = new UruiView("image-group", scope,new UruiElementBinding[]{UruiElementBinding.For("group", ".fixture-1", (e,k)=>new UruiImageElement(e,k,e.Q<global::URUI.Controls.Image>(className:"fixture-image-surface"))), UruiElementBinding.For("group", ".fixture-1", (e,k)=>new UruiButtonElement(e,k)), UruiElementBinding.For("label", ".fixture-3", (e,k)=>new UruiTextElement(e,k))}); view.Initialize();
            var image = view.GetElement<IImageElement>("group");
            Assert.IsNotNull(image);
            Assert.IsNotNull(view.GetElement<ITextElement>("label"));
            var button = view.GetElement<IButtonElement>("group"); var clicks = 0; button.Click.AddListener(() => clicks++);
            image.SpriteSource.Value = "group.png";
            view.GetElement<IColorElement>("group").Tint.Value = new Rgba(1, .3f, .2f, 1);
            yield return null; yield return null;
            var native = scope.Find("group"); var surface = native[0];
            Assert.AreEqual("group.png", surface.GetAttribute("src"));
            Assert.IsTrue(ColorUtility.TryParseHtmlString(surface.Style.Get("--urui-image-color"), out var tint));
            Assert.That(tint.g, Is.EqualTo(.3).Within(1f / 255));
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(60, 65), pressedButtons: 1);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(60, 65));
            Assert.AreEqual(1, clicks);
            image.SpriteSource.Value = null;
            Assert.IsNull(surface.GetAttribute("src"));
            view.GetElement<IVisibilityElement>("group").Visible.Value = false;
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.None, native.Style.Display);
            Assert.AreEqual("child", scope.Find("label").Text);
            view.Destroy(); Object.Destroy(root); yield return null;
            Assert.IsTrue(texture != null); Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator ProgressImageUsesRealFillExtentAndRemainsCallerOwned()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            root = new GameObject("URUI Progress Image Stage");
            root.SetActive(false);
            var stage = root.AddComponent<UruiStage>();
            stage.Images = new[] { new global::URUI.Resources.ImageAsset("fill.png", texture) };
            root.SetActive(true);
            var scope = OpenPage(stage, "<div style='position:absolute;left:40px;top:50px;width:200px;height:20px;' id='progress' class='fixture-1'><img class='declared-progress-image' src='fill.png' style='position:absolute;left:0;top:0;width:0;height:100%;pointer-events:none;'/></div>", Layer.Panel);
            var view = new UruiView("progress-image", scope,new UruiElementBinding[]{UruiElementBinding.For("progress", ".fixture-1", (e,k)=>new UruiProgressElement(e,k,new global::URUI.Controls.ProgressIndicator(e,e.Q(className:"declared-progress-image"))))});
            view.Initialize();
            var progress = view.GetElement<IFloatValueElement>("progress");
            progress.Value.Value = .25f;
            yield return null; yield return null;
            var image = scope.Find("progress").Q(className: "declared-progress-image");
            Assert.IsNotNull(image);
            Assert.AreEqual("fill.png", image.GetAttribute("src"));
            Assert.That(image.Layout.width, Is.EqualTo(50).Within(.01));
            progress.Value.Value = 1;
            yield return null;
            Assert.That(image.Layout.width, Is.EqualTo(200).Within(.01));
            progress.Value.Value = 0;
            yield return null;
            Assert.That(image.Layout.width, Is.Zero.Within(.01));
            view.Destroy();
            Object.Destroy(root);
            yield return null;
            Assert.IsTrue(texture != null);
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator BorrowedImage_RemainsCallerOwnedAfterPageAndStageDestruction()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            root = new GameObject("URUI Borrowed Image Stage");
            root.SetActive(false);
            var stage = root.AddComponent<UruiStage>();
            stage.Images = new[] { new global::URUI.Resources.ImageAsset("borrowed.png", texture) };
            root.SetActive(true);
            var page = OpenPage(stage, "<img src='borrowed.png' style='width:20px;height:20px;' id='image' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("borrowed", page,new UruiElementBinding[]{UruiElementBinding.For("image", ".fixture-1", (e,k)=>new UruiImageElement(e,k))});
            view.Initialize();
            yield return null;
            view.GetElement<IImageElement>("image").SpriteSource.Value = null;
            view.Destroy();
            yield return null;
            Assert.IsTrue(texture != null, "a view must not destroy its caller's texture");
            Object.Destroy(root);
            yield return null;
            Assert.IsTrue(texture != null, "a stage must release its registration without destroying a borrowed texture");
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator HorizontalScroll_UsesAuthoredAxis()
        {
            root = new GameObject("URUI Horizontal Scroll Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:auto;overflow-y:hidden;' id='scroll' class='fixture-1'><div style='display:block;width:600px;height:60px;'></div></div>", Layer.Panel);
            var view = new UruiView("horizontal", scope,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k))});
            view.Initialize();
            var scroll = view.GetElement<IScrollViewElement>("scroll");
            Assert.IsTrue(scroll.Horizontal.Value);
            Assert.IsFalse(scroll.Vertical.Value);
            scroll.NormalizedScroll.Value = new ScrollPoint(1, 1);
            for (var i = 0; i < 4; i++) yield return null;
            Assert.Greater(scope.Find("scroll").ScrollOffset.x, 300);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator FractionalBothAxisScroll_DoesNotEmitIntermediateUserCommands()
        {
            root = new GameObject("URUI Both Axis Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:auto;overflow-y:auto;' id='scroll' class='fixture-1'><div style='display:block;width:601px;height:601px;'></div></div>", Layer.Panel);
            var view = new UruiView("both", scope,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k))});
            view.Initialize();
            var scroll = view.GetElement<IScrollViewElement>("scroll");
            var userEvents = 0;
            scroll.ScrollChanged.AddListener(_ => userEvents++);
            scroll.NormalizedScroll.Value = new ScrollPoint(.33f, .33f);
            for (var i = 0; i < 4; i++) yield return null;
            Assert.Greater(scope.Find("scroll").ScrollOffset.x, 100);
            Assert.Greater(scope.Find("scroll").ScrollOffset.y, 100);
            Assert.AreEqual(0, userEvents);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator FractionalSingleAxisScroll_DoesNotEmitUserCommandAfterRounding()
        {
            root = new GameObject("URUI Fractional Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='position:absolute;left:10px;top:10px;width:200px;height:100px;;overflow-x:auto;overflow-y:hidden;' id='scroll' class='fixture-1'><div style='display:block;width:601px;height:60px;'></div></div>", Layer.Panel);
            var view = new UruiView("fractional", scope,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k))});
            view.Initialize();
            var scroll = view.GetElement<IScrollViewElement>("scroll");
            var userEvents = 0;
            scroll.ScrollChanged.AddListener(_ => userEvents++);
            scroll.NormalizedScroll.Value = new ScrollPoint(.33f, 1);
            for (var i = 0; i < 4; i++) yield return null;
            Assert.AreEqual(0, userEvents);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator StageCreatesPageWithoutAdoptingAnExistingDocument()
        {
            root = new GameObject("URUI Existing Document Stage");
            root.SetActive(false);
            var existing = root.AddComponent<global::URUI.Documents.Document>();
            var stage = root.AddComponent<UruiStage>();
            root.SetActive(true);
            var scope = OpenPage(stage, "<div style='width:200px;height:100px;;overflow-x:hidden;overflow-y:auto;' id='scroll' class='fixture-1'></div>", Layer.Panel);
            Assert.AreNotSame(existing, scope.Document);
            var view = new UruiView("reuse", scope,new UruiElementBinding[]{UruiElementBinding.For("scroll", ".fixture-1", (e,k)=>new UruiScrollElement(e,k))});
            view.Initialize();
            Assert.IsNotNull(view.GetElement<IScrollViewElement>("scroll"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeValueChanges_UpdateBindingPropertiesBeforeCommands()
        {
            root = new GameObject("URUI Value Binding Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<input type='checkbox' id='toggle' class='fixture-1'/><input type='range' min='0' max='1' step='0.01' value='0' id='slider' class='fixture-2'/>", Layer.Panel);
            var view = new UruiView("values", scope,new UruiElementBinding[]{UruiElementBinding.For("toggle", ".fixture-1", (e,k)=>new UruiToggleElement(e,k)), UruiElementBinding.For("slider", ".fixture-2", (e,k)=>new UruiSliderElement(e,k))});
            view.Initialize();
            var toggle = view.GetElement<IToggleElement>("toggle");
            var slider = view.GetElement<ISliderElement>("slider");
            bool? toggleAtCommand = null;
            float? sliderAtCommand = null;
            toggle.ValueChanged.AddListener(_ => toggleAtCommand = toggle.IsOn.Value);
            slider.ValueChanged.AddListener(_ => sliderAtCommand = slider.Value.Value);
            ((global::URUI.Controls.Toggle)scope.Find("toggle")).Value = true;
            ((global::URUI.Controls.Slider)scope.Find("slider")).Value = .65f;
            Assert.IsTrue(toggle.IsOn.Value);
            Assert.AreEqual(true, toggleAtCommand);
            Assert.That(slider.Value.Value, Is.EqualTo(.65f).Within(.001f));
            Assert.That(sliderAtCommand, Is.EqualTo(.65f).Within(.001f));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FloatSlider_DefaultModePreservesFractionalValues()
        {
            root = new GameObject("URUI Float Slider Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<input type='range' min='0' max='1' value='0' id='slider' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("fractional-slider", scope,new UruiElementBinding[]{UruiElementBinding.For("slider", ".fixture-1", (e,k)=>new UruiSliderElement(e,k))});
            view.Initialize();
            var slider = view.GetElement<ISliderElement>("slider");
            slider.Value.Value = .37f;
            Assert.That(((global::URUI.Controls.Slider)scope.Find("slider")).Value, Is.EqualTo(.37f).Within(.00001f));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Slider_RangeBindingsAndWholeNumbersReachNativeControl()
        {
            root = new GameObject("URUI Slider Range Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<input type='range' min='0' max='1' value='0' id='slider' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("slider-range", scope,new UruiElementBinding[]{UruiElementBinding.For("slider", ".fixture-1", (e,k)=>new UruiSliderElement(e,k))});
            view.Initialize();
            var slider = view.GetElement<ISliderElement>("slider");
            var native = (global::URUI.Controls.Slider)scope.Find("slider");
            var commands = 0;
            slider.ValueChanged.AddListener(_ => commands++);
            slider.MaxValue.Value = 5.5f;
            slider.MinValue.Value = .5f;
            Assert.AreEqual(.5f, native.LowValue);
            Assert.AreEqual(5.5f, native.HighValue);
            slider.WholeNumbers.Value = true;
            slider.Value.Value = 2.5f;
            Assert.AreEqual(System.Math.Round(native.Value), native.Value);
            Assert.AreEqual(native.Value, slider.Value.Value);
            slider.WholeNumbers.Value = false;
            Assert.AreEqual(.5f, native.LowValue);
            Assert.AreEqual(5.5f, native.HighValue);
            Assert.AreEqual(0, commands, "programmatic configuration changes must not emit user commands");
            native.Value = 4;
            Assert.AreEqual(1, commands, "native user changes still emit the command");
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Stage_RespectsStylesheetPointerIgnoreOnFragmentRoot()
        {
            root = new GameObject("URUI CSS Pointer Stage");
            var stage = root.AddComponent<UruiStage>();
            var lower = OpenPage(stage, "<button id='lower-css-hit' style='position:absolute;left:10px;top:10px;width:100px;height:100px;' class='fixture-1'>lower</button>", Layer.Panel);
            var overlay = OpenPage(stage, "<div class='pass-through' style='position:absolute;left:0;top:0;width:200px;height:200px;'></div>", Layer.Overlay, ".pass-through{pointer-events:none;}");
            yield return null;
            yield return null;
            Assert.AreEqual("lower-css-hit", Hit(stage, new Vector2(50, 50), lower, overlay)?.Name);
            overlay.Root[0].RemoveFromClassList("pass-through");
            yield return null;
            Assert.AreSame(overlay.Root[0], Hit(stage, new Vector2(50, 50), lower, overlay));
            overlay.Root.SetInnerHtml("<div class='pass-through' style='position:absolute;left:0;top:0;width:200px;height:200px;'></div>", "ui/replacement-fragment.html");
            yield return null;
            Assert.AreEqual("lower-css-hit", Hit(stage, new Vector2(50, 50), lower, overlay)?.Name);

        }

        [UnityTest]
        public IEnumerator SliderNormalization_NotifiesTwoWayBindingOnceWithCanonicalValue()
        {
            root = new GameObject("URUI Slider TwoWay Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<input type='range' min='0' max='10' value='0' id='slider' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("slider-twoway", scope,new UruiElementBinding[]{UruiElementBinding.For("slider", ".fixture-1", (e,k)=>new UruiSliderElement(e,k))});
            view.Initialize();
            var slider = view.GetElement<ISliderElement>("slider");
            slider.WholeNumbers.Value = true;
            var modelValue = new FUI.Binding.BindableProperty<float>(0);
            var notifications = new System.Collections.Generic.List<float>();
            slider.Value.OnValueChanged += (_, value) =>
            {
                notifications.Add(value);
                if (notifications.Count < 8) modelValue.Value = value;
            };
            modelValue.OnValueChanged += (_, value) => slider.Value.Value = value;
            slider.Value.Value = 2.5f;
            Assert.That(notifications, Is.EqualTo(new[] { 3f }));
            Assert.AreEqual(3, modelValue.Value);
            Assert.AreEqual(3, slider.Value.Value);
            view.Destroy();
            modelValue.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TextRichFlag_PreservesLiteralMarkupAndCanToggleCurrentContent()
        {
            root = new GameObject("URUI Text Flag Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("text-flag", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var text = view.GetElement<ITextElement>("label");
            var native = scope.Find("label");
            text.RichText.Value = false;
            text.Text.Value = "<b>A & B</b>";
            Assert.AreEqual("<b>A & B</b>", native.Text);
            text.RichText.Value = true;
            Assert.AreEqual("span", native[0].TagName);
            text.RichText.Value = false;
            Assert.AreEqual("<b>A & B</b>", native.Text);
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RichText_UnknownTagsRemainLiteralInsteadOfCreatingNativeNodes()
        {
            root = new GameObject("URUI Text Tags Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("text-tags", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var text = view.GetElement<ITextElement>("label");
            text.RichText.Value = true;
            text.Text.Value = "A <img src='unrequested.png'/> & B";
            Assert.AreEqual("A <img src='unrequested.png'/> & B", scope.Find("label").Text);
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContentText_UsesSameRichFlagWithoutLosingLastAssignedSource()
        {
            root = new GameObject("URUI Content Text Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("content-text", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var text = view.GetElement<ITextElement>("label");
            var content = view.GetElement<ITextContentElement>("label");
            text.RichText.Value = true;
            content.Content.Value = "<color=#FF0000>cost</color>";
            Assert.AreEqual("span", scope.Find("label")[0].TagName);
            text.RichText.Value = false;
            Assert.AreEqual("<color=#FF0000>cost</color>", scope.Find("label").Text);
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImageTint_WritesAValidNativeColorProperty()
        {
            root = new GameObject("URUI Image Tint Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<img style='width:20px;height:20px;' id='image' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("image-tint", scope,new UruiElementBinding[]{UruiElementBinding.For("image", ".fixture-1", (e,k)=>new UruiImageElement(e,k))});
            view.Initialize();
            view.GetElement<IColorElement>("image").Tint.Value = new Rgba(1, 0, 0, 1);
            Assert.IsNotNull(scope.Find("image").Style.Get("--urui-image-color"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormattedText_UsesRichPolicyAndDoesNotResurrectClearedContent()
        {
            root = new GameObject("URUI Formatted Text Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("formatted-text", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var text = view.GetElement<ITextElement>("label");
            var formatted = view.GetElement<IFormattedTextElement>("label");
            formatted.FormattedTextValue.Value = new FormattedTextRequest("<color=red>{0}</color>", "50");
            Assert.AreEqual("span", scope.Find("label")[0].TagName);
            Assert.AreEqual("50", scope.Find("label")[0].Text);
            formatted.FormattedTextValue.Value = new FormattedTextRequest("");
            Assert.AreEqual("", scope.Find("label").Text);
            text.RichText.Value = false;
            Assert.AreEqual("", scope.Find("label").Text);
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RichText_SizeAndMixedLiteralTextArePreserved()
        {
            root = new GameObject("URUI Mixed Text Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("mixed-text", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            view.GetElement<ITextElement>("label").Text.Value = "A & B <size=45>Lv.1</size> <unknown>";
            var native = scope.Find("label");
            Assert.AreEqual("A & B ", native[0].Text);
            Assert.AreEqual("span", native[1].TagName);
            Assert.AreEqual("Lv.1", native[1].Text);
            Assert.AreEqual("45px", native[1].Style.Get("font-size"));
            Assert.AreEqual(" <unknown>", native[2].Text);
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPlainTextFlagAndFontSizeDoNotDependOnProcessCulture()
        {
            root = new GameObject("URUI Locale Text Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='label' class='fixture-1 fixture-label'>initial</div>", Layer.Panel);
            var view = new UruiView("locale-text", scope,new UruiElementBinding[]{UruiElementBinding.For("label", ".fixture-1", (e,k)=>{ var text=new UruiTextElement(e,k);text.RichText.Value=false;return text; })});
            view.Initialize();
            var text = view.GetElement<ITextElement>("label");
            Assert.IsFalse(text.RichText.Value);
            text.Text.Value = "<b>literal</b>";
            Assert.AreEqual("<b>literal</b>", scope.Find("label").Text);
            var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                text.FontSize.Value = 12.5f;
                Assert.AreEqual("12.5dp", scope.Find("label").Style.Get("font-size"));
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = oldCulture; }
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CloneKeepsAuthoredClassAttributesConsistentWithSelectors()
        {
            root = new GameObject("URUI Clone Class Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/CustomNumeric").text, Layer.Panel);
            var clone = scope.Root.Q(className: "custom-number").Clone();
            scope.Root.Add(clone);
            Assert.IsTrue(clone.ClassListContains("custom-number"));
            Assert.IsTrue(clone.HasAttribute("class"), "Cloned class attributes and selectors must describe the same node.");
            StringAssert.Contains("custom-number", clone.GetAttribute("class"));
            clone.RemoveAttribute("class");
            Assert.IsFalse(clone.ClassListContains("custom-number"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TypedCustomNumericPropertiesDriveAuthoredVariablesAndPreserveCascade()
        {
            root = new GameObject("URUI Custom Numeric Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/CustomNumeric").text, Layer.Panel);
            scope.Document.AddStyleSheet(UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/CustomNumeric.styles"), "ui/CustomNumeric.styles.css");
            var native = scope.Root.Q(className: "custom-number");
            var child = native.Q(className: "custom-inherited");
            yield return null;
            Assert.That(native.Layout.width, Is.EqualTo(20).Within(.01f));
            Assert.That(child.Layout.width, Is.EqualTo(20).Within(.01f));
            native.Style.SetPixels("--extent", 73);
            Assert.AreEqual("73px", native.Style.Get("--extent"));
            Assert.IsNull(child.Style.Get("--extent"), "Inherited variables are not inline overrides.");
            yield return null;
            Assert.That(native.Layout.width, Is.EqualTo(73).Within(.01f));
            Assert.That(child.Layout.width, Is.EqualTo(73).Within(.01f));
            native.Style.Remove("--extent");
            yield return null;
            Assert.That(native.Layout.width, Is.EqualTo(20).Within(.01f));
        }

        [UnityTest]
        public IEnumerator Visibility_HiddenTextLayoutStaysHiddenBeforeBinding()
        {
            root = new GameObject("URUI Hidden Text Layout");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div class='text-layout' id='hint' hidden><span class='text-content'>hint</span></div>", Layer.Panel,
                "[hidden]{display:none;}" + global::UnityEngine.Resources.Load<TextAsset>("URUI/behaviors").text);
            yield return null;
            Assert.AreEqual("none", scope.Find("hint").ResolvedStyle.Get("display"));
        }

        [UnityTest]
        public IEnumerator Visibility_InitialFalseAppliesAndSharedLeaseRestoresAuthorDisplay()
        {
            root = new GameObject("URUI Initial False Visibility");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div id='hint' hidden style='display:flex;'>hint</div>", Layer.Panel);
            var node = scope.Find("hint");
            using var first = new UruiElement(node, "hint");
            using var second = new UruiTextElement(node, "hint-text");
            Assert.AreSame(first.Visible, second.Visible);
            first.Visible.Value = false;
            yield return null;
            Assert.AreEqual("none", node.ResolvedStyle.Get("display"), "首次相同false也必须落实作者隐藏状态。");
            second.Visible.Value = true;
            yield return null;
            Assert.IsFalse(node.HasAttribute("hidden"));
            Assert.AreEqual("flex", node.ResolvedStyle.Get("display"));
            first.Dispose();
            second.Visible.Value = false;
            yield return null;
            Assert.AreEqual("none", node.ResolvedStyle.Get("display"));
            second.Visible.Value = true;
            yield return null;
            Assert.AreEqual("flex", node.ResolvedStyle.Get("display"));
        }

        [UnityTest]
        public IEnumerator Visibility_InitialHiddenAttributeCanBeShownByFirstBinding()
        {
            root = new GameObject("URUI Authored Hidden Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/InitialVisibility").text, Layer.Panel);
            using var adapter = new UruiElement(scope.Root.Q(className: "hidden-by-default"), "hidden");
            Assert.IsFalse(adapter.Visible.Value, "HTML hidden must supply the initial binding state before layout.");
            yield return null;
            Assert.AreEqual("none", adapter.Element.ResolvedStyle.Get("display"));
            adapter.Visible.Value = true;
            yield return null;
            Assert.IsFalse(adapter.Element.HasAttribute("hidden"));
            Assert.AreNotEqual("none", adapter.Element.ResolvedStyle.Get("display"));
            adapter.Visible.Value = false;
            yield return null;
            Assert.AreEqual("none", adapter.Element.ResolvedStyle.Get("display"));
        }

        [UnityTest]
        public IEnumerator Visibility_RestoresAuthoredDisplayInsteadOfForcingFlex()
        {
            root = new GameObject("URUI Display State Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div style='display:block;' id='host' class='fixture-1'>label</div><div style='display:none;' id='initially-hidden' class='fixture-2'>hidden</div>", Layer.Panel);
            var view = new UruiView("display-state", scope,new UruiElementBinding[]{UruiElementBinding.For("host", ".fixture-1", (e,k)=>new UruiTextElement(e,k)), UruiElementBinding.For("initially-hidden", ".fixture-2", (e,k)=>new UruiTextElement(e,k))});
            view.Initialize();
            var visible = view.GetElement<IVisibilityElement>("host");
            var native = scope.Find("host");
            foreach (var display in new[] { "block", "inline-block", "flex", "inline-flex" })
            {
                native.Style.Set("display", display);
                visible.Visible.Value = false;
                Assert.AreEqual("none", native.Style.Get("display"));
                visible.Visible.Value = true;
                Assert.AreEqual(display, native.Style.Get("display"));
            }
            native.Style.Remove("display");
            visible.Visible.Value = false;
            visible.Visible.Value = true;
            Assert.IsNull(native.Style.Get("display"), "no author inline display means restore stylesheet/default cascade");
            view.GetElement<IVisibilityElement>("initially-hidden").Visible.Value = true;
            Assert.AreNotEqual("none", scope.Find("initially-hidden").Style.Get("display"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScopeVisibility_RestoresAuthorLayoutAcrossNavigationCoverage()
        {
            root = new GameObject("URUI Scope Display Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<div>content</div>", Layer.Panel);
            scope.Root.Style.Set("display", "block");
            var view = new UruiView("scope-display", scope,new UruiElementBinding[]{});
            view.Initialize();
            Assert.AreEqual("block", scope.Root.Style.Get("display"));
            view.Visible = false;
            view.Visible = true;
            Assert.AreEqual("block", scope.Root.Style.Get("display"));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitImageClearDoesNotUseAnImplicitFallback()
        {
            root = new GameObject("URUI Image Fallback Stage"); root.SetActive(false);
            var stage = root.AddComponent<UruiStage>();
            stage.Images = new[]{new global::URUI.Resources.ImageAsset("other",Texture2D.whiteTexture),new global::URUI.Resources.ImageAsset("fallback",Texture2D.whiteTexture),new global::URUI.Resources.ImageAsset("authored",Texture2D.whiteTexture)};
            root.SetActive(true);
            var scope = OpenPage(stage, "<img src='authored' id='image' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("fallback-image", scope,new UruiElementBinding[]{UruiElementBinding.For("image", ".fixture-1", (e,k)=>new UruiImageElement(e,k))});
            view.Initialize();
            var image = view.GetElement<IImageElement>("image");
            image.SpriteSource.Value = "other";
            image.SpriteSource.Value = null;
            Assert.IsNull(scope.Find("image").GetAttribute("src"));
            image.SpriteSource.Value = "other";
            image.SpriteSource.Value = null;
            Assert.IsNull(scope.Find("image").GetAttribute("src"), "default clear semantics must stay unchanged");
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FloatSlider_PreservesAuthoredFractionalInitialValueWithStep()
        {
            root = new GameObject("URUI Authored Float Slider Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<input type='range' min='0' max='1' step='0.01' value='0.37' id='slider' class='fixture-1'/>", Layer.Panel);
            var view = new UruiView("authored-slider", scope,new UruiElementBinding[]{UruiElementBinding.For("slider", ".fixture-1", (e,k)=>new UruiSliderElement(e,k))});
            view.Initialize();
            Assert.That(view.GetElement<IFloatValueElement>("slider").Value.Value, Is.EqualTo(.37f).Within(.00001f));
            view.Destroy();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProgressBinding_FillsDeclaredLayoutWithoutUserCommands()
        {
            root = new GameObject("URUI Semantic Progress Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<urui-progress-bar min='0' max='1' style='position:absolute;left:40px;top:50px;width:200px;height:20px;' id='progress' class='fixture-1'></urui-progress-bar>", Layer.Panel);
            var view = new UruiView("semantic-progress", scope,new UruiElementBinding[]{UruiElementBinding.For("progress", ".fixture-1", (e,k)=>new UruiProgressElement(e,k))});
            view.Initialize();
            var progress = view.GetElement<IFloatValueElement>("progress");
            Assert.IsNotNull(progress);
            var commands = 0;
            progress.ValueChanged.AddListener(_ => commands++);
            progress.Value.Value = .25f;
            yield return null;
            yield return null;
            var native = scope.Find("progress");
            var fill = native.Q(className: "urui-progress-fill");
            Assert.AreEqual("absolute", native.Style.Get("position"));
            Assert.That(fill.Layout.width, Is.EqualTo(50).Within(.01));
            progress.Value.Value = 1;
            yield return null;
            Assert.That(fill.Layout.width, Is.EqualTo(200).Within(.01));
            progress.Value.Value = 0;
            yield return null;
            Assert.That(fill.Layout.width, Is.EqualTo(0).Within(.01));
            Assert.AreEqual(0, commands);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator DuplicateName_ClickBindingPrefersNativeButton()
        {
            root = new GameObject("URUI Shared Name Stage");
            var stage = root.AddComponent<UruiStage>();
            var scope = OpenPage(stage, "<img style='position:absolute;left:10px;top:10px;width:100px;height:100px;' id='shared' class='fixture-1'/><button style='position:absolute;left:10px;top:10px;width:100px;height:100px;' id='shared' class='fixture-2'>go</button>", Layer.Panel);
            var view = new UruiView("shared-name", scope,new UruiElementBinding[]{UruiElementBinding.For("shared", ".fixture-1", (e,k)=>new UruiImageElement(e,k)), UruiElementBinding.For("shared", ".fixture-2", (e,k)=>new UruiButtonElement(e,k))});
            view.Initialize();
            Assert.AreEqual("img", ((UruiElement)view.GetElement<IImageElement>("shared")).Element.TagName);
            var button = view.GetElement<IButtonElement>("shared");
            Assert.AreEqual("button", ((UruiElement)button).Element.TagName);
            var clicks = 0;
            button.Click.AddListener(() => clicks++);
            yield return null;
            yield return null;
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(50, 50), pressedButtons: 1);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(50, 50));
            Assert.AreEqual(1, clicks);
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator ScopeWrapper_DoesNotInterceptOutsideAuthoredContent()
        {
            root = new GameObject("URUI Partial Overlay Stage");
            var stage = root.AddComponent<UruiStage>();
            var lower = OpenPage(stage, "<button id='lower-hit' style='position:absolute;left:10px;top:10px;width:100px;height:100px;' class='fixture-1'>lower</button>", Layer.Panel);
            var upper = OpenPage(stage, "<div style='position:absolute;left:150px;top:10px;width:20px;height:20px;'>status</div>", Layer.Overlay);
            upper.Root.Style.Set("width", "200px");
            upper.Root.Style.Set("height", "200px");
            yield return null;
            yield return null;
            Assert.AreEqual("lower-hit", Hit(stage, new Vector2(50, 50), lower, upper)?.Name);
            lower.Destroy();
            upper.Destroy();
        }
    }
}
