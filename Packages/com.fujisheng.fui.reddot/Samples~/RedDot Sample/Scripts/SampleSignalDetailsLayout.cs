using UnityEngine;
using UnityEngine.UI;

namespace FUI.Samples.RedDot
{
    /// <summary>将实时诊断文本置于滚动内容顶部，按真实排版高度扩展，不创建UI资源。</summary>
    sealed class SampleSignalDetailsLayout : MonoBehaviour
    {
        ScrollRect scroll;
        Text label;
        string previous;
        bool resetPosition;

        public void Configure(ScrollRect value, Text text)
        {
            scroll = value;
            label = text;
            label.alignment = TextAnchor.UpperLeft;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(12, -12);
        }

        void OnEnable() { previous = null; resetPosition = true; }

        void LateUpdate()
        {
            if (label == null || previous == label.text) { return; }
            previous = label.text;
            var height = Mathf.Max(label.preferredHeight, scroll.viewport.rect.height - 24);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 24);
            // 仅首次展开回到顶部；刷新诊断时保留用户已滚动的位置。
            if (resetPosition) { scroll.verticalNormalizedPosition = 1; resetPosition = false; }
        }
    }
}
