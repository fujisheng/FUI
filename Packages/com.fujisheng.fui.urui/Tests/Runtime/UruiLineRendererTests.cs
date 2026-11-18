using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Elements;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiLineRendererTests
    {
        [UnityTest]
        public IEnumerator LineDataUsesLocalCoordinatesWithoutCreatingDomAndTracksLayout()
        {
            var callbackType=typeof(Action<IReadOnlyList<LineSegment>>);
            var constructor=typeof(UruiConnectionLinesElement).GetConstructor(new[]{typeof(Element),typeof(string),callbackType});
            Assert.NotNull(constructor,"连线适配器需要接收独立绘制器，不能在代码中构造线条DOM与样式。");
            var root=new GameObject("Line data test");var camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
            UruiConnectionLinesElement adapter=null;
            try
            {
                yield return null;
                var document=FuiHtmlFixtureAssets.Create(root);
                document.TextResources=new[]{new TextResource("URUI/Tests/LineRenderer.styles.css",global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/LineRenderer.styles"))};
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/LineRenderer");
                IReadOnlyList<LineSegment> rendered=null;
                Action<IReadOnlyList<LineSegment>> render=value=>rendered=value;
                var node=document.Q("lines");adapter=(UruiConnectionLinesElement)constructor.Invoke(new object[]{node,"Lines",render});
                adapter.LineSegments.Value=new[]{new LineSegment(new SpatialValue(40,Screen.height-70,0),new SpatialValue(80,Screen.height-70,0),new Rgba(.2f,.4f,.6f,.8f))};
                for(var i=0;i<5;i++){yield return null;}
                Assert.That(rendered.Count,Is.EqualTo(1));
                Assert.That(rendered[0].from.x,Is.EqualTo(5).Within(.01));Assert.That(rendered[0].from.y,Is.EqualTo(10).Within(.01));
                Assert.That(rendered[0].to.x,Is.EqualTo(25).Within(.01));Assert.That(rendered[0].color.a,Is.EqualTo(.8f));
                Assert.AreEqual(0,node.ChildCount);
                document.Q("parent").AddToClassList("shifted");
                for(var i=0;i<4;i++){yield return null;}
                Assert.That(rendered[0].from.x,Is.EqualTo(-5).Within(.01));Assert.That(rendered[0].to.x,Is.EqualTo(15).Within(.01));
                adapter.LineSegments.Value=null;Assert.That(rendered,Is.Empty);Assert.AreEqual(0,node.ChildCount);
                adapter.Dispose();adapter=null;document.Unload();
            }
            finally{adapter?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camera);}
        }
    }
}
