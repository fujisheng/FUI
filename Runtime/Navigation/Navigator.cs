using FUI.Binding;
using FUI.Diagnostics;
using FUI.Infrastructure;
using FUI.Presentation;
using FUI.Resources;
using FUI.Rendering;
using FUI.Transitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Navigation
{
    /// <summary>
    /// FUI 3.0 页面状态机。
    /// 顶层页面彼此独立，不创建 Root 或 Driver GameObject；导航、Canvas 排序、资源所有权由 Navigator 统一提交。
    /// </summary>
    public sealed partial class Navigator
    {
        /// <summary>
        /// 描述一次打开请求的来源。依赖页面参与显示与所有权，但只有显式打开才参与导航。
        /// </summary>
        enum OpenOrigin
        {
            Explicit,
            RequiredDependency,
            AttachedDependency,
        }

        /// <summary>
        /// 保存一个活动 View 的导航状态、资源所有权、依赖关系和异步操作版本。
        /// </summary>
        sealed class NavigationEntry
        {
            public ViewHandle Handle;
            public Route Route;
            public ViewInstance Instance;
            public IViewLease ViewLease;
            public IDisposable OperationGate;
            public int OperationGateVersion;
            public ViewState State;
            // 每次打开或关闭都会递增版本，防止较早完成的异步结果覆盖当前状态。
            public int OperationVersion;
            public bool HiddenByCoverage;
            // 显式所有权表示该实例可以进入历史、获得焦点并被 BringToFront。
            public bool ExplicitOwner;
            // Owners 保存依赖该实例的父 Handle；Dependencies 保存该实例持有的子 Handle。
            public readonly HashSet<ViewHandle> Owners = new HashSet<ViewHandle>();
            public readonly HashSet<ViewHandle> Dependencies = new HashSet<ViewHandle>();
        }

        /// <summary>
        /// 保存一个已缓存 View 的资源租约、缓存时长和最近使用顺序。
        /// </summary>
        sealed class CachedEntry
        {
            public Route Route;
            public ViewInstance Instance;
            public IViewLease ViewLease;
            public float Age;
            public long LastUse;
        }

        // entries 只保存当前可操作实例；terminalStates 为旧 Handle 保留不可逆终态。
        readonly Dictionary<ViewHandle, NavigationEntry> entries = new Dictionary<ViewHandle, NavigationEntry>();
        readonly Dictionary<ViewHandle, ViewState> terminalStates = new Dictionary<ViewHandle, ViewState>();
        // activeOrder 只描述视觉顺序，history 只描述显式导航，两者不能互相替代。
        readonly List<ViewHandle> activeOrder = new List<ViewHandle>();
        readonly List<ViewHandle> history = new List<ViewHandle>();
        readonly List<CachedEntry> cache = new List<CachedEntry>();
        readonly Dictionary<Route, IPreloadLease> preloads = new Dictionary<Route, IPreloadLease>();

        IViewProvider viewProvider;
        IMainThreadDispatcher executionContext;
        IInputGate inputGate;
        ViewHandle focusedHandle;
        long nextHandle;
        long cacheClock;
        bool initialized;
        bool shuttingDown;

        public Navigator(
            IViewProvider viewProvider,
            IMainThreadDispatcher executionContext = null,
            IInputGate inputGate = null)
        {
            this.viewProvider = viewProvider;
            this.executionContext = executionContext;
            this.inputGate = inputGate;
        }

        /// <summary>
        /// 在 Unity 主线程初始化 Navigator，并固定后续状态提交所使用的执行上下文。
        /// </summary>
        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (viewProvider == null)
            {
                throw new InvalidOperationException("FUI IViewProvider is not configured.");
            }

            executionContext ??= new UnityMainThreadDispatcher();
            executionContext.AssertMainThread();
            initialized = true;
        }

        /// <summary>
        /// 替换后续页面创建所使用的资源 Provider。
        /// </summary>
        public void SetViewProvider(IViewProvider provider)
        {
            AssertReady();
            viewProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// 设置外部输入门；传入 null 表示打开和关闭期间不额外阻塞输入。
        /// </summary>
        public void SetInputGate(IInputGate gate)
        {
            AssertReady();
            inputGate = gate;
        }

    }
}
