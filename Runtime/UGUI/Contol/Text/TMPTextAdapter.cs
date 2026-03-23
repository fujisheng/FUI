using TMPro;

using UnityEngine;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPTextAdapter : MonoBehaviour, IText
    {
        TextMeshProUGUI component;
        TextMeshProUGUI Component
        {
            get
            {
                if (component == null)
                {
                    component = GetComponent<TextMeshProUGUI>();
                }
                return component;
            }
        }

        public string Text
        {
            get => Component.text;
            set
            {
                Component.text = value;
            }
        }

        public int FontSize
        {
            get => (int)Component.fontSize;
            set
            {
                Component.fontSize = value;
            }
        }

        public Color Color
        {
            get => Component.color;
            set
            {
                Component.color = value;
            }
        }

        /// <summary>
        /// 文本对齐方式映射
        /// </summary>
        public TextAnchor Alignment
        {
            get
            {
                return Component.alignment switch
                {
                    TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
                    TextAlignmentOptions.Top => TextAnchor.UpperCenter,
                    TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
                    TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
                    TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
                    TextAlignmentOptions.Right => TextAnchor.MiddleRight,
                    TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft,
                    TextAlignmentOptions.Bottom => TextAnchor.LowerCenter,
                    TextAlignmentOptions.BottomRight => TextAnchor.LowerRight,
                    _ => TextAnchor.UpperLeft,
                };
            }
            set
            {
                Component.alignment = value switch
                {
                    TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                    TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                    TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                    TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                    TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                    TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                    TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                    TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                    TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                    _ => TextAlignmentOptions.TopLeft,
                };
            }
        }
    }
}