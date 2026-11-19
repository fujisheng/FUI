using System;
using System.Threading;
using System.Threading.Tasks;
using FUI.Transitions;
using URUI.Animation;
using URUI.Documents;
using URUI.Elements;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// FUI 过渡接到通用命名动画组；作者目标的所有有限轨道结束后才恢复业务输入。
    /// </summary>
    public sealed class ClassTransition : ITransition, IDisposable
    {
        readonly Element root;
        readonly Element clock;
        readonly string className;
        readonly string prepareClassName;
        readonly string completionClassName;
        readonly Document document;
        readonly SynchronizationContext context;
        readonly int thread;
        readonly Func<Element> completionTargetResolver;
        TaskCompletionSource<bool> completion;
        AnimationPlayback playback;
        CancellationTokenRegistration cancellation;
        CancellationToken token;
        int version;
        bool preparedLayout;
        bool disposed;
        public ClassTransition(Element root, string className, Element completionElement, string prepareClassName = null, string completionClassName = null, Func<Element> completionTargetResolver = null)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            clock = completionElement ?? throw new ArgumentNullException(nameof(completionElement));
            if (!ReferenceEquals(root.Document, clock.Document) || string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException("过渡根、状态和作者目标必须有效并属于同一文档。");
            }
            this.className = className;
            this.prepareClassName = prepareClassName;
            this.completionClassName = completionClassName;
            this.completionTargetResolver = completionTargetResolver;
            document = root.Document;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("过渡需要 Unity 主线程上下文。");
            thread = Thread.CurrentThread.ManagedThreadId;
            document.Unloaded += Unloaded;
        }
        public bool IsPlaying => completion != null;
        public ValueTask PlayAsync(CancellationToken cancellationToken)
        {
            CheckThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ClassTransition));
            }
            cancellationToken.ThrowIfCancellationRequested();
            Stop();
            token = cancellationToken;
            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = pending;
            var identity = ++version;
            cancellation = token.Register(() =>
            {
                void Cancel()
                {
                    if (version == identity && completion == pending)
                    {
                        Clear();
                        pending.TrySetCanceled(cancellationToken);
                    }
                }
                if (Thread.CurrentThread.ManagedThreadId == thread)
                {
                    Cancel();
                }
                else
                {
                    context.Post(_ => Cancel(), null);
                }
            });
            if (completion == pending)
            {
                if (string.IsNullOrEmpty(prepareClassName))
                {
                    Start();
                }
                else
                {
                    root.AddToClassList(prepareClassName);
                    document.LayoutUpdated += Begin;
                }
            }
            return new ValueTask(pending.Task);
        }
        void Begin(Document owner)
        {
            if (!preparedLayout)
            {
                preparedLayout = true;
                return;
            }
            document.LayoutUpdated -= Begin;
            if (completion == null)
            {
                return;
            }
            root.RemoveFromClassList(prepareClassName);
            Start();
        }
        void Start()
        {
            var pending = completion;
            var identity = version;
            try
            {
                var target = completionTargetResolver != null ? completionTargetResolver() : clock;
                if (target == null)
                {
                    Complete(pending, identity);
                    return;
                }
                if (!target.IsValid || !ReferenceEquals(target.Document, document) || !ReferenceEquals(target, clock) && !clock.Contains(target))
                {
                    throw new InvalidOperationException("作者完成目标必须位于声明范围内。");
                }
                playback = document.Animations.Play(root, className, AnimationGroup.FromAuthorTargets(() => new[] { target }), token);
                Observe(playback, pending, identity);
            }
            catch (Exception error)
            {
                Clear();
                pending?.TrySetException(error);
            }
        }
        async void Observe(AnimationPlayback current, TaskCompletionSource<bool> pending, int identity)
        {
            try
            {
                await current.Finished;
                if (version == identity && completion == pending)
                {
                    Complete(pending, identity);
                }
            }
            catch (OperationCanceledException)
            {
                if (version == identity && completion == pending)
                {
                    Clear();
                    pending.TrySetCanceled();
                }
            }
            catch (Exception error)
            {
                if (version == identity && completion == pending)
                {
                    Clear();
                    pending.TrySetException(error);
                }
            }
        }
        void Complete(TaskCompletionSource<bool> pending, int identity)
        {
            if (version != identity || completion != pending)
            {
                return;
            }
            Clear();
            if (root.IsValid && !string.IsNullOrEmpty(completionClassName))
            {
                root.AddToClassList(completionClassName);
            }
            pending.TrySetResult(true);
        }
        void Clear()
        {
            completion = null;
            preparedLayout = false;
            version++;
            document.LayoutUpdated -= Begin;
            var previous = playback;
            playback = null;
            previous?.Cancel();
            if (root.IsValid && !string.IsNullOrEmpty(prepareClassName))
            {
                root.RemoveFromClassList(prepareClassName);
            }
            if (root.IsValid && !string.IsNullOrEmpty(completionClassName))
            {
                root.RemoveFromClassList(completionClassName);
            }
            cancellation.Dispose();
            cancellation = default;
        }
        public void Stop()
        {
            CheckThread();
            var pending = completion;
            Clear();
            pending?.TrySetCanceled();
        }
        void Unloaded(Document owner) => Stop();
        void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != thread)
            {
                throw new InvalidOperationException("过渡只能在 Unity 主线程操作。");
            }
        }
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            CheckThread();
            disposed = true;
            Stop();
            document.Unloaded -= Unloaded;
        }
    }
}
