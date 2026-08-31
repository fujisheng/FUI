using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Legacy Text 适配为可绑定文本 Element。
    /// </summary>
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("FUI/Elements/Text/Legacy Text")]
    public class LegacyTextElement : TextElement<Text>
    {
        /// <summary>字体资源。</summary>
        public BindableProperty<Font> Font { get; private set; }

        /// <summary>字体样式。</summary>
        public BindableProperty<FontStyle> FontStyle { get; private set; }

        /// <summary>行间距。</summary>
        public BindableProperty<float> LineSpacing { get; private set; }

        /// <summary>水平溢出策略。</summary>
        public BindableProperty<HorizontalWrapMode> HorizontalOverflow { get; private set; }

        /// <summary>垂直溢出策略。</summary>
        public BindableProperty<VerticalWrapMode> VerticalOverflow { get; private set; }

        /// <summary>是否启用 Best Fit。</summary>
        public BindableProperty<bool> BestFit { get; private set; }

        /// <summary>Best Fit 最小字号。</summary>
        public BindableProperty<int> MinFontSize { get; private set; }

        /// <summary>Best Fit 最大字号。</summary>
        public BindableProperty<int> MaxFontSize { get; private set; }

        /// <inheritdoc />
        protected override string TextValue
        {
            get => Component.text;
            set => Component.text = value;
        }

        /// <inheritdoc />
        protected override float FontSizeValue
        {
            get => Component.fontSize;
            set => Component.fontSize = Mathf.RoundToInt(value);
        }

        /// <inheritdoc />
        protected override TextAnchor AlignmentValue
        {
            get => Component.alignment;
            set => Component.alignment = value;
        }

        /// <inheritdoc />
        protected override bool RichTextValue
        {
            get => Component.supportRichText;
            set => Component.supportRichText = value;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Font = CreateProperty(Component.font, (oldValue, newValue) => Component.font = newValue);
            FontStyle = CreateProperty(Component.fontStyle, (oldValue, newValue) => Component.fontStyle = newValue);
            LineSpacing = CreateProperty(Component.lineSpacing, (oldValue, newValue) => Component.lineSpacing = newValue);
            HorizontalOverflow = CreateProperty(Component.horizontalOverflow, (oldValue, newValue) => Component.horizontalOverflow = newValue);
            VerticalOverflow = CreateProperty(Component.verticalOverflow, (oldValue, newValue) => Component.verticalOverflow = newValue);
            BestFit = CreateProperty(Component.resizeTextForBestFit, (oldValue, newValue) => Component.resizeTextForBestFit = newValue);
            MinFontSize = CreateProperty(Component.resizeTextMinSize, (oldValue, newValue) => Component.resizeTextMinSize = newValue);
            MaxFontSize = CreateProperty(Component.resizeTextMaxSize, (oldValue, newValue) => Component.resizeTextMaxSize = newValue);
        }
    }
}
