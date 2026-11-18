using FUI.Navigation;
using FUI.RedDot.UGUI;
using UnityEngine;

namespace FUI.Samples.RedDot
{
    /// <summary>唯一组合根：持有世界和导航，不在这里计算业务或红点。</summary>
    [AddComponentMenu("FUI/Samples/Red Dot Sample Bootstrap")]
    public sealed class Bootstrap : MonoBehaviour
    {
        Navigator navigator;
        SampleContext context;
        float nextRefresh;
        /// <summary>组合根是否已初始化。</summary>
        public bool IsInitialized => context != null;
        internal Navigator Navigator => navigator;
        internal SampleContext Context => context;
        void Awake() => InitializeForTests();
        internal void InitializeForTests()
        {
            if (context != null) { return; }
            Routes.Initialize();
            context = new SampleContext();
            navigator = new Navigator(new ViewProvider(context));
            navigator.Initialize();
            context.AttachNavigator(navigator);
            // 先创建Element，再配置Runtime，实际演示晚配置。
            RedDotRuntime.Shutdown();
            navigator.Open(Routes.GameHomeView, context);
            context.Connect();
        }
        void LateUpdate()
        {
            if (context == null) { return; }
            context.TickLocalWorlds();
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.25F;
                context.Refresh();
            }
        }
        /// <summary>仅供测试显式推进，普通游戏Command不调用。</summary>
        public void FlushNow() => context?.FlushForTests();
        void OnDestroy()
        {
            navigator?.Shutdown(); navigator = null;
            RedDotRuntime.Shutdown();
            context?.Dispose(); context = null;
        }
    }
}
