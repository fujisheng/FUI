using System;

namespace Feature
{
    /// <summary>
    /// 修改一个Model的信号
    /// </summary>
    /// <typeparam name="T">Model类型</typeparam>
    public struct ModifySignal<T> : IDisposable where T : IModel
    {
        readonly T value;
        readonly Action<T> onSend;

        /// <summary>
        /// 构造一个修改信号
        /// </summary>
        /// <param name="value"></param>
        internal ModifySignal(T value, Action<T> onSend)
        {
            this.value = value;
            this.onSend = onSend;
        }

        /// <summary>
        /// 发送这个信号
        /// </summary>
        public void Send()
        {
            onSend?.Invoke(value);
        }

        void IDisposable.Dispose()
        {
            Send();
        }
    }
}
