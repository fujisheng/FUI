using System;
using FUI.Binding;
using FUI.Infrastructure;

namespace FUI.Presentation
{
    /// <summary>
    /// 嵌套视图投影的受控生命周期句柄，不公开底层 View、Element 或绑定上下文。
    /// </summary>
    public sealed class ProjectionHandle<TViewModel> : IDisposable
        where TViewModel : ObservableObject
    {
        ViewInstance instance;
        readonly Action<ViewInstance> release;

        internal ProjectionHandle(ViewInstance instance, Action<ViewInstance> release)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }

        internal ViewInstance Instance => instance;

        public TViewModel ViewModel => instance?.ViewModel as TViewModel;

        public bool IsAlive => instance != null
            && instance.State.HasFlag(ViewInstanceState.Alive);

        public void Activate(object parameter = null)
        {
            instance?.Enable(parameter);
        }

        public void Deactivate()
        {
            instance?.Disable();
        }

        public void Dispose()
        {
            var current = instance;
            instance = null;
            if (current == null)
            {
                return;
            }

            release(current);
        }
    }
}
