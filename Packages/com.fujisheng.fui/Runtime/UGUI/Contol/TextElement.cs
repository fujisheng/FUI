using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Text))]
    public class TextElement : UGUIView
    {
        Text text;

        public BindableProperty<string> Text { get; private set; }
        public BindableProperty<Font> Font { get; private set; }
        public BindableProperty<string> FontSources { get; private set; }
        public BindableProperty<int> FontSize { get; private set; }
        public BindableProperty<Color> Color { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            text = transform.GetComponent<Text>();
            Text = new BindableProperty<string>(text.text);
            Font = new BindableProperty<Font>(text.font);
            FontSources = new BindableProperty<string>();
            FontSize = new BindableProperty<int>(text.fontSize);
            Color = new BindableProperty<Color>(text.color);

            Text.PropertySet += OnSetText;
            Font.PropertySet += OnSetFont;
            FontSources.PropertySet += OnSetFontSources;
            FontSize.PropertySet += OnSetFontSize;
            Color.PropertySet += OnSetColor;
        }

        void OnSetText(string oldValue, string newValue)
        {
            text.text = newValue;
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

        public override void Destroy()
        {
            base.Destroy();

            Text.ClearEvent();
            Font.ClearEvent();
            FontSources.ClearEvent();
            FontSize.ClearEvent();
            Color.ClearEvent();
        }
    }
}
