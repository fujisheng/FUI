using FUI.Bindable;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// �����ھ�̬�б����б���ͼԪ��
    /// </summary>
    public class StaticListViewElement : ListViewElement
    {
        [SerializeField]
        GameObject itemPrefab;

        List<(UIEntity entity, GameObject gameObject)> items;

        protected override void Initialize()
        {
            UnityEngine.Debug.Log("Initialize...............");
            itemPrefab.SetActive(false);
            items = new List<(UIEntity context, GameObject gameObject)>();
        }

        protected override void OnAdd(int index, ObservableObject itemData)
        {
            // ���itemEntities����������List����������ʾ�ж����itemEntity ��ֱ�Ӹ���
            if (items.Count > List.Count)
            {
                for (int i = index; i < List.Count; i++)
                {
                    var item = items[i];
                    item.entity.SetViewModel(List[i]);
                    SetParent(index, item.gameObject);
                    item.entity.Enable();
                }
                return;
            }

            // ���itemEntities������С�ڵ���List����������ʾû�ж���� �򴴽��µ�
            var itemEntity = CreateItem(itemData, out var itemObject);
            SetParent(index, itemObject);
            itemEntity.Enable();
            items.Insert(index, (itemEntity, itemObject));
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            //Ҫ�Ƴ�ֱ���Ƴ���Ȼ���Ƴ���itemEntities�ŵ��������
            var removed = items[index];
            items.RemoveAt(index);
            removed.entity.Disable();
            items.Add(removed);
            SetParent(items.Count - 1, removed.gameObject);
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            items[index].entity.SetViewModel(newItem);
        }

        protected override void OnUpdate()
        {
            for (int index = 0; index < List.Count; index++)
            {
                var item = List[index];
                UnityEngine.Debug.Log($"OnUpdate  index:{index}   items:{items}");
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

            for (int index = List.Count; index < items.Count; index++)
            {
                items[index].entity.Disable();
            }
        }

        protected override void OnReset()
        {
            OnUpdate();
        }

        UIEntity CreateItem(ObservableObject item, out GameObject itemObject)
        {
            itemObject = Instantiate(itemPrefab);
            return CreateItemEntity(item, itemObject);
        }

        /// <summary>
        /// ����Item�ĸ��ڵ�
        /// </summary>
        /// <param name="index">����</param>
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