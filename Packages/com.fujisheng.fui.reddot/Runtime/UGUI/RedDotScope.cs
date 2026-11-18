using System;
using FUI.RedDot;
using UnityEngine;

namespace FUI.RedDot.UGUI
{
    /// <summary>
    /// 为一棵 UI Transform 子树提供局部只读红点上下文。Scope 不 Tick、也不释放 Reader。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RedDotScope : MonoBehaviour
    {
        internal event Action ReaderChanged;
        bool destroying;

        internal bool IsAvailable => !destroying;

        /// <summary>当前局部 Reader；null 表示显式隔离且尚未配置。</summary>
        public IRedDotReader Reader { get; private set; }

        /// <summary>设置局部 Reader，并通知已启用的后代重新订阅。</summary>
        public void Configure(IRedDotReader reader)
        {
            if (destroying) throw new ObjectDisposedException(nameof(RedDotScope));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (ReferenceEquals(Reader, reader)) return;
            Reader = reader;
            NotifyReaderChanged();
        }

        /// <summary>清除局部 Reader；后代不会回退到默认 Runtime。</summary>
        public void Clear()
        {
            if (destroying) throw new ObjectDisposedException(nameof(RedDotScope));
            if (Reader == null) return;
            Reader = null;
            NotifyReaderChanged();
        }

        void OnDestroy()
        {
            // Unity 销毁回调期间组件仍可能被 GetComponent 找到，先标记不可用再让后代重新解析。
            destroying = true;
            Reader = null;
            NotifyReaderChanged();
            ReaderChanged = null;
        }

        void NotifyReaderChanged()
        {
            var handlers = ReaderChanged;
            if (handlers == null) return;
            var invocationList = handlers.GetInvocationList();
            for (var index = 0; index < invocationList.Length; index++)
            {
                try
                {
                    ((Action)invocationList[index])();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }
    }
}
