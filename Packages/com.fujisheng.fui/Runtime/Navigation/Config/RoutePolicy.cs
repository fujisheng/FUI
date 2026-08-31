using System;
using FUI.Transitions;

namespace FUI.Navigation
{
    /// <summary>
    /// Route 是否作为显式导航记录进入 Back 历史。
    /// </summary>
    public enum HistoryMode
    {
        Stack,
        Transient,
    }

    /// <summary>
    /// 新显式页面覆盖当前焦点页面时，旧页面是否继续可见。
    /// </summary>
    public enum CoverageMode
    {
        KeepVisible,
        Hide,
    }

    /// <summary>
    /// 页面关闭后的实例与资源保留策略。
    /// </summary>
    public enum CacheMode
    {
        None,
        KeepAlive,
        Timed,
    }

    /// <summary>
    /// 依赖相对父页面的创建和视觉提交时机。
    /// </summary>
    public enum DependencyTiming
    {
        RequiredBefore,
        AttachedAfter,
    }

    /// <summary>
    /// Route 依赖声明。依赖只产生所有权和视觉顺序，不独立进入父页面的导航历史。
    /// </summary>
    public sealed class RouteDependency
    {
        readonly Func<Route> routeFactory;

        public DependencyTiming Timing { get; }

        public Route Route => routeFactory();

        public RouteDependency(DependencyTiming timing, Func<Route> routeFactory)
        {
            Timing = timing;
            this.routeFactory = routeFactory ?? throw new ArgumentNullException(nameof(routeFactory));
        }
    }

    /// <summary>
    /// Route 固化后的不可变运行时配置。
    /// Source Generator 在编译期读取 RoutePolicyAttribute，运行时不再扫描 Attribute 或程序集。
    /// </summary>
    public sealed class RoutePolicy
    {
        public static RoutePolicy Default { get; } = new RoutePolicy();

        public Layer Layer { get; }
        public HistoryMode HistoryMode { get; }
        public CoverageMode CoverageMode { get; }
        public CacheMode CacheMode { get; }
        public bool AllowMultiple { get; }
        public int CacheCapacity { get; }
        public float CacheDuration { get; }
        public ITransitionProvider EnterTransitionProvider { get; }
        public ITransitionProvider ExitTransitionProvider { get; }
        public RouteDependency[] Dependencies { get; }

        public RoutePolicy(
            Layer layer = Layer.Panel,
            HistoryMode historyMode = HistoryMode.Stack,
            CoverageMode coverageMode = CoverageMode.KeepVisible,
            CacheMode cacheMode = CacheMode.None,
            bool allowMultiple = false,
            int cacheCapacity = 1,
            float cacheDuration = 0f,
            ITransitionProvider enterTransitionProvider = null,
            ITransitionProvider exitTransitionProvider = null,
            RouteDependency[] dependencies = null)
        {
            // 即使不启用缓存也保持容量合法，避免切换 CacheMode 后出现隐式无效配置。
            if (cacheCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cacheCapacity));
            }

            if (cacheMode == CacheMode.Timed && cacheDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Timed caching requires a positive duration.");
            }

            Layer = layer;
            HistoryMode = historyMode;
            CoverageMode = coverageMode;
            CacheMode = cacheMode;
            AllowMultiple = allowMultiple;
            CacheCapacity = cacheCapacity;
            CacheDuration = cacheDuration;
            EnterTransitionProvider = enterTransitionProvider;
            ExitTransitionProvider = exitTransitionProvider;
            Dependencies = dependencies ?? Array.Empty<RouteDependency>();
        }
    }
}
