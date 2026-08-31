using System.Collections.Generic;
using FUI.Diagnostics;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将可复用 Item 实例放入预先配置的固定 Anchor。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/Anchored List")]
    public class AnchoredListElement : RecyclingListElement
    {
        [SerializeField]
        Transform[] anchors;

        List<Transform> positions;
        bool overflowWarningLogged;

        /// <summary>可用于显示 Item 的 Anchor 数量。</summary>
        public int Capacity => positions?.Count ?? 0;

        protected override int MaximumVisibleItems => Capacity;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            positions = new List<Transform>();
            if (anchors != null && anchors.Length > 0)
            {
                for (var index = 0; index < anchors.Length; index++)
                {
                    if (anchors[index] != null)
                    {
                        positions.Add(anchors[index]);
                    }
                }
            }
            else
            {
                for (var index = 0; index < transform.childCount; index++)
                {
                    var child = transform.GetChild(index);
                    if (itemPrefab == null || child.gameObject != itemPrefab)
                    {
                        positions.Add(child);
                    }
                }
            }

            if (positions.Count == 0)
            {
                throw new MissingReferenceException($"Element '{Name}' requires at least one anchor Transform.");
            }
        }

        protected override void SetParent(int index, GameObject item)
        {
            item.transform.SetParent(positions[index]);
            item.transform.localPosition = Vector3.zero;
        }

        protected override void OnItemsOverflow(int itemCount, int capacity)
        {
            if (overflowWarningLogged)
            {
                return;
            }
            overflowWarningLogged = true;
            Log.Instance.LogWarning(
                $"Element '{Name}' contains {itemCount} items but only {capacity} anchors. Extra items are hidden.");
        }

        protected override void OnRelease()
        {
            positions?.Clear();
            overflowWarningLogged = false;
            base.OnRelease();
        }
    }
}
