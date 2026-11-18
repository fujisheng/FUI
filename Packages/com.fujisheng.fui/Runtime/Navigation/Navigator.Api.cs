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
    /// 提供打开、关闭、导航、预加载、缓存维护和状态查询等 Navigator 公共操作。
    /// </summary>
    public sealed partial class Navigator
    {
        /// <summary>独立预热操作捕获创建它的 Provider 与版本，防止迟到结果污染新后端。</summary>
        sealed class PreloadOperation
        {
            public int Generation;
            public IViewProvider Provider;
            public CancellationTokenSource Cancellation;
            public CancellationToken Token;
        }

        /// <summary>
        /// 同步加载资源并呈现强类型 Route；进入动画独立完成。
        /// </summary>
        public ViewHandle Open<TViewModel>(
            Route<TViewModel> route,
            object param = null,
            OpenOptions options = default)
            where TViewModel : ObservableObject
        {
            return Open((Route)route, param, options);
        }

        /// <summary>
        /// 使用已有 ViewModel 实例同步打开顶层页面，使同一份数据可以同时投影到不同 View。
        /// </summary>
        public ViewHandle OpenWithViewModel<TViewModel>(
            Route<TViewModel> route,
            TViewModel viewModel,
            object param = null,
            OpenOptions options = default)
            where TViewModel : ObservableObject
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            return Open(route, viewModel as ObservableObject, param, options);
        }

        /// <summary>
        /// 同步打开非泛型 Route，主要供生成 Registry 和统一业务入口使用。
        /// </summary>
        public ViewHandle Open(
            Route route,
            object param = null,
            OpenOptions options = default)
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            return Open(route, null, param, options);
        }

        ViewHandle Open(
            Route route,
            ObservableObject viewModel,
            object param,
            OpenOptions options)
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            return OpenSync(route, viewModel, param, options, OpenOrigin.Explicit, ViewHandle.Invalid, new HashSet<Route>());
        }

        /// <summary>
        /// 并行准备资源，按异步调用顺序呈现；返回时不等待进入动画。
        /// </summary>
        public async ValueTask<ViewHandle> OpenAsync<TViewModel>(
            Route<TViewModel> route,
            object param = null,
            OpenOptions options = default,
            CancellationToken cancellationToken = default)
            where TViewModel : ObservableObject
        {
            return await OpenAsync((Route)route, param, options, cancellationToken);
        }

        /// <summary>
        /// 使用已有 ViewModel 实例异步打开顶层页面，只等待本次投影完成。
        /// </summary>
        public async ValueTask<ViewHandle> OpenAsyncWithViewModel<TViewModel>(
            Route<TViewModel> route,
            TViewModel viewModel,
            object param = null,
            OpenOptions options = default,
            CancellationToken cancellationToken = default)
            where TViewModel : ObservableObject
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            return await OpenAsync(route, viewModel as ObservableObject, param, options, cancellationToken);
        }

        /// <summary>
        /// 异步打开非泛型 Route，并在每次 Unity 状态提交前切回主线程。
        /// </summary>
        public async ValueTask<ViewHandle> OpenAsync(
            Route route,
            object param = null,
            OpenOptions options = default,
            CancellationToken cancellationToken = default)
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }
            return await OpenAsync(route, null, param, options, cancellationToken);
        }

        async ValueTask<ViewHandle> OpenAsync(
            Route route,
            ObservableObject viewModel,
            object param,
            OpenOptions options,
            CancellationToken cancellationToken)
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            return await EnqueueOpen(route, viewModel, param, options, cancellationToken);
        }

        /// <summary>
        /// 立即把实例移出导航并进入 Closing；退出动画完成后再释放或缓存实例。
        /// </summary>
        public bool Close(ViewHandle handle)
        {
            AssertReady();
            if (!TryBeginClose(handle, out var instance, out var version))
            {
                return false;
            }

            var transition = GetCloseTransition(instance);
            _ = CompleteCloseOperationAsync(instance, version, transition, CancellationToken.None);

            return true;
        }

        /// <summary>
        /// 等待指定页面完成退出生命周期；取消退出动画也不会阻止最终关闭。
        /// </summary>
        public async ValueTask CloseAsync(ViewHandle handle, CancellationToken cancellationToken = default)
        {
            AssertReady();
            if (!TryBeginClose(handle, out var instance, out var version))
            {
                return;
            }

            var transition = GetCloseTransition(instance);
            await CompleteCloseOperationAsync(instance, version, transition, cancellationToken);
        }

        /// <summary>
        /// 关闭历史中最后一个仍存活的显式页面。依赖页面不会成为 Back 目标。
        /// </summary>
        public bool Back()
        {
            AssertReady();
            for (var index = history.Count - 1; index >= 0; index--)
            {
                var handle = history[index];
                if (IsAlive(handle))
                {
                    return Close(handle);
                }
            }

            return false;
        }

        /// <summary>
        /// 异步关闭历史中最后一个仍存活的显式页面。
        /// </summary>
        public async ValueTask<bool> BackAsync(CancellationToken cancellationToken = default)
        {
            AssertReady();
            for (var index = history.Count - 1; index >= 0; index--)
            {
                var handle = history[index];
                if (!IsAlive(handle))
                {
                    continue;
                }

                await CloseAsync(handle, cancellationToken);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将显式页面移动到同层最前方并切换焦点，不跨 Layer 改变顺序。
        /// </summary>
        public bool BringToFront(ViewHandle handle)
        {
            AssertReady();
            if (!entries.TryGetValue(handle, out var target)
                || !target.ExplicitOwner
                || !CanNavigate(target.State))
            {
                return false;
            }

            if (!MoveToLayerFront(target))
            {
                return false;
            }

            Focus(target);
            return true;
        }

        bool MoveToLayerFront(NavigationEntry target)
        {
            var targetLayer = target.Route.Descriptor.Settings.Layer;
            var lastSameLayerIndex = -1;
            for (var index = activeOrder.Count - 1; index >= 0; index--)
            {
                if (entries.TryGetValue(activeOrder[index], out var candidate)
                    && candidate.Route.Descriptor.Settings.Layer == targetLayer
                    && CanNavigate(candidate.State))
                {
                    lastSameLayerIndex = index;
                    break;
                }
            }

            var currentIndex = activeOrder.IndexOf(target.Handle);
            if (currentIndex < 0 || lastSameLayerIndex < 0)
            {
                return false;
            }

            // 已经是同层最前方时不修改列表；单页面层若先 Remove 再按旧索引 Insert 会越界。
            if (currentIndex == lastSameLayerIndex)
            {
                return true;
            }

            activeOrder.RemoveAt(currentIndex);
            if (currentIndex < lastSameLayerIndex)
            {
                lastSameLayerIndex--;
            }
            activeOrder.Insert(lastSameLayerIndex + 1, target.Handle);
            ReorderLayer(targetLayer);
            return true;
        }

        /// <summary>
        /// 查询 Handle 的当前或终态状态。未知 Handle 按 Destroyed 处理。
        /// </summary>
        public ViewState GetState(ViewHandle handle)
        {
            AssertReady();
            if (entries.TryGetValue(handle, out var instance))
            {
                return instance.State;
            }

            return terminalStates.TryGetValue(handle, out var state) ? state : ViewState.Destroyed;
        }

        /// <summary>
        /// 获取 Handle 当前持有的 ViewModel；关闭、失败或类型不匹配时返回 false。
        /// </summary>
        public bool TryGetViewModel<TViewModel>(ViewHandle handle, out TViewModel viewModel)
            where TViewModel : ObservableObject
        {
            AssertReady();
            if (entries.TryGetValue(handle, out var instance)
                && instance.State != ViewState.Destroyed
                && instance.State != ViewState.Failed
                && instance.Instance?.ViewModel is TViewModel typed)
            {
                viewModel = typed;
                return true;
            }

            viewModel = null;
            return false;
        }

        /// <summary>
        /// 判断 Handle 是否仍指向 Navigator 中的有效实例。
        /// </summary>
        public bool IsAlive(ViewHandle handle)
        {
            AssertReady();
            return entries.TryGetValue(handle, out var instance)
                && instance.State != ViewState.Destroyed
                && instance.State != ViewState.Failed;
        }

        /// <summary>
        /// 预热 Route 资源但不实例化 GameObject，也不触发 View、ViewModel 或 Presenter 生命周期。
        /// </summary>
        public async ValueTask PreloadAsync<TViewModel>(
            Route<TViewModel> route,
            CancellationToken cancellationToken = default)
            where TViewModel : ObservableObject
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (preloads.ContainsKey(route) || cache.Any(entry => entry.Route.Equals(route)))
            {
                return;
            }

            var operation = new PreloadOperation
            {
                Generation = preloadGeneration,
                Provider = viewProvider,
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
            };
            operation.Token = operation.Cancellation.Token;
            pendingPreloadOperations.Add(operation);
            using var gate = inputGate?.Acquire(ViewHandle.Invalid, OperationPhase.Preload);
            IPreloadLease preload = null;
            try
            {
                preload = await operation.Provider.PreloadAsync(route.Descriptor, operation.Token);
                await executionContext.SwitchToMainThreadAsync(CancellationToken.None);
                operation.Token.ThrowIfCancellationRequested();
                if (operation.Generation != preloadGeneration
                    || !ReferenceEquals(operation.Provider, viewProvider)
                    || shuttingDown)
                {
                    throw new OperationCanceledException("The preload result is stale.", operation.Token);
                }
                if (preload == null || !preload.Route.Equals(route.Descriptor))
                {
                    throw new InvalidOperationException($"The provider could not preload Route {route}.");
                }

                if (preloads.TryGetValue(route, out var replaced))
                {
                    replaced.Dispose();
                }
                preloads[route] = preload;
                preload = null;
            }
            finally
            {
                DisposePreparedResource(preload);
                await executionContext.SwitchToMainThreadAsync(CancellationToken.None);
                pendingPreloadOperations.Remove(operation);
                operation.Cancellation.Dispose();
            }
        }

        void InvalidatePendingPreloads()
        {
            preloadGeneration++;
            foreach (var operation in pendingPreloadOperations.ToArray())
            {
                try
                {
                    operation.Cancellation.Cancel();
                }
                catch (Exception exception)
                {
                    LogFailure(null, "CancelPreload", exception, "Late preload result will be rejected by generation");
                }
            }
        }

        /// <summary>
        /// 释放全部缓存实例和预热资源，不影响当前已打开页面。
        /// </summary>
        public void ClearCache()
        {
            AssertReady();
            foreach (var entry in cache.ToArray())
            {
                DestroyCachedEntry(entry, "ClearCache");
            }
            cache.Clear();
            foreach (var preload in preloads.Values)
            {
                preload.Dispose();
            }
            preloads.Clear();
        }

        /// <summary>
        /// 释放指定 Route 的缓存实例和预热资源。
        /// </summary>
        public void ClearCache(Route route)
        {
            AssertReady();
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            foreach (var entry in cache.Where(item => item.Route.Equals(route)).ToArray())
            {
                cache.Remove(entry);
                DestroyCachedEntry(entry, "ClearRouteCache");
            }

            if (preloads.TryGetValue(route, out var preload))
            {
                preloads.Remove(route);
                preload.Dispose();
            }
        }

        /// <summary>
        /// 推进 Timed 缓存计时。FUI 不创建隐藏 Driver，由宿主主动调用。
        /// </summary>
        public void Tick(float unscaledDeltaTime)
        {
            AssertReady();
            if (unscaledDeltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            }

            foreach (var entry in cache.ToArray())
            {
                if (entry.Route.Descriptor.Settings.CacheMode != CacheMode.Timed)
                {
                    continue;
                }

                entry.Age += unscaledDeltaTime;
                if (entry.Age < entry.Route.Descriptor.Settings.CacheDuration)
                {
                    continue;
                }

                cache.Remove(entry);
                DestroyCachedEntry(entry, "TimedCacheExpired");
            }
        }

        /// <summary>
        /// 无动画释放所有活动实例、缓存和预热资源。
        /// </summary>
        public void Shutdown()
        {
            AssertReady();
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            CancelPendingOpens();
            InvalidatePendingPreloads();
            var snapshot = entries.Values.ToArray();
            activeOrder.Clear();
            history.Clear();
            focusedHandle = ViewHandle.Invalid;

            foreach (var instance in snapshot)
            {
                instance.OperationVersion++;
                DestroyInstance(instance, "Shutdown");
            }

            entries.Clear();
            foreach (var entry in cache.ToArray())
            {
                DestroyCachedEntry(entry, "ShutdownCache");
            }
            cache.Clear();
            foreach (var preload in preloads.Values)
            {
                preload.Dispose();
            }
            preloads.Clear();
            shuttingDown = false;
        }

        /// <summary>
        /// 关闭除指定 Layer 外的全部活动页面。
        /// </summary>
        public void CloseAllExcept(Layer layer)
        {
            AssertReady();
            var handles = activeOrder
                .Where(handle => entries.TryGetValue(handle, out var instance)
                    && instance.Route.Descriptor.Settings.Layer != layer)
                .ToArray();
            foreach (var handle in handles)
            {
                Close(handle);
            }
        }

        /// <summary>
        /// 关闭指定 Layer 的全部活动页面。
        /// </summary>
        public void CloseLayer(Layer layer)
        {
            AssertReady();
            var handles = activeOrder
                .Where(handle => entries.TryGetValue(handle, out var instance)
                    && instance.Route.Descriptor.Settings.Layer == layer)
                .ToArray();
            foreach (var handle in handles)
            {
                Close(handle);
            }
        }

    }
}
