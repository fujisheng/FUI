using FUI.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 TextMeshProUGUI 适配为可绑定文本 Element。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [AddComponentMenu("FUI/Elements/Text/TextMeshPro")]
    public class TMPTextElement : TextElement<TextMeshProUGUI>
    {
        /// <summary>TMP 字体资源。</summary>
        public BindableProperty<TMP_FontAsset> FontAsset { get; private set; }

        /// <summary>TMP 字体样式。</summary>
        public BindableProperty<FontStyles> FontStyle { get; private set; }

        /// <summary>是否启用自动字号。</summary>
        public BindableProperty<bool> AutoSize { get; private set; }

        /// <summary>自动字号下限。</summary>
        public BindableProperty<float> MinFontSize { get; private set; }

        /// <summary>自动字号上限。</summary>
        public BindableProperty<float> MaxFontSize { get; private set; }

        /// <summary>字符间距。</summary>
        public BindableProperty<float> CharacterSpacing { get; private set; }

        /// <summary>单词间距。</summary>
        public BindableProperty<float> WordSpacing { get; private set; }

        /// <summary>行间距。</summary>
        public BindableProperty<float> LineSpacing { get; private set; }

        /// <summary>段落间距。</summary>
        public BindableProperty<float> ParagraphSpacing { get; private set; }

        /// <summary>文本换行策略。</summary>
        public BindableProperty<TextWrappingModes> WrappingMode { get; private set; }

        /// <summary>文本溢出策略。</summary>
        public BindableProperty<TextOverflowModes> OverflowMode { get; private set; }

        /// <summary>最多显示的字符数量。</summary>
        public BindableProperty<int> MaxVisibleCharacters { get; private set; }

        /// <summary>是否按从右到左方向渲染文本。</summary>
        public BindableProperty<bool> RightToLeft { get; private set; }

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
            set => Component.fontSize = value;
        }

        /// <inheritdoc />
        protected override TextAnchor AlignmentValue
        {
            get => ToTextAnchor(Component.alignment);
            set => Component.alignment = ToTMPAlignment(value);
        }

        /// <inheritdoc />
        protected override bool RichTextValue
        {
            get => Component.richText;
            set => Component.richText = value;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            FontAsset = CreateProperty(Component.font, (oldValue, newValue) => Component.font = newValue);
            FontStyle = CreateProperty(Component.fontStyle, (oldValue, newValue) => Component.fontStyle = newValue);
            AutoSize = CreateProperty(Component.enableAutoSizing, (oldValue, newValue) => Component.enableAutoSizing = newValue);
            MinFontSize = CreateProperty(Component.fontSizeMin, (oldValue, newValue) => Component.fontSizeMin = newValue);
            MaxFontSize = CreateProperty(Component.fontSizeMax, (oldValue, newValue) => Component.fontSizeMax = newValue);
            CharacterSpacing = CreateProperty(Component.characterSpacing, (oldValue, newValue) => Component.characterSpacing = newValue);
            WordSpacing = CreateProperty(Component.wordSpacing, (oldValue, newValue) => Component.wordSpacing = newValue);
            LineSpacing = CreateProperty(Component.lineSpacing, (oldValue, newValue) => Component.lineSpacing = newValue);
            ParagraphSpacing = CreateProperty(Component.paragraphSpacing, (oldValue, newValue) => Component.paragraphSpacing = newValue);
            WrappingMode = CreateProperty(Component.textWrappingMode, (oldValue, newValue) => Component.textWrappingMode = newValue);
            OverflowMode = CreateProperty(Component.overflowMode, (oldValue, newValue) => Component.overflowMode = newValue);
            MaxVisibleCharacters = CreateProperty(Component.maxVisibleCharacters, (oldValue, newValue) => Component.maxVisibleCharacters = newValue);
            RightToLeft = CreateProperty(Component.isRightToLeftText, (oldValue, newValue) => Component.isRightToLeftText = newValue);
        }

        static TextAnchor ToTextAnchor(TextAlignmentOptions value)
        {
            return value switch
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

        static TextAlignmentOptions ToTMPAlignment(TextAnchor value)
        {
            return value switch
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
