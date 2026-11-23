using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 定义列表数据、选择状态和安全 Item ViewModel 查询的公共契约。
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ListElement : RectTransformElement, IContainerElement, IListView
    {
        bool isUpdatingSelection;

        /// <summary>
        /// 当前列表数据；设置为 null 等价于 Reset。
        /// </summary>
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource { get; private set; }

        /// <summary>
        /// 当前选中索引。
        /// </summary>
        public BindableProperty<int> SelectedIndex { get; private set; }

        /// <summary>
        /// 当前选中项。
        /// </summary>
        public BindableProperty<ObservableObject> SelectedItem { get; private set; }

        /// <summary>
        /// 所有子节点实例
        /// </summary>
        private protected List<ViewInstance> ItemInstances { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ItemsSource = CreateProperty<IReadOnlyObservableList<ObservableObject>>(null, (oldValue, newValue) =>
            {
                if (newValue == null)
                {
                    OnReset();
                    ResetSelection();
                    RefreshSelectionState();
                    return;
                }

                OnUpdate();
                RefreshSelectionState();
            });

            SelectedIndex = CreateProperty(-1, OnSelectedIndexChanged);
            SelectedItem = CreateProperty<ObservableObject>(null, OnSelectedItemChanged);

            ItemInstances = new List<ViewInstance>();
            TrackCleanup(ReleaseItemInstances);
        }

        void IListView.OnAdd(object sender, int? index, object item)
        {
            // 如果index为null或者item为null，表示整个列表都更新了
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnAdd(index.Value, item as ObservableObject);
            }

            RefreshSelectionState();
        }

        void IListView.OnRemove(object sender, int? index, object item)
        {
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnRemove(index.Value, item as ObservableObject);
            }

            RefreshSelectionState();
        }

        void IListView.OnReplace(object sender, int? index, object oldItem, object newItem)
        {
            if (index == null || oldItem == null || newItem == null)
            {
                OnUpdate();
            }
            else
            {
                OnReplace(index.Value, oldItem as ObservableObject, newItem as ObservableObject);
            }

            RefreshSelectionState();
        }

        void IListView.OnUpdate(object sender)
        {
            if (ItemsSource.Value == null)
            {
                OnReset();
            }
            else
            {
                OnUpdate();
            }
            RefreshSelectionState();
        }

        /// <summary>
        /// 获取当前绑定到指定索引的 Item ViewModel，不暴露底层 View 或 ViewInstance。
        /// </summary>
        public virtual bool TryGetBoundItemViewModel(int index, out ObservableObject itemViewModel)
        {
            itemViewModel = null;
            var list = ItemsSource?.Value;
            if (list == null || index < 0 || index >= list.Count || index >= ItemInstances.Count)
            {
                return false;
            }

            var instance = ItemInstances[index];
            if (instance == null)
            {
                return false;
            }

            itemViewModel = instance.ViewModel;
            return itemViewModel != null;
        }

        /// <summary>供渲染扩展查询已实现的行。离屏查询不创建实例。</summary>
        public bool TryGetRealizedItem(int index, out IView view)
        {
            view = null;
            var source = ItemsSource?.Value;
            if (source == null || index < 0 || index >= source.Count)
            {
                return false;
            }
            var slot = GetRealizedSlot(index);
            if (slot < 0 || slot >= ItemInstances.Count)
            {
                return false;
            }
            var instance = ItemInstances[slot];
            if (instance == null || (instance.State & ViewInstanceState.Enabled) == 0 ||
                !ReferenceEquals(instance.ViewModel, source[index]))
            {
                return false;
            }
            view = instance.View;
            return view != null;
        }

        protected virtual int GetRealizedSlot(int index) => index;

        /// <summary>
        /// 当添加一个元素的时候
        /// </summary>
        /// <param name="index">添加的index</param>
        /// <param name="item">添加的item</param>
        protected virtual void OnAdd(int index, ObservableObject item) { }

        /// <summary>
        /// 当移除一个元素的时候
        /// </summary>
        /// <param name="index">移除的index</param>
        /// <param name="item">移除的项</param>
        protected virtual void OnRemove(int index, ObservableObject item) { }

        /// <summary>
        /// 当替换的时候
        /// </summary>
        /// <param name="index">替换的index</param>
        /// <param name="oldItem">替换前的item</param>
        /// <param name="newItem">替换后的item</param>
        protected virtual void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem) { }


        /// <summary>
        /// 当全量更新的时候
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>在 ItemsSource 被清空时隐藏或重置全部可视项目。</summary>
        protected abstract void OnReset();

        /// <summary>
        /// 创建一个ItemView
        /// </summary>
        /// <param name="itemViewModel">这个Item对应的视图模型</param>
        /// <param name="itemObject">这个Item对应的GameObject</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private protected ViewInstance CreateItemInstance(
            ObservableObject itemViewModel,
            GameObject itemObject,
            int? insertIndex = null)
        {
            var itemView = View.Create(this.AssetLoader, itemObject);
            var instance = ViewInstance.Create(itemView, itemViewModel);
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= ItemInstances.Count)
            {
                ItemInstances.Insert(insertIndex.Value, instance);
            }
            else
            {
                ItemInstances.Add(instance);
            }
            return instance;
        }

        private protected void ReleaseItemInstance(ViewInstance itemInstance)
        {
            if (itemInstance == null)
            {
                return;
            }

            var itemView = itemInstance.View as View;
            itemInstance.DestroyProjection();
            itemView?.Release();
            ItemInstances.Remove(itemInstance);
        }

        void ReleaseItemInstances()
        {
            for (var index = ItemInstances.Count - 1; index >= 0; index--)
            {
                var itemInstance = ItemInstances[index];
                var itemView = itemInstance.View as View;
                itemInstance.DestroyProjection();
                itemView?.Release();
            }
            ItemInstances.Clear();
        }

        void ResetSelection()
        {
            isUpdatingSelection = true;
            try
            {
                SelectedIndex.SetValue(-1);
                SelectedItem.SetValue(null);
            }
            finally
            {
                isUpdatingSelection = false;
            }
        }

        void OnSelectedIndexChanged(int oldValue, int newValue)
        {
            if (isUpdatingSelection)
            {
                return;
            }

            isUpdatingSelection = true;
            try
            {
                SelectedItem.SetValue(ResolveItemByIndex(newValue));
            }
            finally
            {
                isUpdatingSelection = false;
            }
        }

        void OnSelectedItemChanged(ObservableObject oldValue, ObservableObject newValue)
        {
            if (isUpdatingSelection)
            {
                return;
            }

            isUpdatingSelection = true;
            try
            {
                SelectedIndex.SetValue(ResolveIndexByItem(newValue));
            }
            finally
            {
                isUpdatingSelection = false;
            }
        }

        void RefreshSelectionState()
        {
            if (isUpdatingSelection)
            {
                return;
            }

            isUpdatingSelection = true;
            try
            {
                var selectedIndex = SelectedIndex.Value;
                var selectedItem = ResolveItemByIndex(selectedIndex);
                if (selectedItem == null && SelectedItem.Value != null)
                {
                    selectedIndex = ResolveIndexByItem(SelectedItem.Value);
                    SelectedIndex.SetValue(selectedIndex);
                    selectedItem = ResolveItemByIndex(selectedIndex);
                }

                if (selectedItem == null && selectedIndex >= 0)
                {
                    SelectedIndex.SetValue(-1);
                }

                SelectedItem.SetValue(selectedItem);
            }
            finally
            {
                isUpdatingSelection = false;
            }
        }

        ObservableObject ResolveItemByIndex(int index)
        {
            var list = ItemsSource?.Value;
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        int ResolveIndexByItem(ObservableObject item)
        {
            var list = ItemsSource?.Value;
            if (list == null || item == null)
            {
                return -1;
            }

            for (var index = 0; index < list.Count; index++)
            {
                if (ReferenceEquals(list[index], item))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
