using FUI.Binding;
using FUI.Infrastructure;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 适用于静态列表的列表视图元素
    /// </summary>
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
                OnItemsOverflow(Items.Value.Count, MaximumVisibleItems);
                return;
            }

            var visibleCount = Mathf.Min(Items.Value.Count, MaximumVisibleItems);
            // 如果itemInstances的数量大于List的数量，表示有多余的itemInstance 则直接复用
            if (items.Count >= visibleCount)
            {
                for (int i = index; i < visibleCount; i++)
                {
                    var item = UpdateOrReplaceItem(i, Items.Value[i]);
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
            var visibleCount = Mathf.Min(Items.Value.Count, MaximumVisibleItems);
            for (int index = 0; index < visibleCount; index++)
            {
                var item = Items.Value[index];
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

            if (Items.Value.Count > MaximumVisibleItems)
            {
                OnItemsOverflow(Items.Value.Count, MaximumVisibleItems);
            }
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
