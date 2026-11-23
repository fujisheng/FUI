using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;
using UnityEngine;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 固定作者槽位：保留预置节点和几何，每项拥有独立绑定上下文。
    /// </summary>
    public sealed class SlotListElement : ElementAdapter, IListView
    {
        readonly List<ViewScope> slots = new List<ViewScope>();
        sealed class Projection
        {
            internal ObservableObject Model;
            internal View View;
            internal ViewInstance Instance;
        }
        readonly Projection[] projections;
        readonly List<ObservableObject> desired = new List<ObservableObject>();
        readonly Action applyRebuild;
        readonly Func<ViewScope, int, View> viewFactory;
        bool rebuilding;
        bool rebuildAgain;
        bool rebuildQueued;
        bool overflowReported;
        bool disposed;

        /// <summary>
        /// 显式作者槽位和条目工厂，不依赖 HTML 中的框架属性。
        /// </summary>
        public SlotListElement(global::URUI.Elements.Element element, string name,
            IReadOnlyList<global::URUI.Elements.Element> authoredSlots, Func<ViewScope, int, View> viewFactory)
            : this(element, name, viewFactory ?? throw new ArgumentNullException(nameof(viewFactory)), ValidateSlots(element, authoredSlots)) { }

        SlotListElement(global::URUI.Elements.Element element, string name,
            Func<ViewScope, int, View> viewFactory, IReadOnlyList<global::URUI.Elements.Element> authoredSlots) : base(element, name)
        {
            this.viewFactory = viewFactory;
            applyRebuild = ApplyRebuild;
            foreach (var node in authoredSlots)
            {
                slots.Add(new ViewScope(node));
            }
            projections = new Projection[slots.Count];
            ItemsSource = new BindableProperty<IReadOnlyObservableList<ObservableObject>>(null, (_, __) => Rebuild());
            foreach (var slot in slots)
            {
                slot.SetVisible(false);
            }
        }

        static IReadOnlyList<global::URUI.Elements.Element> ValidateSlots(global::URUI.Elements.Element element, IReadOnlyList<global::URUI.Elements.Element> authoredSlots)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (authoredSlots == null)
            {
                throw new ArgumentNullException(nameof(authoredSlots));
            }
            var result = new List<global::URUI.Elements.Element>(authoredSlots.Count);
            var unique = new HashSet<global::URUI.Elements.Element>();
            foreach (var slot in authoredSlots)
            {
                if (slot == null || !ReferenceEquals(slot.Parent, element) || slot.TagName == "#text" || !unique.Add(slot))
                {
                    throw new ArgumentException("作者槽位必须是不重复的直接子节点。", nameof(authoredSlots));
                }
                result.Add(slot);
            }
            return result;
        }

        /// <summary>
        /// 作者预制的固定容量。
        /// </summary>
        public int Capacity => slots.Count;
        /// <summary>
        /// 按作者槽位索引获取当前条目视图；隐藏或未绑定的槽位没有视图。
        /// </summary>
        public IView GetItemView(int index) => index >= 0 && index < projections.Length ? projections[index]?.View : null;
        /// <summary>
        /// 按作者槽位顺序显示的数据，多余项隐藏并报告一次警告。
        /// </summary>
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource
        {
            get;
        }
        /// <summary>
        /// 响应列表新增。
        /// </summary>
        public void OnAdd(object sender, int? index, object itemData) => Rebuild();
        /// <summary>
        /// 响应列表移除。
        /// </summary>
        public void OnRemove(object sender, int? index, object item) => Rebuild();
        /// <summary>
        /// 响应列表替换。
        /// </summary>
        public void OnReplace(object sender, int? index, object oldItem, object newItem) => Rebuild();
        /// <summary>
        /// 响应列表整体更新。
        /// </summary>
        public void OnUpdate(object sender) => Rebuild();

        void Rebuild()
        {
            if (disposed)
            {
                return;
            }
            if (rebuilding)
            {
                rebuildAgain = true;
                return;
            }
            if (!rebuildQueued)
            {
                rebuildQueued = true;
                Element.Document.RunAfterEvent(applyRebuild);
            }
        }

        void ApplyRebuild()
        {
            rebuildQueued = false;
            if (disposed || Element == null || !Element.IsValid)
            {
                return;
            }
            rebuilding = true;
            try
            {
                do
                {
                    using var update = Element.Document.BeginUpdate();
                    rebuildAgain = false;
                    var values = ItemsSource.Value;
                    var count = Math.Min(values?.Count ?? 0, Capacity);
                    desired.Clear();
                    for (var index = 0; index < count; index++)
                    {
                        desired.Add(values[index]);
                    }
                    if (values != null && values.Count > Capacity && !overflowReported)
                    {
                        overflowReported = true;
                        Debug.LogWarning($"URUI fixed slots '{Name}': {values.Count} items exceed capacity {Capacity}; extra items are hidden.");
                    }
                    for (var index = 0; index < Capacity; index++)
                    {
                        if (disposed || rebuildAgain)
                        {
                            break;
                        }
                        var previous = projections[index];
                        if (index < desired.Count && previous?.Instance != null)
                        {
                            var nextModel = desired[index];
                            if (ReferenceEquals(previous.Model, nextModel))
                            {
                                continue;
                            }
                            if (previous.Model != null && nextModel != null && previous.Model.GetType() == nextModel.GetType())
                            {
                                previous.Model = nextModel;
                                previous.Instance.UpdateViewModel(nextModel);
                                continue;
                            }
                        }
                        if (previous != null)
                        {
                            ReleaseProjection(index);
                        }
                        if (disposed || rebuildAgain)
                        {
                            break;
                        }
                        if (index >= desired.Count)
                        {
                            continue;
                        }
                        var view = viewFactory(slots[index], index);
                        if (view == null)
                        {
                            throw new InvalidOperationException("作者槽位工厂没有返回条目视图。");
                        }
                        view.RetainRootForOwner(slots[index].Root, preserveOwnerPresentation: false);
                        if (disposed || rebuildAgain)
                        {
                            view.Destroy();
                            break;
                        }
                        var projection = new Projection
                        {
                            Model = desired[index],
                            View = view
                        };
                        projections[index] = projection;
                        view.PrepareProjection();
                        if (disposed || rebuildAgain)
                        {
                            break;
                        }
                        var instance = ViewInstance.Create(view, desired[index]);
                        if (instance == null)
                        {
                            throw new InvalidOperationException("No binding context for fixed slot data.");
                        }
                        if (disposed || rebuildAgain)
                        {
                            // OnCreate 尚未完成移交时可能关闭父列表；禁止重新启用已销毁的子视图。
                            instance.DestroyProjection();
                            break;
                        }
                        projection.Instance = instance;
                        instance.Enable();
                    }
                } while (rebuildAgain && !disposed);
            }
            catch (Exception failure)
            {
                try
                {
                    ReleaseProjections();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }
            finally
            {
                desired.Clear();
                rebuilding = false;
            }
        }

        void ReleaseProjections()
        {
            Exception failure = null;
            for (var index = 0; index < Capacity; index++)
            {
                try
                {
                    ReleaseProjection(index);
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        void ReleaseProjection(int index)
        {
            // 回调可销毁整个列表，先交出当前槽位所有权，避免重复释放。
            var projection = projections[index];
            projections[index] = null;
            Exception failure = null;
            if (projection != null)
            {
                try
                {
                    projection.Instance?.DestroyProjection();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
                try
                {
                    projection.View.Destroy();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }
            try
            {
                if (slots[index].Root.IsValid)
                {
                    slots[index].SetVisible(false);
                }
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// 释放所有子投影，保留作者节点的销毁权给页面。
        /// </summary>
        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            try
            {
                ReleaseProjections();
            }
            finally
            {
                ItemsSource.Dispose();
                base.Dispose();
            }
        }
    }
}
