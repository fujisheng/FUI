using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Hosting;
using URUI.Resources;
using FUI.Rendering;
using FUI.Navigation;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class ResourceTemplateTests
    {
        GameObject root;
        GameObject cameraObject;
        static TextAsset Asset(string key) => global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/" + key);

        static string BoundPage() => "<!doctype html><html><head><style>body{margin:0;font-family:Arial;}.accept-button{width:80px;height:30px;}</style></head><body>" + Asset("BoundFragment").text + "</body></html>";

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            root = new GameObject("External resource template test");
            root.AddComponent<Surface>().Input.UseUnityInput = false;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        [UnityTest]
        public IEnumerator ComposedValueAdaptersKeepSilentBindingAndUserEvents()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/ValueVisuals.styles.css", Asset("ValueVisuals.styles")) };
            document.Load(Asset("ValueVisuals"), "URUI/Tests/ValueVisuals.html");
            var slider = document.Q<global::URUI.Controls.Slider>("slider");
            var toggle = document.Q<global::URUI.Controls.Toggle>("toggle");
            using var sliderAdapter = new SliderElement(slider, "volume", new global::URUI.Controls.SliderVisual(slider, document.Q("fill"), document.Q("thumb")));
            using var toggleAdapter = new ToggleElement(toggle, "enabled", new global::URUI.Controls.ToggleVisual(toggle, document.Q("toggle-host")));
            var changes = 0;
            sliderAdapter.ValueChanged.AddListener(_ => changes++);
            toggleAdapter.ValueChanged.AddListener(_ => changes++);
            sliderAdapter.Value.Value = .25f;
            toggleAdapter.IsOn.Value = false;
            yield return null;
            yield return null;
            Assert.AreEqual(0, changes, "绑定不能触发保存命令。");
            Assert.That(document.Q("fill").Layout.width, Is.EqualTo(50).Within(.1));
            Assert.IsFalse(document.Q("toggle-host").ClassListContains("checked"));
            document.Surface.Input.UseUnityInput = false;
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(170, 40), pressedButtons: 1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(170, 40));
            Assert.AreEqual(1, changes);
            Assert.That(sliderAdapter.Value.Value, Is.EqualTo(.75f).Within(.001));
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(70, 120), pressedButtons: 1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(70, 120));
            Assert.AreEqual(2, changes);
            Assert.IsTrue(toggleAdapter.IsOn.Value);
        }

        [UnityTest]
        public IEnumerator AuthoredSliderVisualTracksSilentWritesPointerAndResize()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/ValueVisuals.styles.css", Asset("ValueVisuals.styles")) };
            document.Load(Asset("ValueVisuals"), "URUI/Tests/ValueVisuals.html");
            var type = typeof(Document).Assembly.GetType("URUI.Controls.SliderVisual");
            Assert.IsNotNull(type, "滑块皮肤需要组合作者节点，保留静默绑定与原生输入。");
            var slider = document.Q<global::URUI.Controls.Slider>("slider");
            using var visual = (IDisposable)Activator.CreateInstance(type, slider, document.Q("fill"), document.Q("thumb"));
            var source = (global::URUI.Controls.INotifyValueChanged<float>)visual;
            var changes = 0;
            using var callback = slider.RegisterValueChangedCallback(_ => changes++);
            source.SetValueWithoutNotify(.25f);
            yield return null;
            yield return null;
            Assert.AreEqual(0, changes);
            Assert.That(document.Q("fill").Layout.width, Is.EqualTo(50).Within(.1));
            Assert.That(document.Q("thumb").WorldBound.center.x, Is.EqualTo(70).Within(.1));
            document.Surface.Input.UseUnityInput = false;
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(170, 40), pressedButtons: 1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(170, 40));
            yield return null;
            yield return null;
            Assert.That(source.Value, Is.EqualTo(.75f).Within(.001));
            Assert.AreEqual(1, changes);
            document.Q("slider-host").Style.SetWidth(400);
            yield return null;
            yield return null;
            Assert.That(document.Q("fill").Layout.width, Is.EqualTo(300).Within(.1));
            Assert.That(document.Q("thumb").WorldBound.center.x, Is.EqualTo(320).Within(.1));
        }

        [UnityTest]
        public IEnumerator AuthoredToggleVisualTracksSilentWritesAndOneClick()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/ValueVisuals.styles.css", Asset("ValueVisuals.styles")) };
            document.Load(Asset("ValueVisuals"), "URUI/Tests/ValueVisuals.html");
            var type = typeof(Document).Assembly.GetType("URUI.Controls.ToggleVisual");
            Assert.IsNotNull(type, "开关外观应由作者样式响应状态，不构造控件标记。");
            var toggle = document.Q<global::URUI.Controls.Toggle>("toggle");
            using var visual = (IDisposable)Activator.CreateInstance(type, toggle, document.Q("toggle-host"));
            var source = (global::URUI.Controls.INotifyValueChanged<bool>)visual;
            var changes = 0;
            using var callback = toggle.RegisterValueChangedCallback(_ => changes++);
            source.SetValueWithoutNotify(false);
            yield return null;
            yield return null;
            Assert.AreEqual(0, changes);
            Assert.IsFalse(document.Q("toggle-host").ClassListContains("checked"));
            document.Surface.Input.UseUnityInput = false;
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, new Vector2(70, 120), pressedButtons: 1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, new Vector2(70, 120));
            yield return null;
            yield return null;
            Assert.IsTrue(source.Value);
            Assert.AreEqual(1, changes);
            Assert.That(document.Q(className: "knob").WorldBound.x, Is.EqualTo(80).Within(.1));
        }

        [UnityTest]
        public IEnumerator LinkedStylesAndClonedTemplateKeepAuthoredStructureAndCascade()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/TemplateFixture.styles.css", Asset("TemplateFixture.styles")) };
            document.Load(Asset("TemplateFixture"), "URUI/Tests/TemplateFixture.html");
            var prototype = document.Q("library").Q(className: "tile");
            var clone = prototype.Clone();
            Assert.IsFalse(clone.IsAttached);
            document.Q("target").Hierarchy.Add(clone);
            yield return null;
            yield return null;
            Assert.That(clone.Layout.width, Is.EqualTo(40).Within(.01f));
            Assert.That(clone.Layout.height, Is.EqualTo(30).Within(.01f));
            clone.Q(className: "caption").Text = "changed";
            Assert.AreEqual("original", prototype.Q(className: "caption").Text);
            document.AddStyleSheet(Asset("TemplateOverride.styles"), "URUI/Tests/TemplateOverride.styles.css");
            yield return null;
            Assert.That(clone.Layout.width, Is.EqualTo(70).Within(.01f));
            Assert.That(clone.Layout.height, Is.EqualTo(30).Within(.01f));
            var detached = prototype.Clone();
            document.Unload();
            Assert.IsFalse(clone.IsValid);
            Assert.IsFalse(detached.IsValid);
        }

        [Test]
        public void DuplicateResourcePathsFailBeforeCreatingRenderer()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            var resource = new TextResource("URUI/Tests/TemplateFixture.styles.css", Asset("TemplateFixture.styles"));
            document.TextResources = new[] { resource, resource };
            Assert.Throws<ArgumentException>(() => document.Load(Asset("TemplateFixture"), "URUI/Tests/TemplateFixture.html"));
            Assert.IsFalse(document.IsLoaded);
            Assert.IsNull(root.transform.Find("URUI Renderer"));
        }

        [UnityTest]
        public IEnumerator NativeClickCanOpenAuthoredPageAfterPropagationWithoutAnotherFrame()
        {
            var stage = root.AddComponent<Stage>();
            var scope = stage.OpenPage(BoundPage(), "URUI/Tests/bound.html", Layer.Panel);
            var view = new View("source", scope, new[] {
                ElementBinding.For("Accept", ".accept-button", (element, name) => new ButtonElement(element, name)) });
            view.Initialize();
            yield return null;
            yield return null;
            var native = scope.Root.Q(className: "accept-button");
            var order = new System.Collections.Generic.List<string>();
            using var capture = native.RegisterCallback<global::URUI.Events.ClickEvent>(_ => order.Add("capture"), global::URUI.Events.TrickleDown.TrickleDown);
            using var bubble = native.RegisterCallback<global::URUI.Events.ClickEvent>(_ => order.Add("bubble"));
            ViewScope opened = null;
            view.GetElement<IButtonElement>("Accept").Click.AddListener(() =>
            {
                order.Add("command");
                stage.Surface.RunAfterEvent(() =>
                {
                    opened = stage.OpenPage(BoundPage(), "URUI/Tests/bound-popup.html", Layer.Popup);
                    Assert.IsNotNull(opened.Root.Q(className: "accept-button"));
                });
            });
            var point = native.WorldBound.center;
            stage.Surface.Input.UseUnityInput = false;
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down, point, pressedButtons: 1);
            stage.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up, point);
            CollectionAssert.AreEqual(new[] { "capture", "bubble", "command" }, order);
            Assert.IsNotNull(opened, "同步导航应在 SendPointer 返回前完成，不等下一帧。");
            Assert.AreEqual(2, stage.PageCount);
            opened.Destroy();
            view.Destroy();
        }

        [UnityTest]
        public IEnumerator AuthoredTextLayoutAlignsWrappedContentAndFollowsResize()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/TextLayout.styles.css", Asset("TextLayout.styles")), new TextResource("/urui/behaviors.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/behaviors")) };
            document.Load(Asset("TextLayout"), "URUI/Tests/TextLayout.html");
            var host = document.Q("text-host");
            var content = document.Q("text-content");
            var type = typeof(Document).Assembly.GetType("URUI.Controls.TextLayout");
            Assert.IsNotNull(type, "多行文本需要可组合到普通作者节点的通用对齐控件。");
            var layout = Activator.CreateInstance(type, host, content, TextAnchor.UpperLeft);
            yield return null;
            yield return null;
            Assert.That(content.WorldBound.yMin, Is.EqualTo(host.WorldBound.yMin).Within(1));
            Assert.That(content.Layout.height, Is.InRange(30, 70));
            type.GetProperty("Alignment").SetValue(layout, TextAnchor.MiddleCenter);
            yield return null;
            yield return null;
            Assert.That(content.WorldBound.center.y, Is.EqualTo(host.WorldBound.center.y).Within(1));
            Assert.AreEqual("center", content.ResolvedStyle.Get("text-align"));
            type.GetProperty("Alignment").SetValue(layout, TextAnchor.LowerRight);
            host.Style.SetHeight(240);
            yield return null;
            yield return null;
            Assert.That(content.WorldBound.yMax, Is.EqualTo(host.WorldBound.yMax).Within(1));
            Assert.AreEqual("right", content.ResolvedStyle.Get("text-align"));
        }

        [UnityTest]
        public IEnumerator LongUnbrokenTextWrapsWithinAuthoredContentWidth()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/TextLayout.styles.css", Asset("TextLayout.styles")), new TextResource("/urui/behaviors.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/behaviors")) };
            document.Load(Asset("TextLayout"), "URUI/Tests/TextLayout.html");
            var host = document.Q("text-host");
            var content = document.Q("text-content");
            var layout = new global::URUI.Controls.TextLayout(host, content);
            host.Style.SetWidth(110);
            content.Text = "unbrokenwordswithoutspaces";
            yield return null;
            yield return null;
            Assert.That(content.Layout.height, Is.InRange(48, 180), "长文本应按作者宽度换行，不能以单行溢出。");
            Assert.That(content.WorldBound.xMax, Is.LessThanOrEqualTo(host.WorldBound.xMax + 1));
        }

        [UnityTest]
        public IEnumerator TextAdapterOwnsItsComposedHostAndRendersIntoContent()
        {
            var document = root.AddComponent<Document>();
            document.Surface = root.GetComponent<Surface>();
            document.TextResources = new[] { new TextResource("URUI/Tests/TextLayout.styles.css", Asset("TextLayout.styles")), new TextResource("/urui/behaviors.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/behaviors")) };
            document.Load(Asset("TextLayout"), "URUI/Tests/TextLayout.html");
            var host = document.Q("text-host");
            var content = document.Q("text-content");
            content.Text = string.Empty;
            var layout = new global::URUI.Controls.TextLayout(host, content);
            TextElement foreign = null;
            try
            {
                Assert.Throws<ArgumentException>(() => foreign = new TextElement(document.DocumentElement, "foreign", layout));
            }
            finally
            {
                foreign?.Dispose();
            }
            using var adapter = new TextElement(host, "message", layout);
            adapter.Text.Value = "message";
            adapter.TextAlignment.Value = TextAlignment.MiddleCenter;
            yield return null;
            yield return null;
            Assert.AreSame(host, adapter.Element);
            Assert.AreEqual("message", content.Text);
            Assert.That(content.WorldBound.center.y, Is.EqualTo(host.WorldBound.center.y).Within(1));
            Assert.AreEqual("center", content.ResolvedStyle.Get("text-align"));
        }

        [UnityTest]
        public IEnumerator ExplicitBindingsUsePlainAuthoredClassesAndComposedProgress()
        {
            var stage = root.AddComponent<Stage>();
            var scope = stage.OpenPage(BoundPage(), "URUI/Tests/bound.html", Layer.Panel);
            var view = new View("explicit", scope, new[]
            {
                ElementBinding.For("Title", ".caption", (element, name) => new TextElement(element, name)),
                ElementBinding.For("Accept", ".accept-button", (element, name) => new ButtonElement(element, name)),
                ElementBinding.For("Progress", ".progress", (element, name) => new ProgressElement(element, name,
                    new global::URUI.Controls.ProgressIndicator(element, element.Q(className: "fill"))))
            });
            view.Initialize();
            view.GetElement<ITextElement>("Title").Text.Value = "updated";
            Assert.AreEqual("updated", scope.Root.Q(className: "caption").Text);
            var progress = view.GetElement<IFloatValueElement>("Progress");
            progress.Value.Value = .4f;
            Assert.AreEqual("40%", scope.Root.Q(className: "fill").Style.Get("width"));
            progress.Value.Value = 2;
            Assert.AreEqual(1, progress.Value.Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Value.Value = float.NaN);
            Assert.IsNotNull(view.GetElement<IButtonElement>("Accept"));
            Assert.IsNull(view.GetElement<IElement>("caption"), "作者 class 不自动变成业务绑定。");
            view.Destroy();
            Assert.IsFalse(scope.Root.IsValid);
            yield return null;
        }
    }
}
