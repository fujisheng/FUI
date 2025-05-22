using System;

namespace FUI.Manager
{
    /// <summary>
    /// 界面任务
    /// </summary>
    abstract class ViewTask
    {
        /// <summary>
        /// 界面名字
        /// </summary>
        internal readonly string viewName;

        /// <summary>
        /// 结果
        /// </summary>
        protected UIEntity result;

        /// <summary>
        /// 是否完成
        /// </summary>
        protected bool isComplated;

        /// <summary>
        /// 完成的回调
        /// </summary>
        protected Action OnComplated;

        /// <summary>
        /// 执行
        /// </summary>
        internal abstract void Execute();

        /// <summary>
        /// 取消
        /// </summary>
        internal abstract void Cancel();

        /// <summary>
        /// 尝试完成这个任务
        /// </summary>
        /// <returns></returns>
        internal abstract bool TryComplete();

        /// <summary>
        /// 添加完成的回调
        /// </summary>
        /// <param name="callback"></param>
        internal void AddCompleteCallback(Action callback)
        {
            if(this.OnComplated != null)
            {
                return;
            }

            OnComplated += callback;
        }

        /// <summary>
        /// 清空完成时的回调
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
    }
}