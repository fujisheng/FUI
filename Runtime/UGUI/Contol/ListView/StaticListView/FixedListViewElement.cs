using FUI.Bindable;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 固定的列表 所有Item都预制好了 且不会增加减少数量
    /// </summary>
    public class FixedListViewElement : ListViewElement
    {
        [SerializeField]
        Transform content;

        List<GameObject> items;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var childCount = content.childCount;
            items = new List<GameObject>(childCount);
            for(int i = 0; i < childCount; i++)
            {
                var child = content.GetChild(i);
                items.Add(child.gameObject);
            }
        }

        protected override void OnAdd(int index, ObservableObject item)
        {
            if(items.Count < List.Value.Count)
            {
                return;
            }

            if(ItemEntites.Count > List.Value.Count)
            {
                for(int i = index; i < List.Value.Count; i++)
                {
                    var itemEntity = ItemEntites[i];
                    itemEntity.UpdateViewModel(List.Value[i]);
                    itemEntity.Enable();
                }

                return;
            }

            var entity = CreateItemEntity(item, items[index]);
            entity.Enable();
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            if(ItemEntites.Count <= index)
            {
                return;
            }

            var itemEntity = ItemEntites[index];
            itemEntity.Disable();
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            if(ItemEntites.Count <= index)
            {
                return;
            }

            ItemEntites[index].UpdateViewModel(newItem);
        }

        protected override void OnUpdate()
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (List.Value.Count <= index)
                {
                    items[index].SetActive(false);
                    continue;
                }
               
                var data = List.Value[index];
                if(index < ItemEntites.Count)
                {
                    var entity = ItemEntites[index];
                    if(entity.ViewModel != data)
                    {
                        entity.UpdateViewModel(data);
                    }
                    else
                    {
                        entity.SynchronizeProperties();
                    }
                }
                else
                {
                    var entity = CreateItemEntity(data, items[index]);
                    entity.Enable();
                }
            }

            for(int index = List.Value.Count; index < ItemEntites.Count; index++)
            {
                ItemEntites[index].Disable();
            }
        }

        protected override void OnRelease()
        {
            ItemEntites.Clear();
            items.Clear();
        }
    }
}