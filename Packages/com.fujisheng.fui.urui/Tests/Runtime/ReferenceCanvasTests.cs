using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Controls;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class ReferenceCanvasTests
    {
        [UnityTest]
        public IEnumerator ExplicitScaleLimitKeepsMinimumLogicalWidthAndUpdatesWithoutResize()
        {
            var host = new GameObject("Limited reference canvas");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            try
            {
                var document = FuiHtmlFixtureAssets.Create(host);
                document.TextResources = new[] { new TextResource("URUI/Tests/ReferenceCanvas.styles.css", UnityEngine.Resources.Load<TextAsset>("URUI/Tests/ReferenceCanvas.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/ReferenceCanvas");
                using var canvas = new ReferenceCanvas(document.Q("canvas"), new Vector2(2532, 1170), .5f, document.Q("safe"));
                var display = new Rect(0, 0, 1600, 1200);
                canvas.Apply(display, display, 1600f / 2532);
                yield return null;
                yield return null;
                Assert.That(canvas.LogicalSize.x, Is.EqualTo(2532).Within(.01f));
                canvas.Apply(display, display, .5f);
                yield return null;
                yield return null;
                Assert.That(canvas.Scale, Is.EqualTo(.5f).Within(.0001f), "只改变上限时也必须更新变换。");
                var button = document.Q("corner");
                var center = button.LocalToWorld(button.Layout.size * .5f);
                Assert.That(Vector2.Distance(button.WorldToLocal(center), button.Layout.size * .5f), Is.LessThan(.01f));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.Apply(display, display, 0));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.Apply(display, display, float.NaN));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(camera);
            }
        }
        [UnityTest]
        public IEnumerator LandscapeRatiosUseUniformReferenceScaleAndSameNativePicking()
        {
            var root = new GameObject("Reference canvas test");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            try
            {
                var document = FuiHtmlFixtureAssets.Create(root);
                document.TextResources = new[] { new TextResource("URUI/Tests/ReferenceCanvas.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/ReferenceCanvas.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/ReferenceCanvas");
                using var canvas = new ReferenceCanvas(document.Q("canvas"), new Vector2(2532, 1170), .5f, document.Q("safe"));
                foreach (var dimensions in new[] { new Vector2(1600, 1200), new Vector2(1920, 1200), new Vector2(1920, 1080), new Vector2(2532, 1170), new Vector2(2400, 1080), new Vector2(2520, 1080) })
                {
                    var display = new Rect(0, 0, dimensions.x, dimensions.y);
                    // 在当前 Editor context 内变换和命中，避免把大屏虚拟尺寸误当真实 Screen。
                    canvas.Apply(display, new Rect(60, 15, dimensions.x - 90, dimensions.y - 25));
                    yield return null;
                    yield return null;
                    var button = document.Q("corner");
                    var origin = button.LocalToWorld(Vector2.zero);
                    var extent = button.LocalToWorld(button.Layout.size) - origin;
                    Assert.That(extent.x / 100, Is.EqualTo(canvas.Scale).Within(.001f));
                    Assert.That(extent.y / 60, Is.EqualTo(canvas.Scale).Within(.001f));
                    Assert.That(origin.x + extent.x, Is.EqualTo(dimensions.x - 30 - 20 * canvas.Scale).Within(.1f), "通用默认策略应保留设备实际左右安全区。");
                    Assert.That(origin.y + extent.y, Is.EqualTo(dimensions.y - 15 - 30 * canvas.Scale).Within(.1f));
                    // Pick 只接收真实文档视口内的点；坐标转换往返也覆盖视口外的布局。
                    var center = button.LocalToWorld(button.Layout.size * .5f);
                    Assert.That(Vector2.Distance(button.WorldToLocal(center), button.Layout.size * .5f), Is.LessThan(.01f));
                    if (center.x < Screen.width && center.y < Screen.height)
                    {
                        Assert.AreSame(button, document.Pick(center));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }

        [UnityTest]
        public IEnumerator SymmetricHorizontalSafeAreaIsAnExplicitPolicy()
        {
            var root = new GameObject("Symmetric safe area test");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            try
            {
                var document = FuiHtmlFixtureAssets.Create(root);
                document.TextResources = new[] { new TextResource("URUI/Tests/ReferenceCanvas.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/ReferenceCanvas.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/ReferenceCanvas");
                var policy = typeof(ReferenceCanvas).Assembly.GetType("URUI.Controls.SafeAreaPolicy");
                Assert.IsNotNull(policy, "项目对称留白应显式选择，不应固化为底层默认行为。");
                using var canvas = (ReferenceCanvas)System.Activator.CreateInstance(typeof(ReferenceCanvas), document.Q("canvas"), new Vector2(1000, 500), .5f,
                    document.Q("safe"), System.Enum.Parse(policy, "SymmetricHorizontal"));
                canvas.Apply(new Rect(0, 0, 1000, 500), new Rect(80, 10, 900, 480));
                yield return null;
                yield return null;
                Assert.That(document.Q("corner").WorldBound.xMax, Is.EqualTo(900).Within(.1f));
                Assert.That(document.Q("corner").WorldBound.yMax, Is.EqualTo(460).Within(.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }
    }
}
