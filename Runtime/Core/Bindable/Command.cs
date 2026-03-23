using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    /// <summary>
    /// 命令模板
    /// </summary>
    /// <typeparam name="T">命令签名类型</typeparam>
    public abstract class CommandTemplate<T> where T : Delegate
    {
        protected readonly List<T> invocationList;

        protected CommandTemplate()
        {
            this.invocationList = new List<T>();
        }

        /// <summary>
        /// 添加一个回调
        /// </summary>
        /// <param name="invocation">回调</param>
        public void AddListener(T invocation)
        {
            if (invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Add(invocation);
        }

        /// <summary>
        /// 移除一个回调
        /// </summary>
        /// <param name="invocation">移除回调</param>
        public void RemoveListener(T invocation)
        {
            if (!invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Remove(invocation);
        }

        /// <summary>
        /// 清空执行列表
        /// </summary>
        public void ClearListeners()
        {
            invocationList.Clear();
        }

        /// <summary>
        /// 安全执行  因为有可能在委托执行过程中修改集合
        /// </summary>
        /// <param name="action">要执行的内容</param>
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