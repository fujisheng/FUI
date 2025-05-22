using System;
using System.Collections.Generic;

namespace FUI.Manager
{
    /// <summary>
    /// �����������
    /// </summary>
    class ViewTaskQueue
    {
        readonly List<ViewTask> tasks;
        bool isProcessing;

        /// <summary>
        /// ���е�����
        /// </summary>
        internal IEnumerable<ViewTask> Tasks => tasks;

        /// <summary>
        /// �����Ϸ����仯ʱ
        /// </summary>
        internal event Action OnCollectionChanged;

        /// <summary>
        /// ��ʼ��һ������
        /// </summary>
        internal ViewTaskQueue()
        {
            tasks = new List<ViewTask>();
            isProcessing = false;
        }

        /// <summary>
        /// ��ǰ�������
        /// </summary>
        ViewTask Peek => tasks[0];

        /// <summary>
        /// ���
        /// </summary>
        /// <param name="task">��ӵ�����</param>
        internal void Execute(ViewTask task)
        {
            tasks.Add(task);
            OnCollectionChanged?.Invoke();
            task.Execute();
            ProcessPeekTask();
        }

        /// <summary>
        /// ������һ������
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
        /// �������ʱ�Ļص�
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
        /// �Ƴ�һ������
        /// </summary>
        /// <param name="task"></param>
        internal void Remove(ViewTask task)
        {
            tasks.Remove(task);
            task.ClearComplateCallback();
            OnCollectionChanged?.Invoke();
        }

        /// <summary>
        /// �����������
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
        /// ����
        /// </summary>
        internal int Count => tasks.Count;
    }
}