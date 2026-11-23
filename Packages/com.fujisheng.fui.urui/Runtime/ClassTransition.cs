using System;
using System.Collections.Generic;
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
        readonly int thread;
        readonly Func<Element> completionTargetResolver;
        readonly Func<IReadOnlyList<Element>> targetGroupResolver;
        AnimationPlayback playback;
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
            if (SynchronizationContext.Current == null)
            {
                throw new InvalidOperationException("过渡需要 Unity 主线程上下文。");
            }
            thread = Thread.CurrentThread.ManagedThreadId;
            targetGroupResolver = ResolveTargets;
            document.Unloaded += Unloaded;
        }

        public bool IsPlaying => playback != null && playback.IsPlaying;

        public ValueTask PlayAsync(CancellationToken cancellationToken)
        {
            CheckThread();
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ClassTransition));
            }
            cancellationToken.ThrowIfCancellationRequested();
            Stop();
            if (string.IsNullOrEmpty(prepareClassName) && completionTargetResolver != null)
            {
                var immediateTarget = ResolveSingleTarget();
                if (immediateTarget == null)
                {
                    if (root.IsValid && !string.IsNullOrEmpty(completionClassName))
                    {
                        root.AddToClassList(completionClassName);
                    }
                    return default;
                }
                playback = document.Animations.Play(root, className, AnimationGroup.FromAuthorTargets(() => new[] { immediateTarget }), cancellationToken, completionClassName);
                return new ValueTask(playback.Finished);
            }
            playback = document.Animations.Play(root, className, AnimationGroup.FromAuthorTargets(targetGroupResolver), cancellationToken, completionClassName, prepareClassName);
            return new ValueTask(playback.Finished);
        }

        Element ResolveSingleTarget()
        {
            var target = completionTargetResolver != null ? completionTargetResolver() : clock;
            if (target == null)
            {
                return null;
            }
            if (!target.IsValid || !ReferenceEquals(target.Document, document) || (!ReferenceEquals(target, clock) && !clock.Contains(target)))
            {
                throw new InvalidOperationException("作者完成目标必须位于声明范围内。");
            }
            return target;
        }

        IReadOnlyList<Element> ResolveTargets()
        {
            var target = ResolveSingleTarget();
            return target == null ? Array.Empty<Element>() : new[] { target };
        }

        public void Stop()
        {
            CheckThread();
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
