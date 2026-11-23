using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using FUI.Binding;
using FUI.Infrastructure;

namespace FUI.Presentation
{
    /// <summary>由模板适配器持有的非虚拟化集合，条目位置由各自的绑定及作者布局决定。</summary>
    internal sealed class ProjectionCollection : IDisposable
    {
        const int MaxIdleItems = 16;

        /// <summary>记录集合条目与其拥有的投影实例。</summary>
        readonly struct Row
        {
            internal Row(ObservableObject model, ProjectionCreation creation)
            {
                Model = model;
                Creation = creation;
            }

            internal ObservableObject Model { get; }
            internal ProjectionCreation Creation { get; }
            internal ViewInstance Instance => Creation.Instance;

            internal void Release() => Creation.Release(Instance);
        }

        readonly Func<ObservableObject, ProjectionCreation> create;
        readonly Action<ViewInstance, ViewInstance> placeBefore;
        readonly Action<Action> schedule;
        readonly Func<IDisposable> beginUpdate;
        readonly Action refresh;
        readonly List<Row> rows = new();
        readonly List<Row> idle = new();
        bool pending;
        bool scheduled;
        bool refreshing;
        bool disposed;
        int? pendingIndex;
        int pendingDelta;

        internal ProjectionCollection(Func<ObservableObject, ProjectionCreation> create,
            Action<ViewInstance, ViewInstance> placeBefore = null, Action<Action> schedule = null,
            Func<IDisposable> beginUpdate = null)
        {
            this.create = create;
            this.placeBefore = placeBefore;
            this.schedule = schedule;
            this.beginUpdate = beginUpdate;
            refresh = Refresh;
            ItemsSource = new BindableProperty<IReadOnlyObservableList<ObservableObject>>(null,
                (_, _) => RequestRefresh(null, 0));
        }

        internal BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource { get; }

        internal void Notify(object source, int? index = null, int countDelta = 0)
        {
            if (ReferenceEquals(ItemsSource.Value, source))
            {
                RequestRefresh(index, countDelta);
            }
        }

        void RequestRefresh(int? index, int countDelta)
        {
            if (disposed)
            {
                return;
            }

            // 同时到达的多个通知统一从最新数据恢复顺序。
            pendingIndex = pending || refreshing ? null : index;
            pendingDelta = countDelta;
            pending = true;
            if (scheduled || refreshing)
            {
                return;
            }

            scheduled = true;
            if (schedule == null)
            {
                Refresh();
            }
            else
            {
                schedule(refresh);
            }
        }

        void Refresh()
        {
            scheduled = false;
            if (disposed)
            {
                return;
            }

            refreshing = true;
            try
            {
                while (pending && !disposed)
                {
                    using var update = beginUpdate?.Invoke();
                    pending = false;
                    var source = ItemsSource.Value;
                    if (source == null)
                    {
                        ReleaseAll();
                    }
                    else if (pendingIndex is int index && rows.Count + pendingDelta == source.Count
                        && index >= 0 && index < Math.Max(rows.Count, source.Count))
                    {
                        ApplyIndexedChange(source, index, pendingDelta);
                    }
                    else
                    {
                        Reconcile(source);
                    }
                }
            }
            catch
            {
                // 允许调用方修正数据后重试，从仍有效的实例恢复。
                pending = true;
                pendingIndex = null;
                throw;
            }
            finally
            {
                refreshing = false;
            }
        }

        void ApplyIndexedChange(IReadOnlyObservableList<ObservableObject> source, int index, int delta)
        {
            if (delta == 0 && ReferenceEquals(rows[index].Model, source[index]))
            {
                return;
            }
            if (delta <= 0)
            {
                Remove(index);
            }
            if (delta >= 0 && !disposed && !pending)
            {
                Insert(index, source[index]);
            }
        }

        void Reconcile(IReadOnlyObservableList<ObservableObject> source)
        {
            for (var index = 0; index < source.Count && !disposed && !pending; index++)
            {
                var model = source[index];
                var existing = index;
                while (existing < rows.Count && !ReferenceEquals(rows[existing].Model, model))
                {
                    existing++;
                }

                if (existing == rows.Count)
                {
                    Insert(index, model);
                }
                else if (existing != index)
                {
                    var row = rows[existing];
                    placeBefore?.Invoke(row.Instance, rows[index].Instance);
                    rows.RemoveAt(existing);
                    rows.Insert(index, row);
                }
            }
            while (rows.Count > source.Count && !disposed && !pending)
            {
                Remove(rows.Count - 1);
            }
        }

        void Insert(int index, ObservableObject model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var reused = TakeIdle(model, out var row);
            if (!reused)
            {
                row = new Row(model, create(model));
            }
            if (disposed)
            {
                row.Release();
                return;
            }

            try
            {
                var next = index < rows.Count ? rows[index].Instance : null;
                if (reused || next != null)
                {
                    placeBefore?.Invoke(row.Instance, next);
                }
                if (!ReferenceEquals(row.Model, model))
                {
                    row.Instance.UpdateViewModel(model);
                    row = new Row(model, row.Creation);
                }
                row.Instance.Enable();
                if (!disposed)
                {
                    rows.Insert(index, row);
                    return;
                }
            }
            catch (Exception failure)
            {
                try
                {
                    row.Release();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }

            row.Release();
        }

        bool TakeIdle(ObservableObject model, out Row row)
        {
            for (var index = idle.Count - 1; index >= 0; index--)
            {
                if (idle[index].Model.GetType() == model.GetType())
                {
                    row = idle[index];
                    idle.RemoveAt(index);
                    return true;
                }
            }
            row = default;
            return false;
        }

        void Remove(int index)
        {
            var row = rows[index];
            rows.RemoveAt(index);
            try
            {
                row.Instance.Disable();
            }
            catch (Exception failure)
            {
                try
                {
                    row.Release();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }

            if (disposed || idle.Count >= MaxIdleItems)
            {
                row.Release();
            }
            else
            {
                idle.Add(row);
            }
        }

        void ReleaseAll()
        {
            Exception failure = null;
            Release(rows, ref failure);
            Release(idle, ref failure);
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        static void Release(List<Row> entries, ref Exception failure)
        {
            while (entries.Count > 0)
            {
                var index = entries.Count - 1;
                var row = entries[index];
                entries.RemoveAt(index);
                try
                {
                    row.Release();
                }
                catch (Exception exception)
                {
                    failure = failure == null ? exception : new AggregateException(failure, exception);
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            ItemsSource.Dispose();
            ReleaseAll();
        }
    }
}
