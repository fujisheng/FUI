#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Controls;
using URUI.Documents;
using URUI.Elements;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class TextFitTests
    {
        static readonly string LongText = "A longer message must stay inside its authored rectangle.";
        static float Font(Element text) => float.Parse(text.ResolvedStyle.Get("font-size").Replace("px", ""), CultureInfo.InvariantCulture);
        static Document Load(GameObject owner)
        {
            var document = FuiHtmlFixtureAssets.Create(owner);
            document.TextResources = new[] { new TextResource("URUI/Tests/TextFit.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/TextFit.styles")), new TextResource("URUI/Tests/Fonts/LatoLatin-Regular.ttf.bytes", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/Fonts/LatoLatin-Regular.ttf")) };
            FuiHtmlFixtureAssets.Load(document, "URUI/Tests/TextFit");
            return document;
        }
        static void Cleanup(GameObject owner, GameObject camera)
        {
            var document = owner ? owner.GetComponent<Document>() : null;
            if (document && document.IsLoaded)
            {
                document.Unload();
            }
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(camera);
        }
        [UnityTest]
        public IEnumerator AuthoredTextShrinksRestoresAndReactsToNarrowBounds()
        {
            var type = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.TextFit");
            Assert.NotNull(type, "自动缩字必须由通用控件承担，不能在项目中按字数猜字号。");
            var owner = new GameObject("Text fitting");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            IDisposable fit = null;
            try
            {
                var document = Load(owner);
                var host = document.Q("host");
                var text = document.Q("text");
                _ = new TextLayout(host, text, TextAnchor.MiddleCenter);
                fit = (IDisposable)Activator.CreateInstance(type, host, text);
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(Font(text), Is.LessThan(40));
                Assert.That(text.Layout.height, Is.LessThanOrEqualTo(host.ContentRect.height + .25f));
                var count = host.ChildCount;
                var wideFont = Font(text);
                host.AddToClassList("narrow");
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(Font(text), Is.LessThan(wideFont));
                Assert.That(text.Layout.height, Is.LessThanOrEqualTo(host.ContentRect.height + .25f));
                host.RemoveFromClassList("narrow");
                text.Text = "Short";
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(Font(text), Is.EqualTo(40).Within(.01f));
                Assert.That(host.ChildCount, Is.EqualTo(count));
                fit.Dispose();
                fit = null;
                Assert.IsNull(text.Style.Get("font-size"));
                Assert.IsFalse(host.ClassListContains("text-fit-pending"));
                document.Unload();
            }
            finally
            {
                fit?.Dispose();
                Cleanup(owner, camera);
            }
        }
        [UnityTest]
        public IEnumerator TextAdapterRefreshesFitterBeforePooledLabelIsShownAndOwnsItsLifetime()
        {
            var type = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.TextFit");
            Assert.NotNull(type);
            var constructor = typeof(TextElement).GetConstructor(new[] { typeof(Element), typeof(string), typeof(TextLayout), type });
            Assert.NotNull(constructor, "文本绑定必须通知并持有可选的缩字控制器。");
            var owner = new GameObject("Bound fitting");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            TextElement adapter = null;
            IDisposable fit = null;
            try
            {
                var document = Load(owner);
                var host = document.Q("host");
                var text = document.Q("text");
                var layout = new TextLayout(host, text, TextAnchor.MiddleCenter);
                host.AddToClassList("text-fit-pending");
                fit = (IDisposable)Activator.CreateInstance(type, host, text);
                adapter = (TextElement)constructor.Invoke(new object[] { host, "message", layout, fit });
                adapter.Text.Value = "Short";
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.IsFalse(host.ClassListContains("text-fit-pending"));
                adapter.Text.Value = LongText;
                Assert.IsTrue(host.ClassListContains("text-fit-pending"), "池化再次显示前就应重新测量，不能先闪出溢出的文字。");
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(text.Layout.height, Is.LessThanOrEqualTo(host.ContentRect.height + .25f));
                adapter.Dispose();
                adapter = null;
                fit = null;
                Assert.IsNull(text.Style.Get("font-size"));
                Assert.IsTrue(host.ClassListContains("text-fit-pending"), "释放恢复作者原有的等待状态。");
                document.Unload();
            }
            finally
            {
                adapter?.Dispose();
                fit?.Dispose();
                Cleanup(owner, camera);
            }
        }
        [UnityTest]
        public IEnumerator InheritedRichTextKeepsMeasuring()
        {
            var type = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.TextFit");
            Assert.NotNull(type);
            var owner = new GameObject("Rich fitting");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            TextElement adapter = null;
            IDisposable fit = null;
            try
            {
                var document = Load(owner);
                var host = document.Q("host");
                var text = document.Q("text");
                var layout = new TextLayout(host, text, TextAnchor.MiddleCenter);
                fit = (IDisposable)Activator.CreateInstance(type, host, text);
                adapter = (TextElement)Activator.CreateInstance(typeof(TextElement), host, "message", layout, fit);
                adapter.Text.Value = "<color=#ffffff>" + LongText + "</color>";
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(text.Layout.height, Is.LessThanOrEqualTo(host.ContentRect.height + .25f));
                Assert.IsFalse(host.ClassListContains("text-fit-pending"));
                Assert.DoesNotThrow(() => adapter.Text.Value = "Short");
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(Font(text), Is.EqualTo(40).Within(.01f));
                document.Unload();
            }
            finally
            {
                adapter?.Dispose();
                fit?.Dispose();
                Cleanup(owner, camera);
            }
        }
        [UnityTest]
        public IEnumerator ReparentedLabelKeepsItsFitter()
        {
            var type = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.TextFit");
            Assert.NotNull(type);
            var owner = new GameObject("Reparented fitting");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            TextElement adapter = null;
            IDisposable fit = null;
            try
            {
                var document = Load(owner);
                var host = document.Q("host");
                var text = document.Q("text");
                var layout = new TextLayout(host, text, TextAnchor.MiddleCenter);
                fit = (IDisposable)Activator.CreateInstance(type, host, text);
                adapter = (TextElement)Activator.CreateInstance(typeof(TextElement), host, "message", layout, fit);
                adapter.Text.Value = "Short";
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                var parking = document.Q("parking");
                parking.Hierarchy.Add(host);
                Assert.IsTrue(host.IsValid);
                parking.Hierarchy.Clear();
                Assert.IsTrue(host.IsValid);
                Assert.IsFalse(host.IsAttached);
                parking.Hierarchy.Add(host);
                Assert.DoesNotThrow(() => adapter.Text.Value = LongText);
                for (var i = 0; i < 16; i++)
                {
                    yield return null;
                }
                Assert.That(text.Layout.height, Is.LessThanOrEqualTo(host.ContentRect.height + .25f));
                document.Unload();
            }
            finally
            {
                adapter?.Dispose();
                fit?.Dispose();
                Cleanup(owner, camera);
            }
        }
    }
}
#endif
