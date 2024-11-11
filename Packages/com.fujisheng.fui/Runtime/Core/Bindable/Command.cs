using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    public abstract class CommandArgs
    {
        /// <summary>
        /// �������ķ�����
        /// </summary>
        public object Sender { get; }
        private CommandArgs() { }

        /// <summary>
        /// ����һ���������
        /// </summary>
        /// <param name="sender">�������</param>
        protected CommandArgs(object sender)
        {
            this.Sender = sender;
        }
    }

    /// <summary>
    /// ����һ��View����
    /// </summary>
    /// <typeparam name="TArgs">��������</typeparam>
    public class Command<TArgs> where TArgs : CommandArgs
    {
        
        readonly List<Action<TArgs>> invocationList;

        /// <summary>
        /// ִ���б�
        /// </summary>
        public IReadOnlyList<Action<TArgs>> InvocationList => invocationList;

        public Command()
        {
            invocationList = new List<Action<TArgs>>();
        }

        public Command(Action<TArgs> invocation) : this()
        {
            AddListener(invocation);
        }

        /// <summary>
        /// ���һ���ص�
        /// </summary>
        /// <param name="invocation">�ص�</param>
        public void AddListener(Action<TArgs> invocation)
        {
            if(invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Add(invocation);
        }

        /// <summary>
        /// �Ƴ�һ���ص�
        /// </summary>
        /// <param name="invocation">�Ƴ��ص�</param>
        public void RemoveListener(Action<TArgs> invocation)
        {
            if (!invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Remove(invocation);
        }

        /// <summary>
        /// ���ִ���б�
        /// </summary>
        public void ClearListener()
        {
            invocationList.Clear();
        }

        /// <summary>
        /// ִ�����command
        /// </summary>
        /// <param name="args"></param>
        public void Invoke(TArgs args)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args);
            }
        }
    }
}