using System;
using System.Threading;
using System.Threading.Tasks;

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
            // 这里只负责线程切换，不捕获或吞掉调用方后续状态提交的异常。
            var completion = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            synchronizationContext.Post(_ =>
            {
                registration.Dispose();
                completion.TrySetResult(true);
            }, null);
            return new ValueTask(completion.Task);
        }
    }
}
