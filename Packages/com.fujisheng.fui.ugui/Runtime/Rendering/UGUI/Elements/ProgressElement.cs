using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>在作者完整矩形中从左向右填充，支持空 Sprite 和父级拉伸。</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("FUI/Elements/Progress")]
    public sealed class ProgressElement : ComponentElement<Image>, IFloatValueElement
    {
        Vector2 authoredSize;
        Vector2 authoredPosition;
        bool initialized;
        bool applying;
        public BindableProperty<float> Value { get; private set; }
        public Command<float> ValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            authoredSize = RectTransform.sizeDelta;
            authoredPosition = RectTransform.anchoredPosition;
            Value = Track(new BindableProperty<float>(1, (_, value) => Apply(), value => Mathf.Clamp01(value)));
            ValueChanged = Track(new Command<float>());
            initialized = true;
            TrackCleanup(() =>
            {
                initialized = false;
                RectTransform.sizeDelta = authoredSize;
                RectTransform.anchoredPosition = authoredPosition;
            });
        }

        void OnRectTransformDimensionsChange()
        {
            if (initialized && !applying) { Apply(); }
        }

        void Apply()
        {
            if (!initialized || applying) { return; }
            applying = true;
            try
            {
                var parent = RectTransform.parent as RectTransform;
                var span = RectTransform.anchorMax.x - RectTransform.anchorMin.x;
                var width = Mathf.Max(0, authoredSize.x + (parent ? parent.rect.width * span : 0));
                var filled = width * Value.Value;
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, filled);
                RectTransform.anchoredPosition = new Vector2(authoredPosition.x - (width - filled) * RectTransform.pivot.x, authoredPosition.y);
            }
            finally { applying = false; }
        }
    }
}
