#if URP_COMPATIBILITY_MODE
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FUI.Rendering.URUI.URP;

namespace FUI.Rendering.URUI.Tests.Editor
{
    public sealed class RendererFeatureTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void FinalOutputPreservesBaseCameraPixelViewport(bool offscreen, bool stacked)
        {
            var feature = ScriptableObject.CreateInstance<RendererFeature>();
            var root = new GameObject("Viewport base camera", typeof(Camera));
            var overlay = new GameObject("Viewport overlay camera", typeof(Camera));
            var target = offscreen ? new RenderTexture(128, 64, 0) : null;
            using var frame = new ContextContainer();
            var cameraData = frame.Create<UniversalCameraData>();
            var camera = root.GetComponent<Camera>();
            camera.targetTexture = target;
            camera.pixelRect = new Rect(16, 8, 64, 32);
            overlay.GetComponent<Camera>().pixelRect = new Rect(0, 0, 8, 8);
            cameraData.camera = stacked ? overlay.GetComponent<Camera>() : camera;
            cameraData.baseCamera = stacked ? camera : null;
            cameraData.targetTexture = target;
            var data = (RenderingData)Activator.CreateInstance(typeof(RenderingData), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { frame }, null);
            using var command = new CommandBuffer();
            try
            {
                feature.Create();
                var pass = (ScriptableRenderPass)typeof(RendererFeature).GetField("pass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feature);
                pass.OnCameraSetup(command, ref data);
                var viewport = pass.GetType().GetField("viewport", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(viewport, "最终输出必须携带像素视口，不能假设原点为零。");
                Assert.AreEqual(camera.pixelRect, (Rect)viewport.GetValue(pass));
            }
            finally
            {
                feature.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(overlay);
                UnityEngine.Object.DestroyImmediate(feature);
                if (target)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScreenPassExplicitlySelectsFinalCameraStackOutput(bool offscreen)
        {
            var feature = ScriptableObject.CreateInstance<RendererFeature>();
            var target = offscreen ? new RenderTexture(64, 32, 0) : null;
            using var frame = new ContextContainer();
            var cameraData = frame.Create<UniversalCameraData>();
            cameraData.targetTexture = target;
            var data = (RenderingData)Activator.CreateInstance(typeof(RenderingData), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { frame }, null);
            using var command = new CommandBuffer();
            try
            {
                feature.Create();
                var pass = (ScriptableRenderPass)typeof(RendererFeature).GetField("pass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feature);
                pass.OnCameraSetup(command, ref data);
                var expected = target ? new RenderTargetIdentifier(target) : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                Assert.IsNotNull(pass.colorAttachmentHandle, "屏幕UI必须声明最终输出附件，不能继承中间颜色目标。");
                Assert.AreEqual(expected, pass.colorAttachmentHandle.nameID);
                var handle = pass.colorAttachmentHandle;
                pass.OnCameraSetup(command, ref data);
                Assert.AreSame(handle, pass.colorAttachmentHandle, "每帧设置附件必须复用包装器，不能持续分配。");
            }
            finally
            {
                feature.Dispose();
                UnityEngine.Object.DestroyImmediate(feature);
                if (target)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }
    }
}

#endif
