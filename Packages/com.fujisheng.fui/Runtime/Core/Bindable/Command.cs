using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
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
    }

    public class Command : CommandTemplate<Action>
    {
        public void Invoke()
        {
            foreach (var action in invocationList)
            {
                action?.Invoke();
            }
        }
    }

    public class Command<TArgs> : CommandTemplate<Action<TArgs>>
    {
        public void Invoke(TArgs args)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args);
            }
        }
    }

    public class Command<TArgs1, TArgs2> : CommandTemplate<Action<TArgs1, TArgs2>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2);
            }
        }
    }

    public class Command<TArgs1, TArgs2, TArgs3> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2, args3);
            }
        }
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2, args3, args4);
            }
        }
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2, args3, args4, args5);
            }
        }
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5, TArgs6 args6)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2, args3, args4, args5, args6);
            }
        }
    }

    public class Command<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7> : CommandTemplate<Action<TArgs1, TArgs2, TArgs3, TArgs4, TArgs5, TArgs6, TArgs7>>
    {
        public void Invoke(TArgs1 args1, TArgs2 args2, TArgs3 args3, TArgs4 args4, TArgs5 args5, TArgs6 args6, TArgs7 args7)
        {
            foreach (var action in invocationList)
            {
                action?.Invoke(args1, args2, args3, args4, args5, args6, args7);
            }
        }
    }
}