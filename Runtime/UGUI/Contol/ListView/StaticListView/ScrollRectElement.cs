using FUI.Bindable;
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
        bool isUpdatingScrollState;
        protected ScrollRect scrollRect;

        public BindableProperty<Vector2> NormalizedPosition { get; private set; }

        public BindableProperty<Vector2> ScrollPosition { get; private set; }

        protected override void OnInitialize()
        {
            scrollRect = GetComponent<ScrollRect>();
            base.OnInitialize();
            NormalizedPosition = new BindableProperty<Vector2>(scrollRect.normalizedPosition, OnNormalizedPositionChanged);
            ScrollPosition = new BindableProperty<Vector2>(GetContentAnchoredPosition(), OnScrollPositionChanged);
            scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
        }

        protected override void SetParent(int index, GameObject item)
        {
            item.transform.SetParent(scrollRect.content);
            item.transform.SetSiblingIndex(index);
        }

        protected override void OnRelease()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
            }

            NormalizedPosition.Dispose();
            ScrollPosition.Dispose();
            base.OnRelease();
        }

        protected virtual void OnScrollStateChanged()
        {
        }

        void OnNormalizedPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (isUpdatingScrollState || scrollRect == null)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                scrollRect.normalizedPosition = newValue;
                ScrollPosition.SetValue(GetContentAnchoredPosition());
                OnScrollStateChanged();
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        void OnScrollPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (isUpdatingScrollState || scrollRect?.content == null)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                scrollRect.content.anchoredPosition = newValue;
                NormalizedPosition.SetValue(scrollRect.normalizedPosition);
                OnScrollStateChanged();
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        void OnScrollRectValueChanged(Vector2 value)
        {
            if (isUpdatingScrollState)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                NormalizedPosition.SetValue(value);
                ScrollPosition.SetValue(GetContentAnchoredPosition());
                OnScrollStateChanged();
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        protected Vector2 GetContentAnchoredPosition()
        {
            return scrollRect?.content == null ? Vector2.zero : scrollRect.content.anchoredPosition;
        }

        protected override GameObject ResolveItemPrefab(GameObject currentItemPrefab)
        {
            if (currentItemPrefab != null)
            {
                return currentItemPrefab;
            }

            if (scrollRect?.content != null && scrollRect.content.childCount > 0)
            {
                return scrollRect.content.GetChild(0).gameObject;
            }

            return base.ResolveItemPrefab(currentItemPrefab);
        }
    }
}
