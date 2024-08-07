using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// 适用于ScrollRect的列表视图元素
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollViewElement : StaticListViewElement
    {
        ScrollRect scrollRect;

        protected override void Initialize()
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