using System;
using System.Collections.Generic;
using FUI.Diagnostics;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 按注册顺序的逆序释放 Element 持有的资源，确保单项失败不会中断剩余清理。
    /// </summary>
    internal sealed class ElementLifetime : IDisposable
    {
        readonly List<Action> cleanupActions = new List<Action>();
        bool disposed;

        internal T Track<T>(T resource) where T : IDisposable
        {
            if (resource == null)
            {
                return resource;
            }

            TrackCleanup(resource.Dispose);
            return resource;
        }

        internal void TrackCleanup(Action cleanup)
        {
            if (cleanup == null)
            {
                return;
            }

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ElementLifetime));
            }

            cleanupActions.Add(cleanup);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (var index = cleanupActions.Count - 1; index >= 0; index--)
            {
                try
                {
                    cleanupActions[index]?.Invoke();
                }
                catch (Exception exception)
                {
                    Log.Instance.LogError($"Element cleanup failed: {exception.Message}");
                    Log.Instance.LogException(exception);
                }
            }

            cleanupActions.Clear();
        }
    }
}
