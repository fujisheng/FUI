using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 适用于固定位置的列表视图元素  即是一个节点下有很多子节点， 每个子节点下都是一个item
    /// </summary>
    public class FixedPositionListViewElement : StaticListViewElement
    {
        List<Transform> positions;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            positions = new List<Transform>(transform.childCount);
            for(int i = 0; i < transform.childCount; i++)
            {
                positions.Add(transform.GetChild(i));
            }
        }

        protected override void SetParent(int index, GameObject item)
        {
            if(index >= positions.Count)
            {
                throw new System.IndexOutOfRangeException($"Index out of range: {index}");
            }

            item.transform.SetParent(positions[index]);
            item.transform.localPosition = Vector3.zero;
        }
    }
}