using FUI.Binding;
using FUI.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.Navigation
{
    /// <summary>并行准备页面资源，按异步调用顺序在主线程提交呈现。</summary>
    public sealed partial class Navigator
    {
        /// <summary>打开请求的资源准备、提交和终止状态。</summary>
        enum OpenRequestState { Preparing, Ready, Committing, Finished }

        /// <summary>持有一次打开请求的参数、预热资源及独立取消边界。</summary>
        sealed class OpenRequest
        {
            public int Generation;
            public Route Route;
            public ObservableObject ViewModel;
            public object Param;
            public OpenOptions Options;
            public IViewProvider Provider;
            public OpenRequestState State;
            public bool PreparationFinished;
            public CancellationTokenSource Cancellation;
            public CancellationToken Token;
            public CancellationTokenRegistration Registration;
            public LinkedListNode<OpenRequest> QueueNode;
            public readonly Dictionary<Route, Queue<IPreloadLease>> Resources = new Dictionary<Route, Queue<IPreloadLease>>();
            public readonly TaskCompletionSource<ViewHandle> Completion = new TaskCompletionSource<ViewHandle>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        readonly LinkedList<OpenRequest> openRequests = new LinkedList<OpenRequest>();
        bool drainingOpens;
        int openGeneration;

        ValueTask<ViewHandle> EnqueueOpen(Route route, ObservableObject viewModel, object param, OpenOptions options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var request = new OpenRequest
            {
                Generation = openGeneration,
                Route = route,
                ViewModel = viewModel,
                Param = param,
                Options = options,
                Provider = viewProvider,
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token),
            };
            request.Token = request.Cancellation.Token;
            // 必须先占据调用顺序；同步完成的预热也不能越过之前的请求。
            request.QueueNode = openRequests.AddLast(request);
            request.Registration = request.Token.Register(() => { _ = CancelOpenRequestAsync(request); });
            if (request.State == OpenRequestState.Finished)
            {
                request.Registration.Dispose();
            }
            _ = PrepareOpenRequestAsync(request);
            return new ValueTask<ViewHandle>(request.Completion.Task);
        }

        async Task PrepareOpenRequestAsync(OpenRequest request)
        {
            try
            {
                var routes = new List<Route>();
                CollectResourceRoutes(request.Route, new HashSet<Route>(), new HashSet<Route>(), routes);
                // 每项准备立即启动，只有提交阶段才串行。
                var loads = routes.Select(route => PrepareOpenResourceAsync(request, route)).ToArray();
                await Task.WhenAll(loads);
                await executionContext.SwitchToMainThreadAsync();
                if (request.State != OpenRequestState.Finished)
                {
                    request.Token.ThrowIfCancellationRequested();
                    request.State = OpenRequestState.Ready;
                    DrainOpenRequests();
                }
            }
            catch (Exception exception)
            {
                await executionContext.SwitchToMainThreadAsync();
                FailOpenRequest(request, exception);
            }
            finally
            {
                request.PreparationFinished = true;
                if (request.State == OpenRequestState.Finished)
                {
                    request.Cancellation.Dispose();
                }
            }
        }

        static void CollectResourceRoutes(Route route, HashSet<Route> path, HashSet<Route> visited, List<Route> routes)
        {
            if (!TryEnterRoute(route, path))
            {
                throw new InvalidOperationException($"A dynamic route dependency cycle was detected at {route}.");
            }
            // 普通依赖共享一次资源准备；多实例节点需要独立的可转移 Lease。
            if (route.Settings.AllowMultiple || visited.Add(route))
            {
                routes.Add(route);
                foreach (var dependency in route.Settings.Dependencies)
                {
                    CollectResourceRoutes(dependency.Route, path, visited, routes);
                }
            }
            path.Remove(route);
        }

        async Task PrepareOpenResourceAsync(OpenRequest request, Route route)
        {
            IPreloadLease lease = null;
            try
            {
                request.Token.ThrowIfCancellationRequested();
                lease = TakePreload(route) ?? await request.Provider.PreloadAsync(route.Descriptor, request.Token);
                await executionContext.SwitchToMainThreadAsync();
                request.Token.ThrowIfCancellationRequested();
                if (lease == null || !lease.Route.Equals(route.Descriptor))
                {
                    throw new InvalidOperationException($"The provider could not prepare Route {route}.");
                }
                if (request.State == OpenRequestState.Finished)
                {
                    return;
                }
                if (!request.Resources.TryGetValue(route, out var resources))
                {
                    resources = new Queue<IPreloadLease>();
                    request.Resources.Add(route, resources);
                }
                resources.Enqueue(lease);
                lease = null;
            }
            catch (Exception exception)
            {
                await executionContext.SwitchToMainThreadAsync();
                FailOpenRequest(request, exception);
            }
            finally
            {
                DisposePreparedResource(lease);
            }
        }

        async Task CancelOpenRequestAsync(OpenRequest request)
        {
            try
            {
                await executionContext.SwitchToMainThreadAsync();
                FailOpenRequest(request, new OperationCanceledException(request.Token));
            }
            catch (Exception exception)
            {
                // 取消回调不能产生无人观察的异常。
                LogFailure(null, "CancelQueuedOpen", exception, "Cancellation dispatch failed");
            }
        }

        void DrainOpenRequests()
        {
            if (drainingOpens || shuttingDown)
            {
                return;
            }
            drainingOpens = true;
            try
            {
                while (openRequests.Count > 0)
                {
                    var request = openRequests.First.Value;
                    if (request.Token.IsCancellationRequested)
                    {
                        FailOpenRequest(request, new OperationCanceledException(request.Token));
                        continue;
                    }
                    if (request.State != OpenRequestState.Ready)
                    {
                        break;
                    }
                    request.State = OpenRequestState.Committing;
                    try
                    {
                        var handle = OpenSync(request.Route, request.ViewModel, request.Param, request.Options,
                            OpenOrigin.Explicit, ViewHandle.Invalid, new HashSet<Route>(), request);
                        FinishOpenRequest(request);
                        request.Completion.TrySetResult(handle);
                    }
                    catch (Exception exception)
                    {
                        // 提交是主线程同步事务；异常已由 OpenSync 释放页面和依赖。
                        request.State = OpenRequestState.Ready;
                        FailOpenRequest(request, exception);
                    }
                }
            }
            finally
            {
                drainingOpens = false;
            }
        }

        void FailOpenRequest(OpenRequest request, Exception exception)
        {
            if (request.State == OpenRequestState.Finished || request.State == OpenRequestState.Committing)
            {
                return;
            }
            FinishOpenRequest(request);
            if (exception is OperationCanceledException)
            {
                request.Completion.TrySetCanceled(request.Token);
            }
            else
            {
                request.Completion.TrySetException(exception);
            }
            DrainOpenRequests();
        }

        void FinishOpenRequest(OpenRequest request)
        {
            request.State = OpenRequestState.Finished;
            if (request.QueueNode?.List != null)
            {
                openRequests.Remove(request.QueueNode);
            }
            request.QueueNode = null;
            request.Registration.Dispose();
            // 取消尚未结束的同请求加载；不可取消的迟到 Lease 由加载任务释放。
            try
            {
                request.Cancellation.Cancel();
            }
            catch (Exception exception)
            {
                LogFailure(null, "CancelPreparedResources", exception, "Continue releasing resources");
            }
            foreach (var resources in request.Resources.Values)
            {
                while (resources.Count > 0)
                {
                    DisposePreparedResource(resources.Dequeue());
                }
            }
            request.Resources.Clear();
            if (request.PreparationFinished)
            {
                request.Cancellation.Dispose();
            }
        }

        static IPreloadLease TakePreparedResource(OpenRequest request, Route route)
        {
            if (!request.Resources.TryGetValue(route, out var resources) || resources.Count == 0)
            {
                throw new InvalidOperationException($"Route {route} has no prepared resource; asynchronous presentation must not perform synchronous loading.");
            }
            return resources.Dequeue();
        }

        static void DisposePreparedResource(IPreloadLease lease)
        {
            try
            {
                lease?.Dispose();
            }
            catch (Exception exception)
            {
                LogFailure(null, "DisposePreparedResource", exception, "Continue releasing resources");
            }
        }

        void EnsureOpenRequestCurrent(OpenRequest request)
        {
            if (request == null)
            {
                return;
            }
            request.Token.ThrowIfCancellationRequested();
            if (request.Generation != openGeneration)
            {
                throw new OperationCanceledException("The Navigator was shut down during presentation.");
            }
        }

        void CancelPendingOpens()
        {
            openGeneration++;
            foreach (var request in openRequests.ToArray())
            {
                FailOpenRequest(request, new OperationCanceledException(request.Token));
            }
        }
    }
}
