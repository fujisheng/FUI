using FUI.RedDot;
using FUI.RedDot.UGUI;
using UnityEngine;

namespace FUI.Samples.RedDot
{
    /// <summary>仅演示UI世界边界：真实配置Scope和改变父级，不复制红点状态。</summary>
    sealed class SampleScopeLayout : MonoBehaviour
    {
        SampleContext context;
        RedDotScope fleetScope;
        RedDotScope wingScope;
        Transform wingPanel;
        Transform fleetDock;
        Transform defaultDock;
        IRedDotReader previousFleet;
        int generation = -1;
        public void Configure(SampleContext value)
        {
            context = value;
            var fleetPanel = SampleViewSetup.Require(transform, "FleetPanel");
            fleetScope = fleetPanel.gameObject.AddComponent<RedDotScope>();
            fleetDock = SampleViewSetup.Require(transform, "FleetDock");
            defaultDock = SampleViewSetup.Require(transform, "DefaultDock");
            wingPanel = SampleViewSetup.Require(transform, "WingPanel");
            Apply();
        }
        void OnEnable() { if (context != null) { context.Changed += Apply; Apply(); } }
        void OnDisable() { if (context != null) { context.Changed -= Apply; } }
        void Apply()
        {
            if (previousFleet != context.FleetReader)
            {
                previousFleet = context.FleetReader;
                if (previousFleet == null) { fleetScope.Clear(); }
                else { fleetScope.Configure(previousFleet); }
            }
            var parent = context.WingAttached ? fleetDock : defaultDock;
            var rebuilding = generation != context.WingGeneration || wingPanel.parent != parent;
            // 调动编队本身是一次隐藏/恢复生命周期，避免后代仍持有旧父级的订阅。
            if (rebuilding) { wingPanel.gameObject.SetActive(false); }
            if (generation != context.WingGeneration)
            {
                generation = context.WingGeneration;
                if (context.WingEnabled)
                {
                    wingScope = wingPanel.gameObject.AddComponent<RedDotScope>();
                    wingScope.Configure(context.WingReader);
                }
                else if (wingScope != null)
                {
                    // Destroy触发框架上下文通知；没有手写状态回填或回退逻辑。
                    wingScope.Clear();
                    Destroy(wingScope); wingScope = null;
                }
            }
            if (wingPanel.parent != parent)
            {
                wingPanel.SetParent(parent, false);
                var rect = (RectTransform)wingPanel;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0F, 1F);
                rect.anchoredPosition = Vector2.zero;
            }
            if (rebuilding) { wingPanel.gameObject.SetActive(true); }
        }
    }
}
