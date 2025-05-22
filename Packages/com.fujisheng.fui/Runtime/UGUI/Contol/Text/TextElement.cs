using FUI.Bindable;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    public class TextElement : UIElement<IText>
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public BindableProperty<string> Text { get; protected set; }

        /// <summary>
        /// 格式化文本
        /// </summary>
        public BindableProperty<FormatString> FormatText { get; protected set; }

        /// <summary>
        /// 文本内容Object
        /// </summary>
        public BindableProperty<object> TextObject { get; protected set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public BindableProperty<int> FontSize { get; private set; }

        /// <summary>
        /// 文本颜色
        /// </summary>
        public BindableProperty<Color> Color { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Text = new BindableProperty<string>(Component.Text, (oldValue, newValue) => Component.Text = newValue);
            FormatText = new BindableProperty<FormatString>(default, (oldValue, newValue) => Component.Text = newValue.ToString());
            TextObject = new BindableProperty<object>(Component.Text, (oldValue, newValue) => Text.Value = newValue?.ToString());
            FontSize = new BindableProperty<int>(Component.FontSize, (oldValue, newValue) => Component.FontSize = newValue);
            Color = new BindableProperty<Color>(Component.Color, (oldValue, newValue) => Component.Color = newValue);
        }

        protected override void OnRelease()
        {
            base.OnRelease();

            Text.Dispose();
            FormatText.Dispose();
            TextObject.Dispose();
            FontSize.Dispose();
            Color.Dispose();
        }

#if UNITY_EDITOR
        void Reset()
        {
            // 检查当前物体上是否有 Text 或 TextMeshPro 组件
            if (GetComponent<Text>() != null && GetComponent<LegacyTextAdapter>() == null)
            {
                // 如果有 Text 组件但没有 LegacyText，则添加 LegacyText
                gameObject.AddComponent<LegacyTextAdapter>();
            }
            else if (GetComponent<TextMeshProUGUI>() != null && GetComponent<TMPTextAdapter>() == null)
            {
                // 如果有 TextMeshProUGUI 组件但没有 TMPText，则添加 TMPText
                gameObject.AddComponent<TMPTextAdapter>();
            }
        }
#endif
    }
}
