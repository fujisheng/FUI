using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// �����ڹ̶�λ�õ��б���ͼԪ��  ����һ���ڵ����кܶ��ӽڵ㣬 ÿ���ӽڵ��¶���һ��item
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class FixedPositionListViewElement : StaticListViewElement
    {
        List<Transform> positions;

        protected override void Initialize()
        {
            base.Initialize();
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
        }
    }
}