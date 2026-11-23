using System.Collections;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class AuthoredSlotsTests
    {
        sealed class Model : ObservableObject
        {
            public string Caption;
        }
        sealed class ProbePresenter : Presenter<Model>
        {
        }
        sealed class OnDispose : System.IDisposable
        {
            readonly System.Action action;
            public OnDispose(System.Action action)
            {
                this.action = action;
            }
            public void Dispose() => action();
        }
        sealed class Context : BindingContext<Model>
        {
            public Context(IView view, Model model) : base(view, model) { }
            protected override void OnSynchronize() => View.GetElement<ITextElement>("Label").Text.Value = ViewModel.Caption;
        }
        [UnityTest]
        public IEnumerator OrdinarySlotsUseExplicitBindingsAndSurviveReplacementClearAndDisposal()
        {
            var host = new GameObject("Ordinary authored slots");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            SlotListElement list = null;
            try
            {
                GeneratedBindingRegistry.Register<Model>((view, model) => new Context(view, model), () => new ProbePresenter());
                var document = FuiHtmlFixtureAssets.Create(host);
                document.TextResources = new[] { new TextResource("URUI/Tests/AuthoredSlots.styles.css", UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredSlots.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/AuthoredSlots");
                var first = document.Q("first");
                var second = document.Q("second");
                var factoryCalls = 0;
                var authoredChildCount = document.Q("slots").ChildCount;
                list = new SlotListElement(document.Q("slots"), "Slots", new[] { first, second }, (scope, index) =>
                {
                    factoryCalls++;
                    return new View("Slot" + index, scope, new[] { ElementBinding.For("Label", ".caption", (element, name) => new TextElement(element, name)) });
                });
                Assert.AreEqual(2, list.Capacity, "装饰节点不应占用显式容量，普通节点不需要 data-fui-slot。");
                list.ItemsSource.Value = new ObservableList<Model> { new() { Caption = "first" }, new() { Caption = "second" } };
                yield return null;
                yield return null;
                Assert.AreEqual(2, factoryCalls);
                Assert.AreEqual("first", first.Q(className: "caption").Text);
                Assert.AreEqual("second", second.Q(className: "caption").Text);
                Assert.That(first.Layout.x, Is.EqualTo(20).Within(.1));
                Assert.That(second.Layout.width, Is.EqualTo(120).Within(.1));
                list.ItemsSource.Value = new ObservableList<Model> { new() { Caption = "replacement" } };
                Assert.AreEqual("replacement", first.Q(className: "caption").Text);
                Assert.AreEqual("none", second.ResolvedStyle.Get("display"));
                list.ItemsSource.Value = null;
                Assert.IsTrue(first.IsValid);
                Assert.IsTrue(second.IsValid);
                list.ItemsSource.Value = new ObservableList<Model> { new() { Caption = "restored" }, new() { Caption = "again" } };
                Assert.AreEqual("restored", first.Q(className: "caption").Text);
                list.Dispose();
                list = null;
                Assert.IsTrue(first.IsValid);
                Assert.IsTrue(second.IsValid);
                Assert.AreEqual(authoredChildCount, document.Q("slots").ChildCount);
            }
            finally
            {
                list?.Dispose();
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(camera);
            }
        }
        [UnityTest]
        public IEnumerator DisposalInsideExplicitFactoryRetiresItsViewAndPreservesAuthorNodes()
        {
            var host = new GameObject("Disposed authored slots");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            SlotListElement list = null;
            try
            {
                var document = FuiHtmlFixtureAssets.Create(host);
                document.TextResources = new[] { new TextResource("URUI/Tests/AuthoredSlots.styles.css", UnityEngine.Resources.Load<TextAsset>("URUI/Tests/AuthoredSlots.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/AuthoredSlots");
                var first = document.Q("first");
                var hostNode = document.Q("slots");
                var retired = 0;
                var authoredChildCount = hostNode.ChildCount;
                System.Func<ViewScope, int, View> factory = (scope, index) =>
                {
                    var view = new View("Borrowed", scope, System.Array.Empty<ElementBinding>());
                    view.Own(new OnDispose(() => retired++));
                    list.Dispose();
                    return view;
                };
                Assert.Throws<System.ArgumentException>(() => new SlotListElement(hostNode, "Invalid", new[] { first, first }, factory));
                Assert.That(first.Style.Get("display"), Is.Null, "无效构造不能隐藏已有作者节点。");
                list = new SlotListElement(hostNode, "Slots", new[] { first }, factory);
                list.ItemsSource.Value = new ObservableList<Model> { new() { Caption = "must not bind" } };
                Assert.AreEqual(1, retired);
                Assert.IsTrue(first.IsValid);
                Assert.AreEqual(authoredChildCount, hostNode.ChildCount);
                Assert.DoesNotThrow(() => list.Dispose());
                yield return null;
            }
            finally
            {
                list?.Dispose();
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(camera);
            }
        }
    }
}
