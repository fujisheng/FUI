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
    /// 实现 View 的同步与异步创建、打开、关闭、Transition 提交及失败回滚流程。
    /// </summary>
    public sealed partial class Navigator
    {
        ViewHandle OpenSync(
            Route route,
            ObservableObject viewModel,
            object param,
            OpenOptions options,
            OpenOrigin origin,
            ViewHandle owner,
            HashSet<Route> routePath)
        {
            if (!TryEnterRoute(route, routePath))
            {
                throw new InvalidOperationException($"A dynamic route dependency cycle was detected at {route}.");
            }

            var existing = FindReusableActive(route);
            if (existing != null)
            {
                RebindExistingViewModel(existing, viewModel, options);
                CommitExistingOpen(existing, origin, owner);
                routePath.Remove(route);
                return existing.Handle;
            }

            // RequiredBefore、页面本体和 AttachedAfter 共用同一回滚列表，任一步失败都按所有权逆序释放。
            var dependencies = new List<ViewHandle>();
            NavigationEntry instance = null;
            IViewLease viewLease = null;
            IPreloadLease pendingPreload = null;
            try
            {
                instance = TakeCachedInstance(route, origin, owner) ?? NewInstance(route, origin, owner);
                RebindCachedViewModel(instance, viewModel);
                AcquireOperationGate(instance, OperationPhase.Open);
                OpenDependenciesSync(route, DependencyTiming.RequiredBefore, instance.Handle, routePath, dependencies);
                instance.State = ViewState.Loading;
                if (instance.Instance == null)
                {
                    pendingPreload = TakePreload(route);
                    viewLease = viewProvider.Create(route.Descriptor, pendingPreload)
                        ?? throw new InvalidOperationException($"The provider could not create View '{route.AssetKey}'.");
                    pendingPreload = null;
                    instance.ViewLease = viewLease;
                    viewLease = null;
                    instance.Instance = route.Descriptor.CreateInstance(instance.ViewLease.View, viewModel);
                }
                instance.State = ViewState.Opening;
                instance.Instance.Enable(param);
                var replaced = CommitOpen(instance, options, origin);

                OpenDependenciesSync(route, DependencyTiming.AttachedAfter, instance.Handle, routePath, dependencies);
                foreach (var dependency in dependencies)
                {
                    instance.Dependencies.Add(dependency);
                }

                if (replaced != null && replaced != instance)
                {
                    Close(replaced.Handle);
                }

                StartEnterTransition(instance);
                routePath.Remove(route);
                return instance.Handle;
            }
            catch (Exception exception)
            {
                RestorePreload(route, pendingPreload);
                viewLease?.Dispose();
                RollbackOpen(instance, dependencies, exception);
                routePath.Remove(route);
                throw;
            }
        }

        async ValueTask<ViewHandle> OpenAsyncCore(
            Route route,
            ObservableObject viewModel,
            object param,
            OpenOptions options,
            OpenOrigin origin,
            ViewHandle owner,
            HashSet<Route> routePath,
            CancellationToken cancellationToken)
        {
            if (!TryEnterRoute(route, routePath))
            {
                throw new InvalidOperationException($"A dynamic route dependency cycle was detected at {route}.");
            }

            var existing = FindReusableActive(route);
            if (existing != null)
            {
                RebindExistingViewModel(existing, viewModel, options);
                CommitExistingOpen(existing, origin, owner);
                routePath.Remove(route);
                return existing.Handle;
            }

            // 异步路径与同步路径保持相同提交顺序，只在 I/O 和异步生命周期处让出执行权。
            var dependencies = new List<ViewHandle>();
            NavigationEntry instance = null;
            IViewLease viewLease = null;
            IPreloadLease pendingPreload = null;
            try
            {
                instance = TakeCachedInstance(route, origin, owner) ?? NewInstance(route, origin, owner);
                RebindCachedViewModel(instance, viewModel);
                AcquireOperationGate(instance, OperationPhase.Open);
                await OpenDependenciesAsync(route, DependencyTiming.RequiredBefore, instance.Handle, routePath, dependencies, cancellationToken);
                instance.State = ViewState.Loading;
                var version = instance.OperationVersion;
                if (instance.Instance == null)
                {
                    pendingPreload = TakePreload(route);
                    viewLease = await viewProvider.CreateAsync(route.Descriptor, pendingPreload, cancellationToken);
                    if (viewLease == null)
                    {
                        throw new InvalidOperationException($"The provider could not create View '{route.AssetKey}'.");
                    }
                    pendingPreload = null;
                    await executionContext.SwitchToMainThreadAsync(cancellationToken);
                    EnsureCurrent(instance, version, "CreateAsync");
                    instance.ViewLease = viewLease;
                    viewLease = null;
                    instance.Instance = route.Descriptor.CreateInstance(instance.ViewLease.View, viewModel);
                }
                instance.State = ViewState.Opening;
                await instance.Instance.EnableAsync(param, cancellationToken);
                await executionContext.SwitchToMainThreadAsync(cancellationToken);
                EnsureCurrent(instance, version, "OnOpenAsync");
                var replaced = CommitOpen(instance, options, origin);

                await OpenDependenciesAsync(route, DependencyTiming.AttachedAfter, instance.Handle, routePath, dependencies, cancellationToken);
                foreach (var dependency in dependencies)
                {
                    instance.Dependencies.Add(dependency);
                }

                if (replaced != null && replaced != instance)
                {
                    Close(replaced.Handle);
                }

                var transition = route.Descriptor.Settings.EnterTransitionProvider?.Get(instance.Instance.View);
                if (transition != null)
                {
                    await PlayTransitionSafelyAsync(instance, version, "OpenTransition", transition, cancellationToken);
                    await executionContext.SwitchToMainThreadAsync(cancellationToken);
                    EnsureCurrent(instance, version, "OpenTransition");
                }

                instance.State = ViewState.Open;
                instance.Instance.Interactable = true;
                ReleaseOperationGate(instance, version);
                routePath.Remove(route);
                return instance.Handle;
            }
            catch (Exception exception)
            {
                RestorePreload(route, pendingPreload);
                viewLease?.Dispose();
                RollbackOpen(instance, dependencies, exception);
                routePath.Remove(route);
                throw;
            }
        }

        NavigationEntry NewInstance(Route route, OpenOrigin origin, ViewHandle owner)
        {
            var handle = new ViewHandle(++nextHandle);
            var instance = new NavigationEntry
            {
                Handle = handle,
                Route = route,
                State = ViewState.Pending,
                OperationVersion = 1,
                ExplicitOwner = origin == OpenOrigin.Explicit,
            };
            if (owner.IsValid)
            {
                instance.Owners.Add(owner);
            }
            entries.Add(handle, instance);
            return instance;
        }

        IPreloadLease TakePreload(Route route)
        {
            if (!preloads.TryGetValue(route, out var preload))
            {
                return null;
            }

            preloads.Remove(route);
            return preload;
        }

        void RestorePreload(Route route, IPreloadLease preload)
        {
            if (preload == null)
            {
                return;
            }

            if (preloads.ContainsKey(route))
            {
                preload.Dispose();
                return;
            }

            preloads.Add(route, preload);
        }

        NavigationEntry CommitOpen(NavigationEntry instance, OpenOptions options, OpenOrigin origin)
        {
            executionContext.AssertMainThread();
            // 所有页面都参与独立 Canvas 排序；导航历史和焦点只属于显式打开页面。
            activeOrder.Add(instance.Handle);
            instance.Instance.Interactable = false;
            ReorderLayer(instance.Route.Descriptor.Settings.Layer);

            if (origin != OpenOrigin.Explicit)
            {
                return null;
            }

            var replaced = options.OpenMode == OpenMode.Replace ? GetFocused() : null;
            if (focusedHandle.IsValid && entries.TryGetValue(focusedHandle, out var previous) && previous != instance)
            {
                var hide = instance.Route.Descriptor.Settings.CoverageMode == CoverageMode.Hide;
                previous.HiddenByCoverage = hide;
                previous.State = ViewState.Covered;
                previous.Instance.Covered(hide);
            }

            if (instance.Route.Descriptor.Settings.HistoryMode == HistoryMode.Stack)
            {
                history.Add(instance.Handle);
            }

            focusedHandle = instance.Handle;
            instance.Instance.Focus();

            return replaced;
        }

        void StartEnterTransition(NavigationEntry instance)
        {
            var version = instance.OperationVersion;
            var transition = instance.Route.Descriptor.Settings.EnterTransitionProvider?.Get(instance.Instance.View);
            if (transition == null)
            {
                instance.State = ViewState.Open;
                instance.Instance.Interactable = true;
                ReleaseOperationGate(instance, version);
                return;
            }

            _ = CompleteOpenTransitionAsync(instance, version, transition);
        }

        async Task CompleteOpenTransitionAsync(NavigationEntry instance, int version, ITransition transition)
        {
            await PlayTransitionSafelyAsync(instance, version, "OpenTransition", transition, CancellationToken.None);
            await executionContext.SwitchToMainThreadAsync();
            if (!IsCurrent(instance, version) || instance.State != ViewState.Opening)
            {
                ReleaseOperationGate(instance, version);
                return;
            }

            instance.State = ViewState.Open;
            instance.Instance.Interactable = true;
            ReleaseOperationGate(instance, version);
        }

        bool TryBeginClose(ViewHandle handle, out NavigationEntry instance, out int version)
        {
            if (!entries.TryGetValue(handle, out instance)
                || instance.State == ViewState.Closing
                || instance.State == ViewState.Destroyed
                || instance.State == ViewState.Failed)
            {
                version = 0;
                return false;
            }

            // 先使旧异步操作失效，再退出导航，保证 Back 和焦点决策立即生效。
            instance.OperationVersion++;
            version = instance.OperationVersion;
            ReleaseOperationGate(instance);
            AcquireOperationGate(instance, OperationPhase.Close);
            instance.State = ViewState.Closing;
            instance.Instance.Interactable = false;
            try
            {
                StopTransition(instance.Route.Descriptor.Settings.EnterTransitionProvider, instance.Instance.View);
            }
            catch (Exception exception)
            {
                LogFailure(instance, "StopEnterTransition", exception, "Continue closing");
            }
            activeOrder.Remove(handle);
            history.Remove(handle);
            ReleaseFocusAfterClose(instance);
            ReorderLayer(instance.Route.Descriptor.Settings.Layer);
            return true;
        }

        async Task CompleteCloseOperationAsync(
            NavigationEntry instance,
            int version,
            ITransition transition,
            CancellationToken cancellationToken)
        {
            if (transition != null)
            {
                await PlayTransitionSafelyAsync(instance, version, "CloseTransition", transition, cancellationToken);
                await executionContext.SwitchToMainThreadAsync(CancellationToken.None);
            }
            if (!IsCurrent(instance, version) || instance.State != ViewState.Closing)
            {
                ReleaseOperationGate(instance, version);
                return;
            }

            Exception failure = null;
            try
            {
                await instance.Instance.DisableAsync(cancellationToken);
                await executionContext.SwitchToMainThreadAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                failure = exception;
                LogFailure(instance, "OnClose", exception, "Continue destruction");
            }

            if (!IsCurrent(instance, version) || instance.State != ViewState.Closing)
            {
                ReleaseOperationGate(instance, version);
                return;
            }

            // 父页面生命周期结束后再释放依赖，避免父 OnClose 执行期间依赖已被销毁。
            ReleaseDependencies(instance);
            ReleaseOperationGate(instance, version);
            if (failure == null && TryCacheInstance(instance))
            {
                return;
            }
            DestroyInstance(instance, failure == null ? "Close" : "CloseAfterFailure");
        }

        void DestroyInstance(NavigationEntry instance, string phase)
        {
            try
            {
                // 顶层 View 的资源所有权属于 ViewLease；Instance 只释放绑定与 Presenter。
                // 这样 Provider 在 Dispose 时只销毁一次实例，避免重复 Destroy。
                instance.Instance?.DestroyProjection();
            }
            catch (Exception exception)
            {
                LogFailure(instance, phase, exception, "Instance removed from Navigator");
            }
            finally
            {
                ReleaseOperationGate(instance);
                try
                {
                    instance.ViewLease?.Dispose();
                }
                catch (Exception exception)
                {
                    LogFailure(instance, phase + "Lease", exception, "Instance removed");
                }
                instance.ViewLease = null;
                entries.Remove(instance.Handle);
                activeOrder.Remove(instance.Handle);
                history.Remove(instance.Handle);
                instance.State = ViewState.Destroyed;
                terminalStates[instance.Handle] = ViewState.Destroyed;
            }
        }

        void RollbackOpen(NavigationEntry instance, List<ViewHandle> dependencies, Exception exception)
        {
            if (instance != null)
            {
                instance.OperationVersion++;
                instance.State = ViewState.Failed;
                ReleaseOperationGate(instance);
                LogFailure(instance, "Open", exception, "Roll back view and dependencies");
                var wasFocused = focusedHandle == instance.Handle;
                activeOrder.Remove(instance.Handle);
                history.Remove(instance.Handle);
                if (wasFocused)
                {
                    ReleaseFocusAfterClose(instance);
                }
                ReorderLayer(instance.Route.Descriptor.Settings.Layer);
                try
                {
                    instance.Instance?.DestroyProjection();
                }
                catch (Exception rollbackException)
                {
                    LogFailure(instance, "OpenRollback", rollbackException, "View removed");
                }
                try
                {
                    instance.ViewLease?.Dispose();
                }
                catch (Exception rollbackException)
                {
                    LogFailure(instance, "OpenLeaseRollback", rollbackException, "View removed");
                }
                instance.ViewLease = null;
                entries.Remove(instance.Handle);
                terminalStates[instance.Handle] = ViewState.Failed;
            }

            for (var index = dependencies.Count - 1; index >= 0; index--)
            {
                ReleaseOwner(dependencies[index], instance?.Handle ?? ViewHandle.Invalid);
            }
        }

    }
}
