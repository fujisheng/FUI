using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// ������ScrollRect���б���ͼԪ��
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollViewElement : StaticListViewElement
    {
        ScrollRect scrollRect;

        public override void Initialize()
        {
            base.Initialize();
            scrollRect = GetComponent<ScrollRect>();
        }

        protected override void SetParent(int index, GameObject item)
        {
            item.transform.SetParent(scrollRect.content);
            item.transform.SetSiblingIndex(index);
        }
    }
}