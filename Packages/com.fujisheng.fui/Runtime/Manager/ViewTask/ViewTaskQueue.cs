using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// 界面任务队列
    /// </summary>
    class ViewTaskQueue
    {
        readonly List<ViewTask> tasks;

        /// <summary>
        /// 所有的任务
        /// </summary>
        internal IEnumerable<ViewTask> Tasks => tasks;

        /// <summary>
        /// 初始化一个队列
        /// </summary>
        internal ViewTaskQueue()
        {
            tasks = new List<ViewTask>();
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="task">入队的任务</param>
        internal void Execute(ViewTask task)
        {
            task.Execute();
            tasks.Add(task);
        }

        /// <summary>
        /// 出队
        /// </summary>
        /// <returns>出队的任务</returns>
        internal ViewTask Complete()
        {
            if (tasks.Count == 0)
            {
                return null;
            }
            var task = tasks[0];
            tasks.RemoveAt(0);
            task.Complete();
            return task;
        }

        /// <summary>
        /// 获取最前面的任务
        /// </summary>
        /// <returns></returns>
        internal ViewTask Peek()
        {
            if (tasks.Count == 0)
            {
                return null;
            }
            return tasks[0];
        }

        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <param name="task"></param>
        internal void Remove(ViewTask task)
        {
            tasks.Remove(task);
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        internal void Clear()
        {
            tasks.Clear();
        }

        /// <summary>
        /// 计数
        /// </summary>
        internal int Count => tasks.Count;

        /// <summary>
        /// 是否存在某个任务
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool Exist(string viewName)
        {
            return tasks.Exists(c => c.ViewName == viewName);
        }
    }
}