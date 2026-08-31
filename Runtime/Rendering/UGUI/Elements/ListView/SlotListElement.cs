using FUI.Binding;
using FUI.Diagnostics;
using FUI.Infrastructure;

using System.Collections.Generic;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 固定的列表 所有Item都预制好了 且不会增加减少数量
    /// </summary>
    [AddComponentMenu("FUI/Elements/Slot List")]
    public class SlotListElement : ListElement
    {
        [SerializeField]
        Transform content;

        List<GameObject> items;
        bool overflowWarningLogged;

        /// <summary>可用于显示 Item 的固定 Slot 数量。</summary>
        public int Capacity => items?.Count ?? 0;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (content == null)
            {
                throw new MissingReferenceException($"Element '{Name}' requires a content Transform.");
            }

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
            OnUpdate();
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            OnUpdate();
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            if(ItemInstances.Count <= index)
            {
                return;
            }

            UpdateSlot(index, newItem);
        }

        protected override void OnUpdate()
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (Items.Value.Count <= index)
                {
                    items[index].SetActive(false);
                    continue;
                }
               
                var data = Items.Value[index];
                if(index < ItemInstances.Count)
                {
                    var instance = ItemInstances[index];
                    if (instance.ViewModel.GetType() != data.GetType())
                    {
                        instance = UpdateSlot(index, data);
                    }
                    else if(instance.ViewModel != data)
                    {
                        instance.UpdateViewModel(data);
                    }
                    else
                    {
                        instance.SynchronizeProperties();
                    }
                    instance.Enable();
                }
                else
                {
                    var instance = CreateItemInstance(data, items[index]);
                    instance.Enable();
                }
            }

            for(int index = Items.Value.Count; index < ItemInstances.Count; index++)
            {
                ItemInstances[index].Disable();
            }

            if (Items.Value.Count > items.Count && !overflowWarningLogged)
            {
                overflowWarningLogged = true;
                Log.Instance.LogWarning(
                    $"Element '{Name}' contains {Items.Value.Count} items but only {items.Count} slots. Extra items are hidden.");
            }
        }

        ViewInstance UpdateSlot(int index, ObservableObject data)
        {
            var current = ItemInstances[index];
            if (current.ViewModel.GetType() == data.GetType())
            {
                current.UpdateViewModel(data);
                return current;
            }

            ReleaseItemInstance(current);
            var replacement = CreateItemInstance(data, items[index], index);
            replacement.Enable();
            return replacement;
        }

        protected override void OnReset()
        {
            if (items == null)
            {
                return;
            }
            for (var index = 0; index < ItemInstances.Count; index++)
            {
                ItemInstances[index].Disable();
            }
            for (var index = ItemInstances.Count; index < items.Count; index++)
            {
                items[index].SetActive(false);
            }
        }

        protected override void OnRelease()
        {
            items?.Clear();
            overflowWarningLogged = false;
            base.OnRelease();
        }
    }
}
