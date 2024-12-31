using FUI.Bindable;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 适用于静态列表的列表视图元素
    /// </summary>
    public class StaticListViewElement : ListViewElement
    {
        [SerializeField]
        GameObject itemPrefab;

        List<(UIEntity entity, GameObject gameObject)> items;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            itemPrefab.SetActive(false);
            items = new List<(UIEntity context, GameObject gameObject)>();
        }

        protected override void OnAdd(int index, ObservableObject itemData)
        {
            // 如果itemEntities的数量大于List的数量，表示有多余的itemEntity 则直接复用
            if (items.Count > List.Value.Count)
            {
                for (int i = index; i < List.Value.Count; i++)
                {
                    var item = items[i];
                    item.entity.UpdateViewModel(List.Value[i]);
                    SetParent(index, item.gameObject);
                    item.entity.Enable();
                }
                return;
            }

            // 如果itemEntities的数量小于等于List的数量，表示没有多余的 则创建新的
            var itemEntity = CreateItem(itemData, out var itemObject);
            SetParent(index, itemObject);
            itemEntity.Enable();
            items.Insert(index, (itemEntity, itemObject));
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            //要移除直接移除，然后将移除的itemEntities放到最后并隐藏
            var removed = items[index];
            items.RemoveAt(index);
            removed.entity.Disable();
            items.Add(removed);
            SetParent(items.Count - 1, removed.gameObject);
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            items[index].entity.UpdateViewModel(newItem);
        }

        protected override void OnUpdate()
        {
            for (int index = 0; index < List.Value.Count; index++)
            {
                var item = List.Value[index];
                if (index < items.Count)
                {
                    items[index].entity.SynchronizeProperties();
                }
                else
                {
                    var itemEntity = CreateItem(item, out var itemObject);
                    SetParent(index, itemObject);
                    itemEntity.Enable();
                    items.Add((itemEntity, itemObject));
                }
            }

            for (int index = List.Value.Count; index < items.Count; index++)
            {
                items[index].entity.Disable();
            }
        }

        UIEntity CreateItem(ObservableObject item, out GameObject itemObject)
        {
            itemObject = Instantiate(itemPrefab);
            return CreateItemEntity(item, itemObject);
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
    }
}