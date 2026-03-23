using System;
using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// 界面任务队列
    /// </summary>
    class ViewTaskQueue
    {
        readonly List<ViewTask> tasks;
        bool isProcessing;

        /// <summary>
        /// 所有的任务
        /// </summary>
        internal IEnumerable<ViewTask> Tasks => tasks;

        /// <summary>
        /// 当集合发生变化时
        /// </summary>
        internal event Action OnCollectionChanged;

        /// <summary>
        /// 初始化一个队列
        /// </summary>
        internal ViewTaskQueue()
        {
            tasks = new List<ViewTask>();
            isProcessing = false;
        }

        /// <summary>
        /// 最前面的任务
        /// </summary>
        ViewTask Peek => tasks[0];

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="task">入队的任务</param>
        internal void Execute(ViewTask task)
        {
            tasks.Add(task);
            OnCollectionChanged?.Invoke();
            task.Execute();
            ProcessPeekTask();
        }

        /// <summary>
        /// 处理下一个任务
        /// </summary>
        void ProcessPeekTask()
        {
            if (isProcessing || tasks.Count == 0)
            {
                return;
            }

            isProcessing = true;

            if (!Peek.TryComplete())
            {
                Peek.AddCompleteCallback(OnTaskCompleted);
                return;
            }

            OnTaskCompleted();
        }

        /// <summary>
        /// 任务完成时的回调
        /// </summary>
        void OnTaskCompleted()
        {
            var task = tasks[0];
            task.ClearComplateCallback();
            tasks.RemoveAt(0);
            
            OnCollectionChanged?.Invoke();
            isProcessing = false;
            ProcessPeekTask();
        }

        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <param name="task"></param>
        internal void Remove(ViewTask task)
        {
            tasks.Remove(task);
            task.ClearComplateCallback();
            OnCollectionChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        internal void Clear()
        {
            tasks.Clear();
            foreach (var task in tasks)
            {
                task.ClearComplateCallback();
            }
            OnCollectionChanged?.Invoke();
        }

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => tasks.Count;
    }
}