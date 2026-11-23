using FUI.Binding;
using FUI.Infrastructure;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 适用于静态列表的列表视图元素
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/Recycling List")]
    public class RecyclingListElement : ListElement
    {
        [SerializeField]
        protected GameObject itemPrefab;

        List<(ViewInstance instance, GameObject gameObject)> items;

        protected virtual int MaximumVisibleItems => int.MaxValue;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            itemPrefab = ResolveItemPrefab(itemPrefab);
            if (itemPrefab == null)
            {
                throw new MissingReferenceException($"{name} does not have a valid itemPrefab.");
            }

            itemPrefab.SetActive(false);
            items = new List<(ViewInstance instance, GameObject gameObject)>();
        }

        protected override void OnAdd(int index, ObservableObject itemData)
        {
            if (index >= MaximumVisibleItems)
            {
                OnItemsOverflow(ItemsSource.Value.Count, MaximumVisibleItems);
                return;
            }

            var visibleCount = Mathf.Min(ItemsSource.Value.Count, MaximumVisibleItems);
            // 如果itemInstances的数量大于List的数量，表示有多余的itemInstance 则直接复用
            if (items.Count >= visibleCount)
            {
                for (int i = index; i < visibleCount; i++)
                {
                    var item = UpdateOrReplaceItem(i, ItemsSource.Value[i]);
                    SetParent(i, item.gameObject);
                    item.instance.Enable();
                }
                return;
            }

            // 如果itemInstances的数量小于等于List的数量，表示没有多余的 则创建新的
            var itemInstance = CreateItem(itemData, out var itemObject);
            SetParent(index, itemObject);
            itemInstance.Enable();
            items.Insert(index, (itemInstance, itemObject));
            MoveLastItemInstanceTo(index);
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            if (index >= items.Count)
            {
                OnUpdate();
                return;
            }
            //要移除直接移除，然后将移除的itemInstances放到最后并隐藏
            var removed = items[index];
            items.RemoveAt(index);
            removed.instance.Disable();
            items.Add(removed);
            var removedInstance = ItemInstances[index];
            ItemInstances.RemoveAt(index);
            ItemInstances.Add(removedInstance);
            SetParent(items.Count - 1, removed.gameObject);
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            if (index >= items.Count)
            {
                return;
            }
            UpdateOrReplaceItem(index, newItem).instance.Enable();
        }

        protected override void OnUpdate()
        {
            var visibleCount = Mathf.Min(ItemsSource.Value.Count, MaximumVisibleItems);
            for (int index = 0; index < visibleCount; index++)
            {
                var item = ItemsSource.Value[index];
                if (index < items.Count)
                {
                    var instance = items[index].instance;
                    if(instance.ViewModel != item)
                    {
                        instance = UpdateOrReplaceItem(index, item).instance;
                    }
                    else
                    {
                        instance.SynchronizeProperties();
                    }

                    instance.Enable();
                }
                else
                {
                    var itemInstance = CreateItem(item, out var itemObject);
                    SetParent(index, itemObject);
                    itemInstance.Enable();
                    items.Add((itemInstance, itemObject));
                }
            }

            for (int index = visibleCount; index < items.Count; index++)
            {
                items[index].instance.Disable();
            }

            if (ItemsSource.Value.Count > MaximumVisibleItems)
            {
                OnItemsOverflow(ItemsSource.Value.Count, MaximumVisibleItems);
            }
        }

        private protected void RealizeRange(int first, int end, bool synchronize)
        {
            var count = end - first;
            for (var position = 0; position < count; position++)
            {
                var index = first + position;
                var model = ItemsSource.Value[index];
                var existing = position;
                while (existing < items.Count && !ReferenceEquals(items[existing].instance.ViewModel, model))
                {
                    existing++;
                }

                if (existing < items.Count)
                {
                    MoveItem(existing, position);
                    if (synchronize)
                    {
                        items[position].instance.SynchronizeProperties();
                    }
                }
                else
                {
                    var spare = position;
                    while (spare < items.Count && AppearsLater(items[spare].instance.ViewModel, index + 1, end))
                    {
                        spare++;
                    }
                    if (spare < items.Count)
                    {
                        MoveItem(spare, position);
                        UpdateOrReplaceItem(position, model);
                    }
                    else
                    {
                        var instance = CreateItem(model, out var row);
                        items.Insert(position, (instance, row));
                        MoveLastItemInstanceTo(position);
                    }
                }

                var item = items[position];
                PlaceRealizedItem(index, position, item.gameObject);
                item.instance.Enable();
            }

            while (items.Count > count)
            {
                var last = items.Count - 1;
                var item = items[last];
                items.RemoveAt(last);
                ReleaseItemInstance(item.instance);
                item.gameObject.SetActive(false);
                Utility.DestroyObject(item.gameObject);
            }
        }

        bool AppearsLater(ObservableObject model, int first, int end)
        {
            for (var index = first; index < end; index++)
            {
                if (ReferenceEquals(ItemsSource.Value[index], model))
                {
                    return true;
                }
            }
            return false;
        }

        void MoveItem(int from, int to)
        {
            if (from == to)
            {
                return;
            }
            var item = items[from];
            items.RemoveAt(from);
            items.Insert(to, item);
            ItemInstances.RemoveAt(from);
            ItemInstances.Insert(to, item.instance);
        }

        private protected virtual void PlaceRealizedItem(int index, int position, GameObject item)
        {
            SetParent(position, item);
        }

        (ViewInstance instance, GameObject gameObject) UpdateOrReplaceItem(int index, ObservableObject itemData)
        {
            var item = items[index];
            if (item.instance.ViewModel.GetType() == itemData.GetType())
            {
                item.instance.UpdateViewModel(itemData);
                return item;
            }

            ReleaseItemInstance(item.instance);
            Utility.DestroyObject(item.gameObject);

            var itemInstance = CreateItem(itemData, out var itemObject);
            SetParent(index, itemObject);
            items[index] = (itemInstance, itemObject);
            MoveLastItemInstanceTo(index);
            return items[index];
        }

        void MoveLastItemInstanceTo(int index)
        {
            var lastIndex = ItemInstances.Count - 1;
            if (lastIndex == index)
            {
                return;
            }

            var instance = ItemInstances[lastIndex];
            ItemInstances.RemoveAt(lastIndex);
            ItemInstances.Insert(index, instance);
        }

        ViewInstance CreateItem(ObservableObject item, out GameObject itemObject)
        {
            itemObject = Instantiate(itemPrefab, transform);
            return CreateItemInstance(item, itemObject);
        }

        /// <summary>
        /// 设置Item的父节点
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="item">item</param>
        protected virtual void SetParent(int index, GameObject item)
        {
            if(item.transform.parent != this.transform)
            {
                item.transform.parent = this.transform;
            }

            if(item.transform.GetSiblingIndex() != index)
            {
                item.transform.SetSiblingIndex(index);
            }
        }

        protected virtual GameObject ResolveItemPrefab(GameObject currentItemPrefab)
        {
            if (currentItemPrefab != null)
            {
                return currentItemPrefab;
            }

            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        protected virtual void OnItemsOverflow(int itemCount, int capacity)
        {
        }

        protected override void OnReset()
        {
            if (items == null)
            {
                return;
            }
            for (var index = 0; index < items.Count; index++)
            {
                items[index].instance.Disable();
            }
        }

        protected override void OnRelease()
        {
            if (items != null)
            {
                for (var index = ItemInstances.Count - 1; index >= 0; index--)
                {
                    ReleaseItemInstance(ItemInstances[index]);
                }
                for (var index = 0; index < items.Count; index++)
                {
                    if (items[index].gameObject != null)
                    {
                        Utility.DestroyObject(items[index].gameObject);
                    }
                }
                items.Clear();
            }
            base.OnRelease();
        }
    }
}
