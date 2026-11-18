using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FUI.Navigation;
using FUI.RedDot;
using FUI.RedDot.UGUI;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Resources;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Samples.RedDot
{
    /// <summary>
    /// 按 Route.AssetKey 从 Resources 加载独立 Prefab。
    /// Provider 只负责 View 的创建和红点视觉装配，不包含任何页面跳转或业务判断。
    /// </summary>
    sealed class ViewProvider : IViewProvider
    {
        readonly SampleContext context;

        public ViewProvider(SampleContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
        {
            preload?.Dispose();
            var prefab = UnityEngine.Resources.Load<GameObject>("UI/Prefabs/" + route.AssetKey);
            if (prefab == null)
            {
                throw new MissingReferenceException($"The red dot sample requires Resources/UI/Prefabs/{route.AssetKey}.prefab.");
            }

            var loader = new SampleAssetLoader();
            var gameObject = Object.Instantiate(prefab);
            gameObject.name = route.AssetKey;
            gameObject.SetActive(false);
            SampleViewSetup.Configure(gameObject, route.AssetKey, context);
            return new SampleViewLease(View.Create(loader, gameObject, route.AssetKey));
        }

        public ValueTask<IViewLease> CreateAsync(RouteDescriptor route, IPreloadLease preload, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<IViewLease>(Create(route, preload));
        }

        public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<IPreloadLease>(new SamplePreloadLease(route));
        }
    }

    /// <summary>给工具生成的静态 Prefab 增加 RedDotElement 和可选局部 Scope。</summary>
    static class SampleViewSetup
    {
        static readonly Dictionary<string, string[]> BadgeNames = new Dictionary<string, string[]>
        {
            { "GameHomeView", new[] { "HomeBadge", "HeroNavBadge", "BagNavBadge", "QuestNavBadge", "MailNavBadge", "SeasonNavBadge" } },
            { "BagView", new[] { "BagItemBadge", "BagDotBadge", "BagCountBadge", "BagCompactBadge", "SharedBadge" } },
            { "QuestView", new[] { "QuestClaimBadge", "HintBadge", "SharedBadge", "ReadinessBadge" } },
            { "HeroView", new[] { "HeroUpgradeBadge" } },
            { "MailView", new[] { "MailItemBadge", "ThreatBadge", "LinkBadge" } },
            { "SeasonView", new[] { "SeasonRewardBadge", "WingBadge", "DefaultFleetBadge" } },
            { "TacticalView", new[] { "SimulationBadge" } },
            { "TrackedStatusView", new[] { "TrackedBadge" } },
        };
        public static void Configure(GameObject root, string assetKey, SampleContext context)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<Button>() == null)
                {
                    // 纯装饰不挡点击；说明面板和ScrollRect视口必须能接收射线与拖动。
                    image.raycastTarget = image.name == "DetailsPanel" || image.name == "Viewport";
                }
            }
            foreach (var label in root.GetComponentsInChildren<Text>(true)) { label.raycastTarget = false; }
            var details = Require(root.transform, "DetailsScroll");
            details.gameObject.AddComponent<SampleSignalDetailsLayout>().Configure(
                details.GetComponent<ScrollRect>(), Require(details, "DetailsText").GetComponent<Text>());
            if (assetKey == "GameHomeView")
            {
                Require(root.transform, "SeasonButton").gameObject.AddComponent<SampleFleetScope>().Configure(context);
            }
            if (assetKey == "SeasonView") { root.AddComponent<SampleScopeLayout>().Configure(context); }
            if (assetKey == "TacticalView") { root.AddComponent<RedDotScope>().Configure(context.Simulation.Center); }
            foreach (var name in BadgeNames[assetKey])
            {
                var host = Require(root.transform, name);
                var visual = Require(host, name + "Visual");
                var label = Require(visual, name + "Count").GetComponent<Text>();
                var mode = name == "HomeBadge" || name == "BagDotBadge" ? RedDotVisualMode.Dot
                    : name == "BagCountBadge" || name == "BagCompactBadge" ? RedDotVisualMode.Count : RedDotVisualMode.Auto;
                host.gameObject.AddComponent<RtsRedDotElement>().Configure(visual.gameObject, label, mode,
                    name == "BagCompactBadge" ? 9 : 99, assetKey == "TrackedStatusView");
            }
        }
        internal static Transform Require(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) { return child; }
            }
            throw new MissingReferenceException(root.name + " requires descendant " + name);
        }
    }

    sealed class SampleViewLease : IViewLease
    {
        bool disposed;

        public SampleViewLease(IView view)
        {
            View = view;
        }

        public IView View { get; private set; }
        public IAssetScope Assets { get; } = new EmptyAssetScope();

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            View?.Destroy();
            View = null;
            Assets.Dispose();
        }
    }

    sealed class SamplePreloadLease : IPreloadLease
    {
        public SamplePreloadLease(RouteDescriptor route)
        {
            Route = route;
        }

        public RouteDescriptor Route { get; }
        public void Dispose() { }
    }

    sealed class EmptyAssetScope : IAssetScope
    {
        public IAssetLease<T> Load<T>(string key) => throw Unsupported();
        public ValueTask<IAssetLease<T>> LoadAsync<T>(string key, CancellationToken token) => throw Unsupported();
        public IAssetLease<T> Instantiate<T>(string key) => throw Unsupported();
        public ValueTask<IAssetLease<T>> InstantiateAsync<T>(string key, CancellationToken token) => throw Unsupported();
        public void Dispose() { }

        static NotSupportedException Unsupported()
        {
            return new NotSupportedException("The red dot sample does not load assets through IAssetScope.");
        }
    }

    sealed class SampleAssetLoader : IAssetLoader
    {
        public T Load<T>(string path) where T : Object => null;
        public ValueTask<T> LoadAsync<T>(string path, CancellationToken? cancellationToken = null) where T : Object => new ValueTask<T>((T)null);
        public GameObject CreateGameObject(string path) => null;
        public ValueTask<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken = null) => new ValueTask<GameObject>((GameObject)null);

        public void DestroyGameObject(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        public void Release() { }
    }

    /// <summary>使用 Prefab 自带的 Legacy Text 显示数量，避免 Sample 强制导入 TMP Essentials。</summary>
    sealed class RtsRedDotElement : RedDotElement
    {
        Text label;
        bool pulse;
        float pulseUntil;

        public void Configure(GameObject root, Text value, RedDotVisualMode visualMode, int limit, bool animate)
        {
            visualRoot = root;
            countLabel = null;
            label = value;
            mode = visualMode;
            countLimit = limit;
            pulse = animate;
        }

        protected override void ApplyState(RedDotState state)
        {
            base.ApplyState(state);
            if (pulse) { pulseUntil = Time.unscaledTime + 0.5F; }
            if (label == null)
            {
                return;
            }

            var showCount = state.IsVisible
                && (mode == RedDotVisualMode.Count || mode == RedDotVisualMode.Auto && state.Count > 1);
            label.gameObject.SetActive(showCount);
            if (showCount)
            {
                label.text = state.Count > countLimit ? countLimit + "+" : state.Count.ToString();
            }
        }
        // 动画只作用于本Prefab的视觉，不修改Key或Center状态。
        void Update()
        {
            if (!pulse || visualRoot == null) { return; }
            var remaining = Mathf.Clamp01((pulseUntil - Time.unscaledTime) * 2F);
            visualRoot.transform.localScale = Vector3.one * (1F + Mathf.Sin(remaining * Mathf.PI) * 0.12F);
        }
    }
}
