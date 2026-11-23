using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace FUI.Rendering.URUI.URP
{
    /// <summary>
    /// 在 URP 最终场景输出之后，以输出分辨率执行屏幕 UI（同时支持 Unity 6 RenderGraph 与 Compatibility Mode，以及多 Surface Stage）。
    /// URP 不执行 CameraEvent.AfterEverything，因此必须由 RendererFeature 提交。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "FUI.Rendering.URUI.URP", sourceClassName: "UruiRendererFeature")]
    public sealed class RendererFeature : ScriptableRendererFeature
    {
        sealed class Pass : ScriptableRenderPass, System.IDisposable
        {
            sealed class RenderGraphPassData
            {
                internal TextureHandle ColorTarget;
                internal RenderTexture RenderTexture;
                internal Rect Viewport;
                internal int TargetDisplay;
            }

#if URP_COMPATIBILITY_MODE
            RTHandle output = RTHandles.Alloc(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            RenderTargetIdentifier destination;
            Rect viewport;
            global::URUI.Rendering.RenderTargetInfo targetInfo;
#endif

            public Pass()
            {
                profilingSampler = new ProfilingSampler("URUI Screen UI");
                // URP resolves/upscales at +1. Its Overlay pass is enqueued after features at +2;
                // the stable pass sort keeps regular Overlay Canvas above this screen UI.
                renderPassEvent = RenderPassEvent.AfterRendering + 2;
            }

            public void Dispose()
            {
#if URP_COMPATIBILITY_MODE
                output?.Release();
                output = null;
#endif
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!HasRenderableStage())
                {
                    return;
                }
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                var camera = cameraData.baseCamera ? cameraData.baseCamera : cameraData.camera;
                var cameraViewport = camera ? camera.pixelRect : default;
                if (cameraViewport.width <= 0 || cameraViewport.height <= 0)
                {
                    return;
                }
                var colorTarget = resourceData.activeColorTexture;
                using (var builder = renderGraph.AddUnsafePass<RenderGraphPassData>("URUI Screen UI", out var passData, profilingSampler))
                {
                    passData.ColorTarget = colorTarget;
                    passData.RenderTexture = cameraData.targetTexture;
                    passData.Viewport = cameraViewport;
                    passData.TargetDisplay = camera ? camera.targetDisplay : 0;
                    if (colorTarget.IsValid())
                    {
                        builder.UseTexture(colorTarget, AccessFlags.ReadWrite);
                    }
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (RenderGraphPassData data, UnsafeGraphContext ctx) =>
                    {
                        var stages = Stage.ActiveStages;
                        if (stages.Count == 0)
                        {
                            return;
                        }
                        var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        var target = data.RenderTexture
                            ? new RenderTargetIdentifier(data.RenderTexture)
                            : data.ColorTarget.IsValid()
                                ? (RenderTargetIdentifier)data.ColorTarget
                                : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                        var info = data.RenderTexture
                            ? global::URUI.Rendering.RenderTargetInfo.FromTexture(data.RenderTexture)
                            : global::URUI.Rendering.RenderTargetInfo.FromScreen(data.TargetDisplay, 1);
                        for (var i = 0; i < stages.Count; i++)
                        {
                            var stage = stages[i];
                            if (stage && stage.PageCount > 0)
                            {
                                stage.Surface.SubmitRendering(cmd, target, data.Viewport, info);
                            }
                        }
                    });
                }
            }

#if URP_COMPATIBILITY_MODE
#pragma warning disable CS0618
#pragma warning disable CS0672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // URP 的相机栈继承 base camera 的输出视口；最终叠加相机
                // 自身的 rect 和中间渲染分辨率均不能替代它。
                var camera = renderingData.cameraData.baseCamera ? renderingData.cameraData.baseCamera : renderingData.cameraData.camera;
                viewport = camera ? camera.pixelRect : default;
                var target = renderingData.cameraData.targetTexture;
                destination = target ? new RenderTargetIdentifier(target) : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                targetInfo = target ? global::URUI.Rendering.RenderTargetInfo.FromTexture(target) : global::URUI.Rendering.RenderTargetInfo.FromScreen(camera ? camera.targetDisplay : 0, 1);
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref output, destination);
                // FinalPostProcess不一定更新renderer默认附件；必须像URP Overlay UI一样声明最终输出。
                ConfigureTarget(output);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var stages = Stage.ActiveStages;
                if (stages.Count == 0 || viewport.width <= 0 || viewport.height <= 0)
                {
                    return;
                }
                for (var i = 0; i < stages.Count; i++)
                {
                    var stage = stages[i];
                    if (stage && stage.PageCount > 0)
                    {
                        stage.Surface.SubmitRendering(context, destination, viewport, targetInfo);
                    }
                }
            }
#pragma warning restore CS0672
#pragma warning restore CS0618
#endif
        }

        Pass pass;

        static bool HasRenderableStage()
        {
            var stages = Stage.ActiveStages;
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage && stage.PageCount > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Create()
        {
            pass?.Dispose();
            pass = new Pass();
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game && renderingData.cameraData.resolveFinalTarget && HasRenderableStage())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }
}
