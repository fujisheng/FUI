using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FUI.Rendering.URUI.URP
{
    /// <summary>
    /// 在 URP 最终场景输出之后，以输出分辨率执行屏幕 UI。
    /// URP 不执行 CameraEvent.AfterEverything，因此必须由 RendererFeature 提交。
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "FUI.Rendering.URUI.URP", sourceClassName: "UruiRendererFeature")]
    public sealed class RendererFeature : ScriptableRendererFeature
    {
        sealed class Pass : ScriptableRenderPass, System.IDisposable
        {
            RTHandle output = RTHandles.Alloc(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            RenderTargetIdentifier destination;
            Rect viewport;
            global::URUI.Rendering.RenderTargetInfo targetInfo;

            public Pass()
            {
                // URP resolves/upscales at +1. Its Overlay pass is enqueued after features at +2;
                // the stable pass sort keeps regular Overlay Canvas above this screen UI.
                renderPassEvent = RenderPassEvent.AfterRendering + 2;
            }

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

            public void Dispose()
            {
                output?.Release();
                output = null;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var stage = Stage.Current;
                if (stage == null || stage.PageCount == 0)
                {
                    return;
                }

                if (viewport.width <= 0 || viewport.height <= 0)
                {
                    return;
                }
                stage.Surface.SubmitRendering(context, destination, viewport, targetInfo);
            }
        }

        Pass pass;

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
            if (renderingData.cameraData.cameraType == CameraType.Game && renderingData.cameraData.resolveFinalTarget)
            {
                renderer.EnqueuePass(pass);
            }
        }
    }
}
