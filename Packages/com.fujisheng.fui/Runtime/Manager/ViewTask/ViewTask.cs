using System;

namespace FUI.Manager
{
    /// <summary>
    /// ��������
    /// </summary>
    abstract class ViewTask
    {
        /// <summary>
        /// ��������
        /// </summary>
        internal readonly string viewName;

        /// <summary>
        /// ���
        /// </summary>
        protected UIEntity result;

        /// <summary>
        /// �Ƿ����
        /// </summary>
        protected bool isComplated;

        /// <summary>
        /// ��ɵĻص�
        /// </summary>
        protected Action OnComplated;

        /// <summary>
        /// ִ��
        /// </summary>
        internal abstract void Execute();

        /// <summary>
        /// ȡ��
        /// </summary>
        internal abstract void Cancel();

        /// <summary>
        /// ��������������
        /// </summary>
        /// <returns></returns>
        internal abstract bool TryComplete();

        /// <summary>
        /// �����ɵĻص�
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
        /// ������ʱ�Ļص�
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