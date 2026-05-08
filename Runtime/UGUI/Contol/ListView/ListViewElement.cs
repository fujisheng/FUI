using FUI.Bindable;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 列表视图元素基类
    /// </summary>
    public abstract class ListViewElement : UIElement, IContainerElement, IListView
    {
        bool isUpdatingSelection;

        /// <summary>
        /// 数据
        /// </summary>
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> List { get; private set; }

        /// <summary>
        /// 数据别名，供集合 binding 使用。
        /// </summary>
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> Items => List;

        /// <summary>
        /// 当前选中索引。
        /// </summary>
        public BindableProperty<int> SelectedIndex { get; private set; }

        /// <summary>
        /// 当前选中项。
        /// </summary>
        public BindableProperty<ObservableObject> SelectedItem { get; private set; }

        /// <summary>
        /// item template 的业务数据路径。
        /// </summary>
        public BindableProperty<string> ItemTemplateDataPath { get; private set; }

        /// <summary>
        /// 所有子节点实体
        /// </summary>
        protected List<UIEntity> ItemEntites { get; private set; }

        protected override void OnInitialize()
        {
            List = new BindableProperty<IReadOnlyObservableList<ObservableObject>>(null, (oldValue, newValue) =>
            {
                if (newValue == null)
                {
                    RefreshSelectionState();
                    return;
                }

                OnUpdate();
                RefreshSelectionState();
            });

            SelectedIndex = new BindableProperty<int>(-1, OnSelectedIndexChanged);
            SelectedItem = new BindableProperty<ObservableObject>(null, OnSelectedItemChanged);
            ItemTemplateDataPath = new BindableProperty<string>(string.Empty);

            ItemEntites = new List<UIEntity>();
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
            OnUpdate();
            RefreshSelectionState();
        }

        /// <summary>
        /// 获取当前绑定到指定索引的 item ViewModel。
        /// </summary>
        public virtual bool TryGetBoundItemViewModel(int index, out ObservableObject itemViewModel)
        {
            itemViewModel = null;
            if (index < 0 || index >= ItemEntites.Count)
            {
                return false;
            }

            var entity = ItemEntites[index];
            if (entity == null)
            {
                return false;
            }

            itemViewModel = entity.ViewModel;
            return itemViewModel != null;
        }

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

        /// <summary>
        /// 创建一个ItemView
        /// </summary>
        /// <param name="itemViewModel">这个Item对应的视图模型</param>
        /// <param name="itemObject">这个Item对应的GameObject</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected UIEntity CreateItemEntity(ObservableObject itemViewModel, GameObject itemObject)
        {
            var itemView = View.Create(this.AssetLoader, itemObject);
            var entity = UIEntity.Create(itemView, itemViewModel);
            ItemEntites.Add(entity);
            return entity;
        }

        protected override void OnRelease()
        {
            List.Dispose();
            SelectedIndex.Dispose();
            SelectedItem.Dispose();
            ItemTemplateDataPath.Dispose();

            foreach (var itemEntity in ItemEntites)
            {
                itemEntity.Destroy();
            }
            ItemEntites.Clear();
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
            var list = List?.Value;
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        int ResolveIndexByItem(ObservableObject item)
        {
            var list = List?.Value;
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
