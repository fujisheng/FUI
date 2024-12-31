using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// ������ScrollRect���б���ͼԪ��
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectElement : StaticListViewElement
    {
        ScrollRect scrollRect;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            scrollRect = GetComponent<ScrollRect>();
        }

        protected override void SetParent(int index, GameObject item)
        {
            item.transform.SetParent(scrollRect.content);
            item.transform.SetSiblingIndex(index);
        }
    }
}