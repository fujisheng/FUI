using FUI.Bindable;

using System;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Text))]
    public class TextElement : UIElement<Text>
    {
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
        public BindableProperty<string> FontSource { get; private set; }

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
            base.Initialize();

            Text = new BindableProperty<string>(Component.text, (oldValue, newValue) => Component.text = newValue);
            TextObject = new BindableProperty<object>(Component.text, (oldValue, newValue) => Text.Value = newValue?.ToString());
            Font = new BindableProperty<Font>(Component.font, (oldValue, newValue) => Component.font = newValue);
            FontSource = new BindableProperty<string>(null, (oldValue, newValue) => Font.Value = AssetLoader.Load<Font>(newValue));
            FontSize = new BindableProperty<int>(Component.fontSize, (oldValue, newValue) => Component.fontSize = newValue);
            Color = new BindableProperty<Color>(Component.color, (oldValue, newValue) => Component.color = newValue);
        }

        protected override void Destroy()
        {
            base.Destroy();

            Text.Dispose();
            TextObject.Dispose();
            Font.Dispose();
            FontSource.Dispose();
            FontSize.Dispose();
            Color.Dispose();
        }
    }
}
