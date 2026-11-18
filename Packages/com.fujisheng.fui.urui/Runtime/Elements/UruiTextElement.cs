using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FUI.Binding;
using FUI.Rendering;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>文本适配器：URUI Label/Element 的纯文本与字号/对齐。</summary>
    public class UruiTextElement : UruiElement, ITextElement, ITextContentElement, ITextAlignmentElement, IFormattedTextElement
    {
        string lastText;
        readonly Element textNode;
        readonly global::URUI.Controls.TextLayout textLayout;
        readonly global::URUI.Controls.TextFit textFit;
        static readonly Regex richTags = new Regex(@"<(?:(?<close>/)(?<end>b|i|size|color)|(?<simple>b|i)|size\s*=\s*(?<size>[0-9]+(?:\.[0-9]+)?)|color\s*=\s*(?<color>#[0-9a-f]{3,8}|[a-z]+)|(?<br>br\s*/?))>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>绑定作者文本布局；成功构造后由适配器持有并释放可选的缩字控制器。</summary>
        public UruiTextElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.TextLayout textLayout = null, global::URUI.Controls.TextFit textFit = null) : base(ValidateHost(element, textLayout, textFit), name)
        {
            this.textLayout = textLayout;
            this.textFit = textFit;
            textNode = textLayout?.Content ?? element;
            if (!ReferenceEquals(element.Document, textNode.Document)) { throw new ArgumentException("文本布局必须属于同一文档。", nameof(textLayout)); }
            lastText = textNode.Text ?? string.Empty;
            Text = new BindableProperty<string>(lastText, (_, value) => ApplyText(value));
            FontSize = new BindableProperty<float>(0f, (_, value) => element.Style.SetDensityPixels("font-size", value));
            RichText = new BindableProperty<bool>(true, (_, value) => RenderText(lastText));
            TextAlignment = new BindableProperty<global::FUI.Rendering.TextAlignment>(textLayout != null ? (global::FUI.Rendering.TextAlignment)textLayout.Alignment : global::FUI.Rendering.TextAlignment.MiddleLeft, (_, value) => ApplyAlignment(value));
            FormattedTextValue = new BindableProperty<IFormattedTextValue>(null, (_, value) => ApplyFormatted(value));
            Content = new BindableProperty<object>(null, (_, value) => ApplyText(value?.ToString() ?? string.Empty));
        }

        static Element ValidateHost(Element element, global::URUI.Controls.TextLayout layout, global::URUI.Controls.TextFit fit)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            if (layout != null && !ReferenceEquals(layout.Host, element)) { throw new ArgumentException("文本适配器必须绑定到其布局的外层节点。", nameof(layout)); }
            if (fit != null && (!ReferenceEquals(fit.Host, element) || !ReferenceEquals(fit.Content, layout?.Content ?? element)))
            { throw new ArgumentException("缩字控制器必须匹配相同的文本布局。", nameof(fit)); }
            return element;
        }

        public BindableProperty<string> Text { get; }
        public BindableProperty<float> FontSize { get; }
        public BindableProperty<bool> RichText { get; }
        public BindableProperty<global::FUI.Rendering.TextAlignment> TextAlignment { get; }
        public BindableProperty<IFormattedTextValue> FormattedTextValue { get; }
        public BindableProperty<object> Content { get; }

        /// <summary>项目可在此扩展源文本转换，例如本地化；通用后端不依赖项目文本服务。</summary>
        protected virtual void ApplyText(string value)
        {
            value ??= string.Empty;
            lastText = value;
            RenderText(value);
        }

        void RenderText(string value)
        {
            if ((RichText?.Value ?? true) && richTags.IsMatch(value))
            {
                ApplyRichText(value);
            }
            else { textNode.Text = value; }
            textFit?.Refresh();
        }

        void ApplyRichText(string value)
        {
            textNode.Text = string.Empty;
            var current = textNode;
            var opened = new Stack<(string kind, Element parent)>();
            var position = 0;
            foreach (Match match in richTags.Matches(value))
            {
                AppendText(current, value.Substring(position, match.Index - position));
                position = match.Index + match.Length;
                if (match.Groups["close"].Success)
                {
                    var tag = match.Groups["end"].Value.ToLowerInvariant();
                    if (opened.Count > 0 && opened.Peek().kind == tag)
                    {
                        current = opened.Pop().parent;
                    }
                    else { AppendText(current, match.Value); }
                    continue;
                }
                if (match.Groups["br"].Success)
                {
                    current.Add(UruiStage.CreateHost(Element.Document, "text-break"));
                    continue;
                }
                var kind = match.Groups["simple"].Value.ToLowerInvariant();
                var run = UruiStage.CreateHost(Element.Document, "text-run");
                if (kind == "b" || kind == "i") { run.AddToClassList(kind == "b" ? "text-bold" : "text-italic"); }
                else if (match.Groups["size"].Success && float.TryParse(match.Groups["size"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var size) && size > 0 && !float.IsInfinity(size))
                {
                    kind = "size";
                    run.Style.SetPixels("font-size", size);
                }
                else if (match.Groups["color"].Success && UnityEngine.ColorUtility.TryParseHtmlString(match.Groups["color"].Value, out var color))
                {
                    kind = "color";
                    run.Style.SetColor("color", color);
                }
                else
                {
                    run.Destroy();
                    AppendText(current, match.Value);
                    continue;
                }
                current.Add(run);
                opened.Push((kind, current));
                current = run;
            }
            AppendText(current, value.Substring(position));
        }

        void AppendText(Element parent, string value)
        {
            if (value.Length == 0) return;
            if (!ReferenceEquals(parent, textNode) && parent.ChildCount == 0) { parent.Text = value; return; }
            var run = UruiStage.CreateHost(Element.Document, "text-run");
            run.Text = value;
            parent.Add(run);
        }

        void ApplyAlignment(global::FUI.Rendering.TextAlignment alignment)
        {
            if (textLayout != null) { textLayout.Alignment = (UnityEngine.TextAnchor)alignment; return; }
            var value = alignment switch
            {
                global::FUI.Rendering.TextAlignment.UpperLeft or global::FUI.Rendering.TextAlignment.MiddleLeft or global::FUI.Rendering.TextAlignment.LowerLeft => "left",
                global::FUI.Rendering.TextAlignment.UpperRight or global::FUI.Rendering.TextAlignment.MiddleRight or global::FUI.Rendering.TextAlignment.LowerRight => "right",
                _ => "center",
            };
            Element.Style.Set("text-align", value);
        }

        /// <summary>项目可先转换格式模板，再保留参数和文化信息调用通用文本绘制。</summary>
        protected virtual void ApplyFormatted(IFormattedTextValue value)
        {
            if (value == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(value.Format))
            {
                ApplyText(string.Empty);
                return;
            }
            ApplyText(string.Format(value.FormatProvider, value.Format, value.Arguments.ToArray()));
        }

        public override void Dispose()
        {
            textFit?.Dispose();
            Text?.Dispose();
            FontSize?.Dispose();
            RichText?.Dispose();
            TextAlignment?.Dispose();
            FormattedTextValue?.Dispose();
            Content?.Dispose();
            base.Dispose();
        }
    }
}
