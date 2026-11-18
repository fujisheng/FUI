using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiImageResolverTests
    {
        sealed class SubmissionPipelineAsset : RenderPipelineAsset
        {
            internal Action<ScriptableRenderContext> draw;
            protected override RenderPipeline CreatePipeline() => new SubmissionPipeline(this);
        }
        sealed class SubmissionPipeline : RenderPipeline
        {
            readonly SubmissionPipelineAsset asset;
            internal SubmissionPipeline(SubmissionPipelineAsset asset) { this.asset = asset; }
            protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
            {
                asset.draw?.Invoke(context);
                // 管线负责帧末提交；文档只把绘制和 fence 排入同一上下文。
                context.Submit();
            }
        }
        GameObject root, cameraObject;
        Texture2D texture;
        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            root = new GameObject("按需图片测试");
            texture = new Texture2D(4, 2);
            texture.Apply(false, true);
        }
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        static void Configure(Document document, Func<string, ImageLease> resolver)
        {
            document.ImageResolver = resolver;
        }

        [UnityTest]
        public IEnumerator ResolvesOnceStaticWinsAndRemovalKeepsContextLeaseUntilUnload()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            var calls = 0; var released = 0;
            doc.Images = new[] { new ImageAsset("static.png", texture) };
            Configure(doc, source =>
            {
                Assert.AreEqual("dynamic.png", source);
                calls++;
                return new ImageLease(texture, () => released++);
            });
            doc.LoadHtml("<!doctype html><html><head></head><body><img id='a' src='dynamic.png' style='width:20px;height:10px;'/><img id='b' src='dynamic.png' style='width:20px;height:10px;'/><img src='static.png'/></body></html>", "image-resolver.html");
            yield return null; yield return null;
            Assert.AreEqual(1, calls); Assert.AreEqual(0, released);
            Assert.DoesNotThrow(() => Configure(doc, null));
            Assert.IsTrue(doc.TryLoadImage("dynamic.png", out _));
            Assert.AreEqual(1, calls, "修改下次加载的配置不能替换当前资源代际。");
            doc.Q("a").RemoveFromHierarchy(); doc.Q("b").RemoveFromHierarchy();
            yield return null;
            Assert.AreEqual(0, released, "删除节点不代表 Html 文件缓存已释放");
            doc.Unload();
            yield return null; yield return null;
            Assert.AreEqual(1, released);
            Assert.IsTrue(texture);
        }

        [UnityTest]
        public IEnumerator ResolverQueuesDocumentLifecycleUntilNativeCallReturns()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            var requested = false;
            Configure(doc, source =>
            {
                doc.Unload(); requested = true;
                return new ImageLease(texture);
            });
            doc.LoadHtml("<!doctype html><html><head></head><body><img src='test.png' style='width:20px;height:10px;'/></body></html>", "image-resolver.html");
            yield return null; yield return null;
            Assert.IsTrue(requested); Assert.IsFalse(doc.IsLoaded);
        }

        [UnityTest]
        public IEnumerator OwnedImagesCannotEscapeThroughUntrackedCommandBuffer()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            Configure(doc, source => new ImageLease(texture, () => { }));
            doc.LoadHtml("<!doctype html><html><head></head><body><img src='owned.png' style='width:20px;height:10px;'/></body></html>", "image-resolver.html");
            yield return null; yield return null;
            Assert.Throws<InvalidOperationException>(() => { var ignored = doc.RenderCommandBuffer; },
                "可释放图片不能借给无法追踪 Submit 时机的外部命令缓冲调用者");
            doc.Unload();
        }

        [UnityTest]
        public IEnumerator ImmediateOwnerDestructionDuringResolveDefersNativeRelease()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            var released = 0;
            Configure(doc, source =>
            {
                UnityEngine.Object.DestroyImmediate(root);
                return new ImageLease(texture, () => released++);
            });
            var cancelled = false;
            try { doc.LoadHtml("<!doctype html><html><head></head><body><img src='close.png' style='width:20px;height:10px;'/></body></html>", "image-resolver.html"); }
            catch (InvalidOperationException) { cancelled = true; }
            yield return null; yield return null;
            Assert.IsTrue(cancelled || !root);
            Assert.AreEqual(1, released);
            root = new GameObject("销毁后重新加载");
            var next=FuiHtmlFixtureAssets.Create(root);
            next.LoadHtml("<!doctype html><html><head></head><body>next</body></html>", "image-resolver.html");
            Assert.IsTrue(next.IsLoaded);
            next.Unload();
        }

        [UnityTest]
        public IEnumerator SrpSubmissionKeepsOwnedImagesUntilSubmittedFenceRetires() => SrpRetirement(false);

        [UnityTest]
        public IEnumerator SrpSubmissionRetiresOnlyTheClosedDocumentWhileAnotherRemainsLoaded() => SrpRetirement(true);

        IEnumerator SrpRetirement(bool secondPage)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsGraphicsFence)
                Assert.Ignore("此项需要真实 GPU 与 fence；无图形批次不伪造提交验收。");
            var previous = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            var asset = ScriptableObject.CreateInstance<SubmissionPipelineAsset>();
            var target = new RenderTexture(32, 32, 0);
            var camera = cameraObject.GetComponent<Camera>();
            camera.targetTexture = target;
            var doc=FuiHtmlFixtureAssets.Create(root);
            var released = 0; var submitted = false; var retainedAfterUnload = false;
            Document other = null; Texture2D otherTexture = null; var otherReleased = 0;
            try
            {
                Configure(doc, source => new ImageLease(texture, () => released++));
                doc.LoadHtml("<!doctype html><html><head></head><body><img src='owned.png' style='width:20px;height:10px;'/></body></html>", "image-resolver.html");
                if (secondPage)
                {
                    var otherRoot = new GameObject("Other SRP document"); otherRoot.transform.SetParent(root.transform, false);
                    otherTexture = new Texture2D(2, 2); otherTexture.Apply(false, true);
                    other = otherRoot.AddComponent<Document>(); other.Surface = doc.Surface;
                    other.ImageResolver = _ => new ImageLease(otherTexture, () => otherReleased++);
                    other.LoadHtml("<!doctype html><html><head></head><body><img src='other.png' style='width:20px;height:10px;'></body></html>", "other-image.html");
                }
                asset.draw = context =>
                {
                    if (!doc.IsLoaded) return;
                    submitted = doc.SubmitRendering(context);
                    doc.Unload();
                    retainedAfterUnload = released == 0;
                };
                QualitySettings.renderPipeline = null;
                GraphicsSettings.defaultRenderPipeline = asset;
                for (var i = 0; i < 60 && !submitted; i++)
                {
                    camera.Render();
                    yield return null;
                }
                Assert.IsTrue(submitted);
                Assert.IsTrue(retainedAfterUnload, "管线 Submit 前卸载仍需保留图片直到 GPU fence 完成");
                for (var i = 0; i < 60 && released == 0; i++) yield return null;
                Assert.AreEqual(1, released);
                if (other)
                {
                    Assert.IsTrue(other.IsLoaded); Assert.IsTrue(other.DocumentElement.IsValid);
                    Assert.AreEqual(0, otherReleased);
                    other.Unload();
                    for (var i = 0; i < 60 && otherReleased == 0; i++) yield return null;
                    Assert.AreEqual(1, otherReleased);
                }
            }
            finally
            {
                asset.draw = null;
                if (doc && doc.IsLoaded) doc.Unload();
                if (other && other.IsLoaded) other.Unload();
                GraphicsSettings.defaultRenderPipeline = previous;
                QualitySettings.renderPipeline = previousQuality;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(asset);
                if (otherTexture) UnityEngine.Object.DestroyImmediate(otherTexture);
            }
        }

        [UnityTest]
        public IEnumerator InvalidSrpContextDoesNotCreateUncertainRecordsOrPreventUnload()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            Configure(doc, source => new ImageLease(Texture2D.whiteTexture, () => { }));
            doc.LoadHtml("<!doctype html><html><head></head><body><img src='owned.png' style='width:20px;height:10px;'/></body></html>", "image-resolver.html");
            yield return null; yield return null;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var host = typeof(Document).GetField("host", flags).GetValue(doc);
            var submissions = (ICollection)host.GetType().GetField("imageSubmissions", flags).GetValue(host);
            Assert.Throws<InvalidOperationException>(() => doc.SubmitRendering(default));
            Assert.AreEqual(0, submissions.Count, "无效上下文不能排队，必须在持有提交资源之前拒绝");
            Assert.Throws<InvalidOperationException>(() => doc.SubmitRendering(default));
            Assert.AreEqual(0, submissions.Count);
            Assert.DoesNotThrow(() => doc.Unload());
            UnityEngine.Object.DestroyImmediate(root); root = null;
        }

        [UnityTest]
        public IEnumerator ReleasedImageCacheRetiresGpuPinsWithoutUnloadingOtherUi()
        {
            var doc=FuiHtmlFixtureAssets.Create(root);
            var calls = 0; var released = 0;
            Configure(doc, _ => { calls++; return new ImageLease(texture, () => released++); });
            doc.TextResources = new[] { new TextResource("URUI/Tests/LiveTexture.styles.css", global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/LiveTexture.styles")) };
            FuiHtmlFixtureAssets.Load(doc, "URUI/Tests/LiveTexture");
            yield return null; yield return null;
            cameraObject.GetComponent<Camera>().Render();
            var image = doc.Q(className: "live-image");
            var parent = image.Parent;
            var replacement = image.Clone();
            image.Destroy();
            var release = typeof(Document).GetMethod("ReleaseImage");
            Assert.IsNotNull(release, "页面关闭时需要释放动态媒体缓存，不能一直保留到整个 Stage 退出。");
            release.Invoke(doc, new object[] { "live-frame" });
            for (var i = 0; i < 60 && released == 0; i++) yield return null;
            Assert.AreEqual(1, released);
            Assert.IsTrue(doc.IsLoaded, "其他页面继续使用同一文档。");
            parent.Add(replacement);
            yield return null; yield return null;
            Assert.AreEqual(2, calls, "相同资源路径失效后允许重新解析新的租约。");
            doc.Unload();
            for (var i = 0; i < 60 && released < 2; i++) yield return null;
            Assert.AreEqual(2, released);
        }
    }
}
