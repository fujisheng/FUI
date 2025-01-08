using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// �����������
    /// </summary>
    class ViewTaskQueue
    {
        readonly List<ViewTask> tasks;

        /// <summary>
        /// ���е�����
        /// </summary>
        internal IEnumerable<ViewTask> Tasks => tasks;

        /// <summary>
        /// ��ʼ��һ������
        /// </summary>
        internal ViewTaskQueue()
        {
            tasks = new List<ViewTask>();
        }

        /// <summary>
        /// ���
        /// </summary>
        /// <param name="task">��ӵ�����</param>
        internal void Execute(ViewTask task)
        {
            task.Execute();
            tasks.Add(task);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <returns>���ӵ�����</returns>
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
        /// ��ȡ��ǰ�������
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
        /// �Ƴ�һ������
        /// </summary>
        /// <param name="task"></param>
        internal void Remove(ViewTask task)
        {
            tasks.Remove(task);
        }

        /// <summary>
        /// �����������
        /// </summary>
        internal void Clear()
        {
            tasks.Clear();
        }

        /// <summary>
        /// ����
        /// </summary>
        internal int Count => tasks.Count;

        /// <summary>
        /// �Ƿ����ĳ������
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool Exist(string viewName)
        {
            return tasks.Exists(c => c.ViewName == viewName);
        }
    }
}