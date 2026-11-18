using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Rendering;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiPaintingTests
    {
        [UnityTest]
        public IEnumerator PainterPreservesDomOrderClipTransformOpacityAndGpuOwnership()
        {
            var root = new GameObject("Paint test");
            var cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var output = new RenderTexture(Screen.width, Screen.height, 0); output.Create(); camera.targetTexture = output;
            var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var mesh = new Mesh { vertices = new[] { Vector3.zero, new Vector3(70, 0), new Vector3(70, 60), new Vector3(0, 60) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 } };
            var material = new Material(Shader.Find("Hidden/URUI/Tests/Painting"));
            IDisposable registration = null;
            var released = 0; var draws = 0; ElementPaintContext previousContext = default;
            try
            {
                // 让前一个测试延迟销毁的 MainCamera 退出，再由本例宿主选择相机。
                yield return null;
                var document=FuiHtmlFixtureAssets.Create(root);
                document.TextResources = new[] { new TextResource("URUI/Tests/Painting.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/Painting.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/Painting");
                var node = document.Q(className: "paint-node");
                registration = document.AttachPainter(node, "test-paint", context =>
                {
                    draws++; previousContext = context;
                    Assert.AreSame(node, context.Element);
                    Assert.That(context.Size.x, Is.EqualTo(70).Within(.01f));
                    var properties=new MaterialPropertyBlock();properties.SetFloat("_UruiPaintOpacity",-9);
                    context.DrawMesh(mesh, material, Matrix4x4.identity,properties);
                    Assert.AreEqual(1,properties.GetFloat("_UruiPaintOpacity"),"合成组统一处理透明度，绘制器不能重复相乘。");
                }, () => { released++; UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(material); });
                yield return null; yield return null;
                Assert.Greater(draws, 0);
                Assert.Throws<InvalidOperationException>(() => previousContext.DrawMesh(mesh, material, Matrix4x4.identity));
                camera.Render();
                Color Sample(int x, int y)
                {
                    var prior = RenderTexture.active;
                    try { RenderTexture.active = output; pixel.ReadPixels(new Rect(x, output.height - y - 1, 1, 1), 0, 0); pixel.Apply(); return pixel.GetPixel(0, 0); }
                    finally { RenderTexture.active = prior; }
                }
                var front = Sample(30, 40);
                Debug.Log($"paint pixels: front={front}, inside={Sample(45,50)}, outside={Sample(70,50)}; node opacity={node.ResolvedStyle.Get("opacity")}, front opacity={document.Q(className: "paint-front").ResolvedStyle.Get("opacity")}");
                Assert.Greater(front.b, .9f); Assert.Less(front.g, .1f);
                var painted = Sample(45, 50);
                if(QualitySettings.activeColorSpace==ColorSpace.Linear&&!output.sRGB)painted=painted.gamma;
                Assert.That(painted.g,Is.EqualTo(.5f).Within(.025f));Assert.That(painted.r,Is.EqualTo(.5f).Within(.025f));
                var outside = Sample(70, 50); Assert.Greater(outside.r, .9f); Assert.Less(outside.g, .1f);
                node.Style.Set("opacity",".5");yield return null;yield return null;camera.Render();
                var faded=Sample(45,50);
                if(QualitySettings.activeColorSpace==ColorSpace.Linear&&!output.sRGB)faded=faded.gamma;
                Assert.That(faded.r,Is.EqualTo(.75f).Within(.025f));Assert.That(faded.g,Is.EqualTo(.25f).Within(.025f));
                node.Destroy(); registration.Dispose(); registration = null;
                for (var index = 0; index < 20 && released == 0; index++) { yield return null; }
                Assert.AreEqual(1, released, "撤销绘制并完成 GPU fence 后只释放一次。");
                var count = draws; yield return null; Assert.AreEqual(count, draws);
                document.Unload(); Assert.AreEqual(1, released);
            }
            finally
            {
                if (root) { UnityEngine.Object.DestroyImmediate(root); }
                registration?.Dispose(); camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject); output.Release();
                UnityEngine.Object.DestroyImmediate(output); UnityEngine.Object.DestroyImmediate(pixel);
                if (released == 0) { if (mesh) UnityEngine.Object.DestroyImmediate(mesh); if (material) UnityEngine.Object.DestroyImmediate(material); }
            }
        }
    }
}
