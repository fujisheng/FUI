using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 基于 ScrollRect 的列表视图元素。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("FUI/Elements/Scroll List")]
    public class ScrollListElement : RecyclingListElement, IScrollPositionElement
    {
        bool isUpdatingScrollState;
        protected ScrollRect scrollRect;

        /// <summary>ScrollRect 的归一化位置。</summary>
        public BindableProperty<Vector2> NormalizedPosition { get; private set; }

        public BindableProperty<ScrollPoint> NormalizedScroll { get; private set; }

        /// <summary>Content 的锚点位置。</summary>
        public BindableProperty<Vector2> ScrollPosition { get; private set; }

        /// <summary>当前滚动速度。</summary>
        public BindableProperty<Vector2> Velocity { get; private set; }

        /// <summary>是否允许水平滚动。</summary>
        public BindableProperty<bool> Horizontal { get; private set; }

        /// <summary>是否允许垂直滚动。</summary>
        public BindableProperty<bool> Vertical { get; private set; }

        /// <summary>真实 ScrollRect 输入产生的位置变更事件。</summary>
        public Command<Vector2> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            scrollRect = GetComponent<ScrollRect>();
            base.OnInitialize();
            NormalizedPosition = CreateProperty(scrollRect.normalizedPosition, OnNormalizedPositionChanged);
            var normalized = NormalizedPosition.Value;
            NormalizedScroll = CreateProperty(new ScrollPoint(normalized.x,normalized.y), (oldValue,newValue) =>
                NormalizedPosition.Value = new Vector2(newValue.x,newValue.y));
            ValueChangedHandler<Vector2> onNormalizedChanged = (oldValue,newValue) =>
                NormalizedScroll.Value = new ScrollPoint(newValue.x,newValue.y);
            NormalizedPosition.OnValueChanged += onNormalizedChanged;
            TrackCleanup(() => NormalizedPosition.OnValueChanged -= onNormalizedChanged);
            ScrollPosition = CreateProperty(GetContentAnchoredPosition(), OnScrollPositionChanged);
            Velocity = CreateProperty(scrollRect.velocity, (oldValue, newValue) => scrollRect.velocity = newValue);
            Horizontal = CreateProperty(scrollRect.horizontal, (oldValue, newValue) => scrollRect.horizontal = newValue);
            Vertical = CreateProperty(scrollRect.vertical, (oldValue, newValue) => scrollRect.vertical = newValue);
            OnValueChanged = Track(new Command<Vector2>());
            scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
            TrackCleanup(() => scrollRect?.onValueChanged.RemoveListener(OnScrollRectValueChanged));
        }

        protected override void SetParent(int index, GameObject item)
        {
            item.transform.SetParent(scrollRect.content);
            item.transform.SetSiblingIndex(index);
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
                Velocity.SetValue(scrollRect.velocity);
                OnScrollStateChanged();
                OnValueChanged.Invoke(value);
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
