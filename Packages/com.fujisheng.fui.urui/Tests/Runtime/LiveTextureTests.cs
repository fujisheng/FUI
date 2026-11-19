using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class LiveTextureTests
    {
        [UnityTest]
        public IEnumerator LegacyRecordingSamplesUpdatedGpuFrame() => Verify();

        static IEnumerator Verify()
        {
            var root = new GameObject("Live texture test");
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var output = new RenderTexture(Screen.width, Screen.height, 0);
            output.Create();
            var expectedOutput = new RenderTexture(Screen.width, Screen.height, 0);
            expectedOutput.Create();
            var referenceMaterial = new Material(Shader.Find("Hidden/URUI/Tests/StraightTextureReference"));
            camera.targetTexture = output;
            var source = new RenderTexture(4, 4, 0);
            source.Create();
            var sample = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            var quadrants = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            quadrants.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.yellow });
            quadrants.Apply();
            try
            {
                var calls = 0;
                var document = FuiHtmlFixtureAssets.Create(root);
                document.ImageResolver = _ =>
                {
                    calls++;
                    return new ImageLease(source);
                };
                document.TextResources = new[] { new TextResource("URUI/Tests/LiveTexture.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/LiveTexture.styles")) };
                FuiHtmlFixtureAssets.Load(document, "URUI/Tests/LiveTexture");
                foreach (var color in new[] { Color.red, Color.green, new Color(.8f, .5f, .2f, .5f), Color.clear })
                {
                    var previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = source;
                        GL.Clear(false, true, color);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                    if (color == Color.clear)
                    {
                        Graphics.Blit(quadrants, source);
                    }
                    previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = expectedOutput;
                        GL.Clear(false, true, Color.black);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                    Graphics.Blit(source, expectedOutput, referenceMaterial);
                    yield return null;
                    yield return null;
                    camera.Render();
                    foreach (var location in new[] { new Vector2(.25f, .25f), new Vector2(.75f, .25f), new Vector2(.25f, .75f), new Vector2(.75f, .75f) })
                    {
                        var point = new Rect((int)(output.width * location.x), (int)(output.height * location.y), 1, 1);
                        previous = RenderTexture.active;
                        try
                        {
                            RenderTexture.active = output;
                            sample.ReadPixels(point, 0, 0);
                            sample.Apply();
                        }
                        finally
                        {
                            RenderTexture.active = previous;
                        }
                        var pixel = sample.GetPixel(0, 0);
                        previous = RenderTexture.active;
                        try
                        {
                            RenderTexture.active = expectedOutput;
                            sample.ReadPixels(point, 0, 0);
                            sample.Apply();
                        }
                        finally
                        {
                            RenderTexture.active = previous;
                        }
                        var expected = sample.GetPixel(0, 0);
                        Assert.That(pixel.r, Is.EqualTo(expected.r).Within(.03f), "red channel at " + location);
                        Assert.That(pixel.g, Is.EqualTo(expected.g).Within(.03f), "green channel at " + location);
                        Assert.That(pixel.b, Is.EqualTo(expected.b).Within(.03f), "blue channel at " + location);
                    }
                }
                Assert.AreEqual(1, calls, "更新原 GPU 纹理不应重解析图片或重建页面。");
                document.Unload();
                Assert.IsTrue(source.IsCreated(), "借用输出由调用者释放。");
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
                source.Release();
                output.Release();
                expectedOutput.Release();
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(sample);
                Object.DestroyImmediate(quadrants);
                Object.DestroyImmediate(expectedOutput);
                Object.DestroyImmediate(referenceMaterial);
            }
        }
    }
}
