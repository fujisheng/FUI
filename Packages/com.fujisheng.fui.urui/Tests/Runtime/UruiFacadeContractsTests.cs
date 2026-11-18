using System;
using System.Collections;
using FUI.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiFacadeContractsTests
    {
        GameObject root,camera;
        Document document;
        [UnitySetUp] public IEnumerator SetUp()
        {
            camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
            root=new GameObject("Explicit facet contracts");document=FuiHtmlFixtureAssets.Create(root);
            document.TextResources=new[]{new TextResource("FUI.URUI.Tests/Facets.styles.css",global::UnityEngine.Resources.Load<TextAsset>("FUI.URUI.Tests/Facets.styles"))};
            try { FuiHtmlFixtureAssets.Load(document, "FUI.URUI.Tests/Facets"); }
            catch { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camera); throw; }
            document.Surface.Input.UseUnityInput=false;
            yield return null;yield return null;
        }
        [TearDown] public void TearDown()
        {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camera);}

        [Test] public void PlainFacetDoesNotAdvertiseButtonOrClickCapabilities()
        {
            using var plain=new UruiElement(document.Q("plain"),"plain");
            Assert.IsFalse(plain is IButtonElement);Assert.IsFalse(plain is IClickElement);
        }
        [Test] public void DisplayOnlyViewDoesNotInferBindingsFromMarkup()
        {
            var host=document.Q("child");host.SetAttribute("data-fui-name","legacy-name");
            var view=new UruiView("display-only",new UruiScope(host));
            try{view.Initialize();Assert.IsEmpty(view.Elements,"绑定能力必须由明确工厂声明，不能猜测标签或旧框架属性。");}
            finally{view.Destroy();}
        }
        [Test] public void VisibilityIsSharedAndSurvivesPartialFacetDisposal()
        {
            var host=document.Q("plain");host.Style.Display=global::URUI.Elements.DisplayStyle.Flex;
            using var first=new UruiElement(host,"first");
            using var second=new UruiElement(host,"second");
            first.Visible.Value=false;Assert.IsFalse(second.Visible.Value);
            first.Dispose();second.Visible.Value=true;
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.Flex,host.Style.Display);
            using var third=new UruiElement(host,"third");
            third.Visible.Value=false;Assert.IsFalse(second.Visible.Value);
            second.Dispose();third.Visible.Value=true;
            Assert.AreEqual(global::URUI.Elements.DisplayStyle.Flex,host.Style.Display);
        }
        [Test] public void NestedButtonsInvokeOnlyTheActualButtonCommand()
        {
            var outer=document.Q<global::URUI.Controls.Button>("outer");var inner=document.Q<global::URUI.Controls.Button>("inner");
            // HTML 作者语法禁止嵌套按钮；运行时 DOM 仍可构造此结构来验证事件隔离。
            outer.Add(inner); document.SynchronizeLayout();
            using var parent=new UruiButtonElement(outer,"outer");using var child=new UruiButtonElement(inner,"inner");
            var parents=0;var children=0;parent.Click.AddListener(()=>parents++);child.Click.AddListener(()=>children++);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down,inner.WorldBound.center,pressedButtons:1);
            document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up,inner.WorldBound.center);
            Assert.AreEqual(1,children);Assert.AreEqual(0,parents,"不能把子按钮点击重复发布为父按钮命令。");
        }
        [UnityTest] public IEnumerator NativeValueBindingCanInitializeAnotherView()=>OpenFromValue(false);
        [UnityTest] public IEnumerator NativeValueCommandCanInitializeAnotherView()=>OpenFromValue(true);
        IEnumerator OpenFromValue(bool command)
        {
            var toggle=document.Q<global::URUI.Controls.Toggle>("toggle");
            using var adapter=new UruiToggleElement(toggle,"toggle");
            var view=new UruiView("child",new UruiScope(document.Q("child")),Array.Empty<UruiElementBinding>());
            Exception observed=null;var calls=0;
            void Open(){calls++;try{view.Initialize();}catch(Exception error){observed=error;}}
            if(command){adapter.ValueChanged.AddListener(_=>Open());}else{adapter.Value.OnValueChanged+=(_,value)=>Open();}
            try
            {
                document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Down,toggle.WorldBound.center,pressedButtons:1);
                document.Surface.Input.SendPointer(global::URUI.Events.PointerPhase.Up,toggle.WorldBound.center);
                Assert.AreEqual(1,calls);Assert.IsTrue(adapter.Value.Value);
                Assert.IsNull(observed,"值事件必须退出原生回调后发布，使共享逻辑能同步打开页面。");
                yield return null;
            }
            finally{view.Destroy();}
        }
    }
}
