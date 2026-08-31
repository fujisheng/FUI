using System;
using System.Globalization;
using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 定义所有文本后端共享的绑定契约。
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class TextElement : GraphicElement
    {
        /// <summary>读取或写入后端文本。</summary>
        protected abstract string TextValue { get; set; }

        /// <summary>读取或写入后端字号。</summary>
        protected abstract float FontSizeValue { get; set; }

        /// <summary>读取或写入后端对齐方式。</summary>
        protected abstract TextAnchor AlignmentValue { get; set; }

        /// <summary>读取或写入后端富文本开关。</summary>
        protected abstract bool RichTextValue { get; set; }

        /// <summary>文本内容。</summary>
        public BindableProperty<string> Text { get; protected set; }

        /// <summary>格式化文本。</summary>
        public BindableProperty<FormattedText> FormattedText { get; protected set; }

        /// <summary>任意对象形式的文本内容；使用当前文化转换，null 转为空字符串。</summary>
        public BindableProperty<object> Content { get; protected set; }

        /// <summary>字体大小。</summary>
        public BindableProperty<float> FontSize { get; private set; }

        /// <summary>文本对齐方式。</summary>
        public BindableProperty<TextAnchor> Alignment { get; private set; }

        /// <summary>是否解析富文本标签。</summary>
        public BindableProperty<bool> RichText { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Text = CreateProperty(TextValue ?? string.Empty, (oldValue, newValue) => ApplyText(newValue));
            FormattedText = CreateProperty<FormattedText>(default, (oldValue, newValue) => ApplyFormattedText(newValue));
            Content = CreateProperty<object>(TextValue ?? string.Empty, (oldValue, newValue) => Text.Value = ConvertContent(newValue));
            FontSize = CreateProperty(FontSizeValue, (oldValue, newValue) => FontSizeValue = newValue);
            Alignment = CreateProperty(AlignmentValue, (oldValue, newValue) => AlignmentValue = newValue);
            RichText = CreateProperty(RichTextValue, (oldValue, newValue) => RichTextValue = newValue);
        }

        /// <summary>将普通文本应用到被适配组件。</summary>
        protected virtual void ApplyText(string value)
        {
            TextValue = value ?? string.Empty;
        }

        /// <summary>将格式化文本应用到被适配组件。</summary>
        protected virtual void ApplyFormattedText(FormattedText value)
        {
            TextValue = value.ToString();
        }

        static string ConvertContent(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.CurrentCulture)
                : value.ToString();
        }
    }

    /// <summary>
    /// 为自定义文本后端提供强类型组件缓存和 Graphic 适配。
    /// </summary>
    /// <typeparam name="TText">Prefab 中预置的文本图形组件类型。</typeparam>
    public abstract class TextElement<TText> : TextElement where TText : MaskableGraphic
    {
        /// <summary>获取当前 Element 适配的强类型文本组件。</summary>
        protected TText Component { get; private set; }

        /// <inheritdoc />
        protected sealed override MaskableGraphic GraphicComponent => Component;

        protected override void OnInitialize()
        {
            Component = GetComponent<TText>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TText).FullName}'.");
            }

            TrackCleanup(() => Component = null);
            base.OnInitialize();
        }
    }
}
