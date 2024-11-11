using System;
using System.Collections.Generic;

namespace FUI.Bindable
{
    public abstract class CommandArgs
    {
        /// <summary>
        /// 这个命令的发送者
        /// </summary>
        public object Sender { get; }
        private CommandArgs() { }

        /// <summary>
        /// 构造一个命令参数
        /// </summary>
        /// <param name="sender">命令发起者</param>
        protected CommandArgs(object sender)
        {
            this.Sender = sender;
        }
    }

    /// <summary>
    /// 定义一个View命令
    /// </summary>
    /// <typeparam name="TArgs">参数类型</typeparam>
    public class Command<TArgs> where TArgs : CommandArgs
    {
        
        readonly List<Action<TArgs>> invocationList;

        /// <summary>
        /// 执行列表
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
        /// 添加一个回调
        /// </summary>
        /// <param name="invocation">回调</param>
        public void AddListener(Action<TArgs> invocation)
        {
            if(invocationList.Contains(invocation))
            {
                return;
            }

            invocationList.Add(invocation);
        }

        /// <summary>
        /// 移除一个回调
        /// </summary>
        /// <param name="invocation">移除回调</param>
        public void RemoveListener(Action<TArgs> invocation)
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
        public void ClearListener()
        {
            invocationList.Clear();
        }

        /// <summary>
        /// 执行这个command
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