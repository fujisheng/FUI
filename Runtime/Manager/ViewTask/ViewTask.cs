using System;
using System.Threading;

namespace FUI.Manager
{
    /// <summary>
    /// 视图任务基类
    /// </summary>
    abstract class ViewTask
    {
        /// <summary>
        /// 视图名字
        /// </summary>
        internal readonly string viewName;

        /// <summary>
        /// 任务结果对应的UI实体
        /// </summary>
        protected UIEntity result;

        /// <summary>
        /// 是否已完成创建或获取（非过渡完成）
        /// </summary>
        protected bool isComplated;

        /// <summary>
        /// 完成后的回调（过渡完成时触发）
        /// </summary>
        protected Action OnComplated;

        /// <summary>
        /// 执行任务
        /// </summary>
        internal abstract void Execute();

        /// <summary>
        /// 取消任务
        /// </summary>
        internal abstract void Cancel();

        /// <summary>
        /// 尝试完成任务（若过渡动画未完成则返回 false）
        /// </summary>
        /// <returns></returns>
        internal abstract bool TryComplete();

        /// <summary>
        /// 添加完成回调
        /// </summary>
        /// <param name="callback">完成时回调</param>
        internal void AddCompleteCallback(Action callback)
        {
            if (this.OnComplated != null)
            {
                return;
            }

            OnComplated += callback;
        }

        /// <summary>
        /// 清理完成时的回调
        /// </summary>
        internal void ClearComplateCallback()
        {
            OnComplated = null;
        }

        internal ViewTask(string viewName)
        {
            this.viewName = viewName;
            isComplated = false;
        }

        /// <summary>
        /// 开始播放过渡动画并等待完成
        /// </summary>
        /// <param name="transition">过渡动画</param>
        /// <param name="started">是否已启动标记</param>
        /// <param name="cts">取消令牌源</param>
        /// <param name="onFinally">动画结束后的处理</param>
        /// <param name="onNoTransition">没有动画时的处理</param>
        /// <returns>如果没有过渡动画则返回 true 表示已完成，否则返回 false 等待完成</returns>
        protected bool BeginTransition(ITransition transition, ref bool started, ref CancellationTokenSource cts, Action onFinally, Action onNoTransition = null)
        {
            if (transition == null)
            {
                onNoTransition?.Invoke();
                return true;
            }

            if (!started)
            {
                started = true;
                cts = new CancellationTokenSource();
                var ct = cts.Token;
                PlayTransitionAsync(transition, ct, onFinally);
            }

            return false;
        }

        /// <summary>
        /// 取消过渡动画播放
        /// </summary>
        /// <param name="transition">过渡动画</param>
        /// <param name="cts">取消令牌源</param>
        protected void CancelTransition(ITransition transition, ref CancellationTokenSource cts)
        {
            cts?.Cancel();
            if (transition != null && transition.IsPlaying)
            {
                transition.Stop();
            }
        }

        /// <summary>
        /// 异步播放过渡动画
        /// </summary>
        /// <param name="transition">过渡动画</param>
        /// <param name="ct">取消标记</param>
        /// <param name="onFinally">动画结束后的处理</param>
        protected async void PlayTransitionAsync(ITransition transition, CancellationToken ct, Action onFinally)
        {
            try
            {
                await transition.PlayAsync(ct);
            }
            catch { }

            finally
            {
                try
                {
                    onFinally?.Invoke();
                }
                catch { }

                try
                {
                    OnComplated?.Invoke();
                }
                catch { }
            }
        }
    }
}