using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Text))]
    public class TextElement : UGUIView
    {
        Text text;

        /// <summary>
        /// 文本内容
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// 文本内容Object
        /// </summary>
        public BindableProperty<object> TextObject { get; private set; }

        /// <summary>
        /// 字体
        /// </summary>
        public BindableProperty<Font> Font { get; private set; }

        /// <summary>
        /// 字体资源
        /// </summary>
        public BindableProperty<string> FontSources { get; private set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public BindableProperty<int> FontSize { get; private set; }

        /// <summary>
        /// 文本颜色
        /// </summary>
        public BindableProperty<Color> Color { get; private set; }

        protected override void Initialize()
        {
            text = transform.GetComponent<Text>();

            Text = new BindableProperty<string>(text.text);
            TextObject = new BindableProperty<object>(text.text);
            Font = new BindableProperty<Font>(text.font);
            FontSources = new BindableProperty<string>();
            FontSize = new BindableProperty<int>(text.fontSize);
            Color = new BindableProperty<Color>(text.color);

            Text.OnValueChanged += OnSetText;
            TextObject.OnValueChanged += OnSetTextObject;
            Font.OnValueChanged += OnSetFont;
            FontSources.OnValueChanged += OnSetFontSources;
            FontSize.OnValueChanged += OnSetFontSize;
            Color.OnValueChanged += OnSetColor;
        }

        void OnSetText(string oldValue, string newValue)
        {
            text.text = newValue;
        }

        void OnSetTextObject(object oldValue, object newValue)
        {
            text.text = newValue == null ? string.Empty : newValue.ToString();
        }

        void OnSetFont(Font oldValue,  Font newValue)
        {
            text.font = newValue;
        }

        void OnSetFontSources(string oldValue, string newValue)
        {
            text.font = AssetLoader.Load<Font>(newValue);
        }

        void OnSetFontSize(int oldValue, int newValue)
        {
            text.fontSize = newValue;
        }

        void OnSetColor(Color oldValue, Color newValue)
        {
            text.color = newValue;
        }

        protected override void Destroy()
        {
            Text.Dispose();
            TextObject.Dispose();
            Font.Dispose();
            FontSources.Dispose();
            FontSize.Dispose();
            Color.Dispose();
        }
    }
}
