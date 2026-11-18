using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace FUI.Navigation
{
    /// <summary>
    /// FUI 的可测试主线程边界。
    /// </summary>
    public interface IMainThreadDispatcher
    {
        /// <summary>
        /// 当前调用是否位于初始化时捕获的 Unity 主线程。
        /// </summary>
        bool IsMainThread { get; }

        /// <summary>
        /// 断言当前位于 Unity 主线程，避免后台 I/O 直接提交 Unity 状态。
        /// </summary>
        void AssertMainThread();

        /// <summary>
        /// 将异步流程切回 Unity 主线程。
        /// </summary>
        ValueTask SwitchToMainThreadAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 在 Initialize 时捕获 Unity SynchronizationContext 与线程 ID。
    /// </summary>
    public sealed class UnityMainThreadDispatcher : IMainThreadDispatcher
    {
        readonly SynchronizationContext synchronizationContext;
        readonly int threadId;

        public bool IsMainThread => Environment.CurrentManagedThreadId == threadId;

        public UnityMainThreadDispatcher()
        {
            synchronizationContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException("FUI must be initialized on the Unity main thread, but no SynchronizationContext is available.");
            threadId = Environment.CurrentManagedThreadId;
        }

        public void AssertMainThread()
        {
            if (!IsMainThread)
            {
                throw new InvalidOperationException($"FUI Unity state can only be committed on the main thread. Main thread={threadId}, current thread={Environment.CurrentManagedThreadId}.");
            }
        }

        public ValueTask SwitchToMainThreadAsync(CancellationToken cancellationToken = default)
        {
            if (IsMainThread)
            {
                return default;
            }

            cancellationToken.ThrowIfCancellationRequested();
            // Task 在主线程完成并不保证其续体在主线程执行；直接调度 ValueTask 的续体。
            return new ValueTask(new MainThreadSwitch(synchronizationContext, threadId, cancellationToken), 0);
        }

        /// <summary>将一次等待的续体直接投递到捕获线程，不复用或池化等待状态。</summary>
        sealed class MainThreadSwitch : IValueTaskSource
        {
            readonly SynchronizationContext context;
            readonly int threadId;
            readonly CancellationToken token;
            OperationCanceledException cancellation;
            int registered;
            int completed;

            public MainThreadSwitch(SynchronizationContext context, int threadId, CancellationToken token)
            {
                this.context = context;
                this.threadId = threadId;
                this.token = token;
            }

            public ValueTaskSourceStatus GetStatus(short version)
            {
                if (Volatile.Read(ref completed) == 0)
                {
                    return ValueTaskSourceStatus.Pending;
                }
                return cancellation == null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Canceled;
            }

            public void GetResult(short version)
            {
                if (Volatile.Read(ref completed) == 0 || Environment.CurrentManagedThreadId != threadId)
                {
                    throw new InvalidOperationException("The main-thread switch must be awaited on its captured thread.");
                }
                if (cancellation != null)
                {
                    throw cancellation;
                }
            }

            public void OnCompleted(Action<object> continuation, object state, short version, ValueTaskSourceOnCompletedFlags flags)
            {
                if (Interlocked.Exchange(ref registered, 1) != 0)
                {
                    throw new InvalidOperationException("A main-thread switch can only be awaited once.");
                }
                var flow = (flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0
                    ? ExecutionContext.Capture()
                    : null;
                // 此操作明确选择目标线程，不回到调用者可能拥有的另一个 SchedulingContext。
                context.Post(_ =>
                {
                    if (token.IsCancellationRequested)
                    {
                        cancellation = new OperationCanceledException(token);
                    }
                    Volatile.Write(ref completed, 1);
                    if (flow == null)
                    {
                        continuation(state);
                    }
                    else
                    {
                        ExecutionContext.Run(flow, ignored =>
                        {
                            // Mono 的 ExecutionContext 也会携带调用方的 SynchronizationContext。
                            // 流转 AsyncLocal，但在回调内部保持本次操作选择的目标上下文。
                            var previous = SynchronizationContext.Current;
                            SynchronizationContext.SetSynchronizationContext(context);
                            try { continuation(state); }
                            finally { SynchronizationContext.SetSynchronizationContext(previous); }
                        }, null);
                    }
                }, null);
            }
        }
    }
}
