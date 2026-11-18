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
    /// 维护焦点、层级、缓存、依赖所有权、操作门和异步版本一致性。
    /// </summary>
    public sealed partial class Navigator
    {
        /// <summary>已有页面复用前的导航快照，用于把失败提交恢复为原状态。</summary>
        sealed class NavigationSnapshot
        {
            public readonly Dictionary<ViewHandle, EntrySnapshot> Entries = new Dictionary<ViewHandle, EntrySnapshot>();
            public readonly List<ViewHandle> ActiveOrder = new List<ViewHandle>();
            public readonly List<ViewHandle> History = new List<ViewHandle>();
            public ViewHandle FocusedHandle;
        }

        /// <summary>记录单个导航条目在事务回滚时需要恢复的状态。</summary>
        sealed class EntrySnapshot
        {
            public NavigationEntry Entry;
            public ObservableObject ViewModel;
            public ViewState State;
            public bool EnterTransitionPending;
            public bool HiddenByCoverage;
            public bool ExplicitOwner;
            public bool Visible;
            public bool Interactable;
            public readonly HashSet<ViewHandle> Owners = new HashSet<ViewHandle>();
        }

        NavigationSnapshot CaptureNavigationSnapshot()
        {
            var snapshot = new NavigationSnapshot { FocusedHandle = focusedHandle };
            snapshot.ActiveOrder.AddRange(activeOrder);
            snapshot.History.AddRange(history);
            foreach (var pair in entries)
            {
                var instance = pair.Value;
                var entry = new EntrySnapshot
                {
                    Entry = instance,
                    ViewModel = instance.Instance?.ViewModel,
                    State = instance.State,
                    EnterTransitionPending = instance.EnterTransitionPending,
                    HiddenByCoverage = instance.HiddenByCoverage,
                    ExplicitOwner = instance.ExplicitOwner,
                    Visible = instance.Instance?.View.Visible ?? false,
                    Interactable = instance.Instance?.Interactable ?? false,
                };
                entry.Owners.UnionWith(instance.Owners);
                snapshot.Entries.Add(pair.Key, entry);
            }
            return snapshot;
        }

        void RestoreNavigationSnapshot(NavigationSnapshot snapshot, Exception originalException)
        {
            foreach (var pair in entries.ToArray())
            {
                if (!snapshot.Entries.ContainsKey(pair.Key))
                {
                    DestroyInstance(pair.Value, "ExistingOpenRollback");
                }
            }

            foreach (var pair in snapshot.Entries)
            {
                if (!entries.TryGetValue(pair.Key, out var current)
                    || !ReferenceEquals(current, pair.Value.Entry)
                    || current.Instance == null)
                {
                    continue;
                }

                var saved = pair.Value;
                try
                {
                    if (saved.ViewModel != null && !ReferenceEquals(current.Instance.ViewModel, saved.ViewModel))
                    {
                        current.Instance.UpdateViewModel(saved.ViewModel);
                    }
                    current.State = saved.State;
                    current.EnterTransitionPending = saved.EnterTransitionPending;
                    current.HiddenByCoverage = saved.HiddenByCoverage;
                    current.ExplicitOwner = saved.ExplicitOwner;
                    current.Owners.Clear();
                    current.Owners.UnionWith(saved.Owners);
                    current.Instance.View.Visible = saved.Visible;
                    current.Instance.Interactable = saved.Interactable;
                }
                catch (Exception rollbackException)
                {
                    LogFailure(current, "ExistingOpenRollback", new AggregateException(originalException, rollbackException),
                        "Continue restoring navigation snapshot");
                }
            }

            activeOrder.Clear();
            activeOrder.AddRange(snapshot.ActiveOrder.Where(entries.ContainsKey));
            history.Clear();
            history.AddRange(snapshot.History.Where(entries.ContainsKey));
            focusedHandle = snapshot.FocusedHandle.IsValid && entries.ContainsKey(snapshot.FocusedHandle)
                ? snapshot.FocusedHandle
                : ViewHandle.Invalid;

            foreach (var layer in entries.Values
                .Where(instance => CanNavigate(instance.State))
                .Select(instance => instance.Route.Descriptor.Settings.Layer)
                .Distinct())
            {
                try
                {
                    ReorderLayer(layer);
                }
                catch (Exception rollbackException)
                {
                    LogFailure(null, "ExistingOpenOrderRollback", new AggregateException(originalException, rollbackException),
                        "Navigation state restored; layer order may require refresh");
                }
            }
        }

        void Focus(NavigationEntry target)
        {
            if (!target.Route.Descriptor.Settings.TakeFocus || focusedHandle == target.Handle)
            {
                return;
            }

            if (focusedHandle.IsValid && entries.TryGetValue(focusedHandle, out var previous) && CanNavigate(previous.State))
            {
                var hide = target.Route.Descriptor.Settings.CoverageMode == CoverageMode.Hide;
                previous.Instance.Interactable = false;
                previous.HiddenByCoverage = hide;
                previous.State = ViewState.Covered;
                previous.Instance.Covered(hide);
            }

            if (target.State == ViewState.Covered)
            {
                target.Instance.Revealed(target.HiddenByCoverage);
                target.HiddenByCoverage = false;
                target.State = ViewState.Open;
                target.Instance.Interactable = !target.EnterTransitionPending;
            }
            else
            {
                target.Instance.Focus();
            }

            focusedHandle = target.Handle;
        }

        void ReleaseFocusAfterClose(NavigationEntry closing)
        {
            if (focusedHandle != closing.Handle)
            {
                return;
            }

            focusedHandle = ViewHandle.Invalid;
            for (var index = activeOrder.Count - 1; index >= 0; index--)
            {
                if (!entries.TryGetValue(activeOrder[index], out var candidate)
                    || !candidate.ExplicitOwner
                    || !candidate.Route.Descriptor.Settings.TakeFocus
                    || !CanNavigate(candidate.State))
                {
                    continue;
                }

                candidate.Instance.Revealed(candidate.HiddenByCoverage);
                candidate.HiddenByCoverage = false;
                candidate.State = ViewState.Open;
                candidate.Instance.Interactable = !candidate.EnterTransitionPending;
                focusedHandle = candidate.Handle;
                break;
            }
        }

        NavigationEntry GetFocused()
        {
            return focusedHandle.IsValid && entries.TryGetValue(focusedHandle, out var instance) ? instance : null;
        }

        NavigationEntry FindReusableActive(Route route)
        {
            if (route.Descriptor.Settings.AllowMultiple)
            {
                return null;
            }

            return entries.Values.FirstOrDefault(instance => instance.Route.Equals(route) && CanNavigate(instance.State));
        }

        NavigationEntry TakeCachedInstance(Route route, OpenOrigin origin, ViewHandle owner)
        {
            var entry = cache
                .Where(item => item.Route.Equals(route))
                .OrderByDescending(item => item.LastUse)
                .FirstOrDefault();
            if (entry == null)
            {
                return null;
            }

            cache.Remove(entry);
            var instance = NewInstance(route, origin, owner);
            instance.Instance = entry.Instance;
            instance.ViewLease = entry.ViewLease;
            return instance;
        }

        static void RebindCachedViewModel(NavigationEntry instance, ObservableObject viewModel)
        {
            if (viewModel != null
                && instance.Instance != null
                && !ReferenceEquals(instance.Instance.ViewModel, viewModel))
            {
                instance.Instance.UpdateViewModel(viewModel);
            }
        }

        bool TryCacheInstance(NavigationEntry instance)
        {
            var settings = instance.Route.Descriptor.Settings;
            if (settings.CacheMode == CacheMode.None || settings.CacheCapacity <= 0)
            {
                return false;
            }

            // 缓存只转移实例与 Lease，不保留 Handle；复用时必须重新分配身份。
            entries.Remove(instance.Handle);
            activeOrder.Remove(instance.Handle);
            history.Remove(instance.Handle);
            instance.State = ViewState.Cached;
            terminalStates[instance.Handle] = ViewState.Cached;
            cache.Add(new CachedEntry
            {
                Route = instance.Route,
                Instance = instance.Instance,
                ViewLease = instance.ViewLease,
                Age = 0f,
                LastUse = ++cacheClock,
            });
            instance.Instance = null;
            instance.ViewLease = null;

            var overflow = cache
                .Where(entry => entry.Route.Equals(instance.Route))
                .OrderBy(entry => entry.LastUse)
                .Take(Math.Max(0, cache.Count(entry => entry.Route.Equals(instance.Route)) - settings.CacheCapacity))
                .ToArray();
            foreach (var entry in overflow)
            {
                cache.Remove(entry);
                DestroyCachedEntry(entry, "CacheCapacity");
            }
            return true;
        }

        static void DestroyCachedEntry(CachedEntry entry, string phase)
        {
            Exception failure = null;
            try
            {
                entry.Instance?.DestroyProjection();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                entry.ViewLease?.Dispose();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
            if (failure != null)
            {
                Log.Instance.LogError($"FUI Route={entry.Route} Phase={phase} Rollback=Cache entry removed");
                Log.Instance.LogException(failure);
            }
        }

        void AcquireOperationGate(NavigationEntry instance, OperationPhase phase)
        {
            instance.OperationGate = inputGate?.Acquire(instance.Handle, phase);
            instance.OperationGateVersion = instance.OperationVersion;
        }

        static void ReleaseOperationGate(NavigationEntry instance, int? version = null)
        {
            if (instance == null)
            {
                return;
            }
            if (version.HasValue && instance.OperationGateVersion != version.Value)
            {
                return;
            }

            var gate = instance.OperationGate;
            instance.OperationGate = null;
            instance.OperationGateVersion = 0;
            gate?.Dispose();
        }

        void ReorderLayer(Layer layer)
        {
            // activeOrder 是全局视觉序列，这里只压缩目标层的 LocalOrder，不触碰其他独立 Canvas。
            var localOrder = 0;
            foreach (var handle in activeOrder)
            {
                if (!entries.TryGetValue(handle, out var instance)
                    || instance.Route.Descriptor.Settings.Layer != layer
                    || !CanNavigate(instance.State))
                {
                    continue;
                }

                if (localOrder >= 1000)
                {
                    throw new InvalidOperationException($"UI layer {layer} exceeds the limit of 1000 active views.");
                }

                instance.Instance.Layer = (int)layer;
                instance.Instance.Order = localOrder++;
            }
        }

        void OpenDependenciesSync(
            Route parent,
            DependencyTiming timing,
            ViewHandle owner,
            HashSet<Route> routePath,
            List<ViewHandle> opened,
            OpenRequest request = null)
        {
            foreach (var dependency in parent.Descriptor.Settings.Dependencies)
            {
                if (dependency.Timing != timing)
                {
                    continue;
                }

                var dependencyOwner = owner.IsValid ? owner : ViewHandle.Invalid;
                var origin = timing == DependencyTiming.RequiredBefore
                    ? OpenOrigin.RequiredDependency
                    : OpenOrigin.AttachedDependency;
                var dependencyRoute = dependency.Route;

                var handle = OpenSync(dependencyRoute, null, null, OpenOptions.Push, origin, dependencyOwner, routePath, request);
                opened.Add(handle);
            }
        }

        static void RebindExistingViewModel(
            NavigationEntry instance,
            ObservableObject viewModel,
            OpenOptions options)
        {
            if (viewModel == null || ReferenceEquals(instance.Instance?.ViewModel, viewModel))
            {
                return;
            }

            if (options.ExistingViewModelMode != ExistingViewModelMode.Rebind)
            {
                throw new InvalidOperationException(
                    $"Route {instance.Route} is already bound to another ViewModel. Use OpenOptions.Rebind to retain the View and bind new data.");
            }

            instance.Instance.UpdateViewModel(viewModel);
        }

        void CommitExistingOpen(NavigationEntry instance, OpenOrigin origin, ViewHandle owner)
        {
            var wasExplicit = instance.ExplicitOwner;
            AddOwnership(instance, origin, owner);
            // 依赖实例被用户显式打开时升级为导航页面；已是显式页面则不重复写入历史。
            if (origin == OpenOrigin.Explicit)
            {
                if (!wasExplicit && instance.Route.Descriptor.Settings.TakeFocus && instance.Route.Descriptor.Settings.HistoryMode == HistoryMode.Stack)
                {
                    history.Add(instance.Handle);
                }
                BringToFront(instance.Handle);
                return;
            }

            // AttachedAfter 只调整视觉顺序，不能通过 BringToFront 间接获得焦点。
            if (origin == OpenOrigin.AttachedDependency)
            {
                MoveToLayerFront(instance);
            }
        }

        static void AddOwnership(NavigationEntry instance, OpenOrigin origin, ViewHandle owner)
        {
            if (origin == OpenOrigin.Explicit)
            {
                instance.ExplicitOwner = true;
            }
            if (owner.IsValid)
            {
                instance.Owners.Add(owner);
            }
        }

        void ReleaseDependencies(NavigationEntry parent)
        {
            foreach (var dependency in parent.Dependencies.ToArray())
            {
                ReleaseOwner(dependency, parent.Handle);
            }
            parent.Dependencies.Clear();
        }

        void ReleaseOwner(ViewHandle dependencyHandle, ViewHandle owner)
        {
            if (!entries.TryGetValue(dependencyHandle, out var dependency))
            {
                return;
            }

            dependency.Owners.Remove(owner);
            // 共享依赖由最后一个父页面释放；用户显式打开产生的独立所有权不受父页面关闭影响。
            if (dependency.Owners.Count == 0 && !dependency.ExplicitOwner)
            {
                Close(dependencyHandle);
            }
        }

        async ValueTask PlayTransitionSafelyAsync(
            NavigationEntry instance,
            int version,
            string phase,
            ITransition transition,
            CancellationToken cancellationToken)
        {
            try
            {
                await transition.PlayAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogFailure(instance, phase, new OperationCanceledException(cancellationToken), "State commit remains protected by the operation version");
            }
            catch (Exception exception)
            {
                LogFailure(instance, phase, exception, phase == "CloseTransition" ? "Continue closing" : "Complete opening");
            }
        }

        static void StopTransition(ITransitionProvider provider, IView view)
        {
            var transition = provider?.Get(view);
            if (transition?.IsPlaying == true)
            {
                transition.Stop();
            }
        }

        static ITransition GetCloseTransition(NavigationEntry instance)
        {
            try
            {
                return instance.Route.Descriptor.Settings.ExitTransitionProvider?.Get(instance.Instance.View);
            }
            catch (Exception exception)
            {
                LogFailure(instance, "CreateCloseTransition", exception, "Skip transition and continue closing");
                return null;
            }
        }

        static bool CanNavigate(ViewState state)
        {
            return state == ViewState.Opening
                || state == ViewState.Open
                || state == ViewState.Covered;
        }

        static bool TryEnterRoute(Route route, HashSet<Route> routePath)
        {
            return route != null && routePath.Add(route);
        }

        bool IsCurrent(NavigationEntry instance, int version)
        {
            // Handle、对象引用和操作版本必须同时匹配，才能提交异步结果。
            return entries.TryGetValue(instance.Handle, out var current)
                && ReferenceEquals(current, instance)
                && instance.OperationVersion == version;
        }

        void EnsureCurrent(NavigationEntry instance, int version, string phase)
        {
            if (!IsCurrent(instance, version))
            {
                throw new OperationCanceledException($"The {phase} result for Route {instance.Route}, Handle {instance.Handle}, is stale.");
            }
        }

        void AssertReady()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Navigator has not been initialized.");
            }
            executionContext.AssertMainThread();
            if (shuttingDown)
            {
                throw new InvalidOperationException("Navigator is shutting down.");
            }
        }

        static void LogFailure(NavigationEntry instance, string phase, Exception exception, string rollback)
        {
            Log.Instance.LogError($"FUI Route={instance?.Route} Handle={instance?.Handle.ToString() ?? "Invalid"} Phase={phase} Rollback={rollback}");
            Log.Instance.LogException(exception);
        }
    }
}
