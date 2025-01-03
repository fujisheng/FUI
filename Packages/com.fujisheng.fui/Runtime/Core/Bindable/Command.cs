using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// ����ģ��
    /// </summary>
    /// <typeparam name="T">����ǩ������</typeparam>
    public abstract class CommandTemplate<T> where T : Delegate
    {
        protected readonly List<T> invocationList;

        protected CommandTemplate()
        {
            this.invocationList = new List<T>();
        }

        /// <summary>
        /// ���һ���ص�
        /// </summary>
        /// <param name="invocation">�ص�</param>
        public void AddListener(T invocation)
        {
            if (invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Add(invocation);
        }

        /// <summary>
        /// �Ƴ�һ���ص�
        /// </summary>
        /// <param name="invocation">�Ƴ��ص�</param>
        public void RemoveListener(T invocation)
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
        public void ClearListeners()
        {
            invocationList.Clear();
        }

        /// <summary>
        /// ��ȫִ��  ��Ϊ�п�����ί��ִ�й������޸ļ���
        /// </summary>
        /// <param name="action">Ҫִ�е�����</param>
        protected void SafeInvoke(Action<T> action)
        {
            if (invocationList.Count == 0)
            {
                return;
            }

            for (int i = invocationList.Count - 1; i >= 0; i--)
            {
                if (i >= invocationList.Count)
                {
                    continue;
                }

                action?.Invoke(invocationList[i]);
            }
        }
    }

    public class Command : CommandTemplate<Action>
    {
        public void Invoke() => SafeInvoke((item) => item.Invoke());
    }

    public class Command<TArgs> : CommandTemplate<Action<TArgs>>
    {
        public void Invoke(TArgs args) => SafeInvoke((item) => item.Invoke(args));
    }

    public class Command<TArgs1, TArgs2> : CommandTemplate<Action<TArgs1, TArgs2>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2) => SafeInvoke((item) => item.Invoke(args1, args2));
    }

    public class Command<TArgs1, TArgs2, TArgs3> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3) => SafeInvoke((item) => item.Invoke(args1, args2, args3));
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4)=> SafeInvoke((item) => item.Invoke(args1, args2, args3, args4));
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5)=> SafeInvoke((item) => item.Invoke(args1, args2, args3, args4, args5));
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5, TArgs6 args6)=> SafeInvoke((item) => item.Invoke(args1, args2, args3, args4, args5, args6));
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5, TArgs6 args6, TArgs7 args7)=> SafeInvoke((item) => item.Invoke(args1, args2, args3, args4, args5, args6, args7));
    }
}