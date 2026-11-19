using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Controls;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class RowContentSizeTests
    {
        [UnityTest]
        public IEnumerator AuthoredRowMeasuresOuterEdgesAndShrinksWithoutChangingChildren()
        {
            var type = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.RowContentSize");
            Assert.NotNull(type, "横向内容应复用通用测量器，不能把英雄行的尺寸规则复制到其他页面。");
            var host = new GameObject("Authored row size");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            IDisposable size = null;
            try
            {
                var document = host.AddComponent<Document>();
                document.Surface = host.AddComponent<global::URUI.Hosting.Surface>();
                document.TextResources = new[] { new TextResource("URUI/Tests/RowContent.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/RowContent.styles")) };
                document.Load(global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/RowContent"), "URUI/Tests/RowContent.html");
                var row = document.Q("row");
                size = (IDisposable)Activator.CreateInstance(type, row);
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                }
                Assert.That(row.Layout.width, Is.EqualTo(395).Within(.02f));
                Assert.That(row.ChildCount, Is.EqualTo(3));
                row.AddToClassList("hide-last");
                for (var frame = 0; frame < 4; frame++)
                {
                    yield return null;
                }
                Assert.That(row.Layout.width, Is.EqualTo(280).Within(.02f));
                row.AddToClassList("empty");
                for (var frame = 0; frame < 4; frame++)
                {
                    yield return null;
                }
                Assert.That(row.Layout.width, Is.EqualTo(60).Within(.02f));
                size.Dispose();
                size = null;
                Assert.IsNull(row.Style.Get("width"), "释放后恢复作者尺寸，不遗留运行时覆盖。");
                document.Unload();
            }
            finally
            {
                size?.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(camera);
            }
        }
    }
}
