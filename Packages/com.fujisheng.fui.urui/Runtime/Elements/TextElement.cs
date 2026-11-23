using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FUI.Binding;
using FUI.Rendering;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 文本适配器：URUI Label/Element 的纯文本与字号/对齐。
    /// </summary>
    public class TextElement : ElementAdapter, ITextElement, ITextContentElement, ITextAlignmentElement, IFormattedTextElement
    {
        enum RichTagKind : byte
        {
            None,
            Bold,
            Italic,
            Size,
            Color,
            Break
        }

        readonly struct RichTagToken
        {
            internal readonly int Start;
            internal readonly int End;
            internal readonly RichTagKind Kind;
            internal readonly bool Closing;
            internal readonly float SizeValue;
            internal readonly UnityEngine.Color ColorValue;
            internal readonly bool ValidValue;

            internal RichTagToken(int start, int end, RichTagKind kind, bool closing, float sizeValue = 0, UnityEngine.Color colorValue = default, bool validValue = true)
            {
                Start = start;
                End = end;
                Kind = kind;
                Closing = closing;
                SizeValue = sizeValue;
                ColorValue = colorValue;
                ValidValue = validValue;
            }
        }

        string lastText;
        readonly Element textNode;
        readonly global::URUI.Controls.TextLayout textLayout;
        readonly global::URUI.Controls.TextFit textFit;
        BindableProperty<float> fontSize;
        BindableProperty<bool> richText;
        BindableProperty<global::FUI.Rendering.TextAlignment> textAlignment;
        BindableProperty<IFormattedTextValue> formattedTextValue;
        BindableProperty<object> content;
        List<RichTagToken> richTokenScratch;
        Stack<(RichTagKind kind, Element parent)> richStackScratch;

        /// <summary>
        /// 绑定作者文本布局；成功构造后由适配器持有并释放可选的缩字控制器。
        /// </summary>
        public TextElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.TextLayout textLayout = null, global::URUI.Controls.TextFit textFit = null) : base(ValidateHost(element, textLayout, textFit), name)
        {
            this.textLayout = textLayout;
            this.textFit = textFit;
            textNode = textLayout?.Content ?? element;
            if (!ReferenceEquals(element.Document, textNode.Document))
            {
                throw new ArgumentException("文本布局必须属于同一文档。", nameof(textLayout));
            }
            lastText = textNode.Text ?? string.Empty;
            Text = new BindableProperty<string>(lastText, (_, value) => ApplyText(value));
        }

        static Element ValidateHost(Element element, global::URUI.Controls.TextLayout layout, global::URUI.Controls.TextFit fit)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (layout != null && !ReferenceEquals(layout.Host, element))
            {
                throw new ArgumentException("文本适配器必须绑定到其布局的外层节点。", nameof(layout));
            }
            if (fit != null && (!ReferenceEquals(fit.Host, element) || !ReferenceEquals(fit.Content, layout?.Content ?? element)))
            {
                throw new ArgumentException("缩字控制器必须匹配相同的文本布局。", nameof(fit));
            }
            return element;
        }

        public BindableProperty<string> Text
        {
            get;
        }
        public BindableProperty<float> FontSize
            => fontSize ??= new BindableProperty<float>(0f, (_, value) => Element.Style.SetDensityPixels("font-size", value));
        public BindableProperty<bool> RichText
            => richText ??= new BindableProperty<bool>(true, (_, value) => RenderText(lastText));
        public BindableProperty<global::FUI.Rendering.TextAlignment> TextAlignment
            => textAlignment ??= new BindableProperty<global::FUI.Rendering.TextAlignment>(
                textLayout != null ? (global::FUI.Rendering.TextAlignment)textLayout.Alignment : global::FUI.Rendering.TextAlignment.MiddleLeft,
                (_, value) => ApplyAlignment(value));
        public BindableProperty<IFormattedTextValue> FormattedTextValue
            => formattedTextValue ??= new BindableProperty<IFormattedTextValue>(null, (_, value) => ApplyFormatted(value));
        public BindableProperty<object> Content
            => content ??= new BindableProperty<object>(null, (_, value) => ApplyText(value?.ToString() ?? string.Empty));

        /// <summary>
        /// 项目可在此扩展源文本转换，例如本地化；通用后端不依赖项目文本服务。
        /// </summary>
        protected virtual void ApplyText(string value)
        {
            value ??= string.Empty;
            lastText = value;
            RenderText(value);
        }

        void RenderText(string value)
        {
            if ((richText?.Value ?? true) && value.IndexOf('<') >= 0 && TryScanRichTags(value, out var tokens))
            {
                ApplyRichText(value, tokens);
            }
            else
            {
                textNode.Text = value;
            }
            textFit?.Refresh();
        }

        bool TryScanRichTags(string value, out List<RichTagToken> tokens)
        {
            tokens = null;
            var span = value.AsSpan();
            var index = 0;
            while (index < span.Length)
            {
                var open = span.Slice(index).IndexOf('<');
                if (open < 0)
                {
                    break;
                }
                open += index;
                var close = span.Slice(open + 1).IndexOf('>');
                if (close < 0)
                {
                    break;
                }
                close += open + 1;
                if (TryParseRichTag(span.Slice(open + 1, close - open - 1), open, close + 1, out var token))
                {
                    if (tokens == null)
                    {
                        richTokenScratch ??= new List<RichTagToken>(8);
                        richTokenScratch.Clear();
                        tokens = richTokenScratch;
                    }
                    tokens.Add(token);
                    index = close + 1;
                }
                else
                {
                    index = open + 1;
                }
            }
            return tokens != null && tokens.Count > 0;
        }

        static bool TryParseRichTag(ReadOnlySpan<char> inner, int start, int end, out RichTagToken token)
        {
            token = default;
            if (inner.Length == 0)
            {
                return false;
            }
            if (inner[0] == '/')
            {
                var name = inner.Slice(1);
                if (name.Length == 1)
                {
                    var c = char.ToLowerInvariant(name[0]);
                    if (c == 'b')
                    {
                        token = new RichTagToken(start, end, RichTagKind.Bold, closing: true);
                        return true;
                    }
                    if (c == 'i')
                    {
                        token = new RichTagToken(start, end, RichTagKind.Italic, closing: true);
                        return true;
                    }
                }
                else if (name.Equals("size".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    token = new RichTagToken(start, end, RichTagKind.Size, closing: true);
                    return true;
                }
                else if (name.Equals("color".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    token = new RichTagToken(start, end, RichTagKind.Color, closing: true);
                    return true;
                }
                return false;
            }
            if (inner.Length == 1)
            {
                var c = char.ToLowerInvariant(inner[0]);
                if (c == 'b')
                {
                    token = new RichTagToken(start, end, RichTagKind.Bold, closing: false);
                    return true;
                }
                if (c == 'i')
                {
                    token = new RichTagToken(start, end, RichTagKind.Italic, closing: false);
                    return true;
                }
                return false;
            }
            if (inner.Length >= 2 && (inner[0] == 'b' || inner[0] == 'B') && (inner[1] == 'r' || inner[1] == 'R'))
            {
                var p = 2;
                while (p < inner.Length && char.IsWhiteSpace(inner[p]))
                {
                    p++;
                }
                if (p < inner.Length && inner[p] == '/')
                {
                    p++;
                }
                if (p == inner.Length)
                {
                    token = new RichTagToken(start, end, RichTagKind.Break, closing: false);
                    return true;
                }
                return false;
            }
            if (inner.Length > 4 && inner.Slice(0, 4).Equals("size".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                var p = 4;
                while (p < inner.Length && char.IsWhiteSpace(inner[p]))
                {
                    p++;
                }
                if (p >= inner.Length || inner[p] != '=')
                {
                    return false;
                }
                p++;
                while (p < inner.Length && char.IsWhiteSpace(inner[p]))
                {
                    p++;
                }
                var arg = inner.Slice(p);
                if (!IsDecimalNumber(arg))
                {
                    return false;
                }
                var parsed = float.TryParse(arg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var size) && size > 0 && !float.IsInfinity(size);
                token = new RichTagToken(start, end, RichTagKind.Size, closing: false, sizeValue: size, validValue: parsed);
                return true;
            }
            if (inner.Length > 5 && inner.Slice(0, 5).Equals("color".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                var p = 5;
                while (p < inner.Length && char.IsWhiteSpace(inner[p]))
                {
                    p++;
                }
                if (p >= inner.Length || inner[p] != '=')
                {
                    return false;
                }
                p++;
                while (p < inner.Length && char.IsWhiteSpace(inner[p]))
                {
                    p++;
                }
                var arg = inner.Slice(p);
                if (!IsColorToken(arg))
                {
                    return false;
                }
                var parsed = UnityEngine.ColorUtility.TryParseHtmlString(arg.ToString(), out var color);
                token = new RichTagToken(start, end, RichTagKind.Color, closing: false, colorValue: color, validValue: parsed);
                return true;
            }
            return false;
        }

        static bool IsDecimalNumber(ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
            {
                return false;
            }
            var i = 0;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                i++;
            }
            if (i == 0)
            {
                return false;
            }
            if (i == text.Length)
            {
                return true;
            }
            if (text[i] != '.')
            {
                return false;
            }
            i++;
            var fracStart = i;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                i++;
            }
            return i > fracStart && i == text.Length;
        }

        static bool IsColorToken(ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
            {
                return false;
            }
            if (text[0] == '#')
            {
                var hexLen = text.Length - 1;
                if (hexLen < 3 || hexLen > 8)
                {
                    return false;
                }
                for (var i = 1; i < text.Length; i++)
                {
                    var c = text[i];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    {
                        return false;
                    }
                }
                return true;
            }
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                {
                    return false;
                }
            }
            return true;
        }

        void ApplyRichText(string value, List<RichTagToken> tokens)
        {
            using var update = Element.Document.BeginUpdate();
            textNode.Text = string.Empty;
            var current = textNode;
            richStackScratch ??= new Stack<(RichTagKind kind, Element parent)>();
            richStackScratch.Clear();
            var opened = richStackScratch;
            var position = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Start > position)
                {
                    AppendText(current, value.Substring(position, token.Start - position));
                }
                position = token.End;
                if (token.Closing)
                {
                    if (opened.Count > 0 && opened.Peek().kind == token.Kind)
                    {
                        current = opened.Pop().parent;
                    }
                    else
                    {
                        AppendText(current, value.Substring(token.Start, token.End - token.Start));
                    }
                    continue;
                }
                if (token.Kind == RichTagKind.Break)
                {
                    current.Add(Stage.CreateHost(Element.Document, "text-break"));
                    continue;
                }
                if (!token.ValidValue)
                {
                    AppendText(current, value.Substring(token.Start, token.End - token.Start));
                    continue;
                }
                var run = Stage.CreateHost(Element.Document, "text-run");
                if (token.Kind == RichTagKind.Bold || token.Kind == RichTagKind.Italic)
                {
                    run.AddToClassList(token.Kind == RichTagKind.Bold ? "text-bold" : "text-italic");
                }
                else if (token.Kind == RichTagKind.Size)
                {
                    run.Style.SetPixels("font-size", token.SizeValue);
                }
                else if (token.Kind == RichTagKind.Color)
                {
                    run.Style.SetColor("color", token.ColorValue);
                }
                current.Add(run);
                opened.Push((token.Kind, current));
                current = run;
            }
            if (position < value.Length)
            {
                AppendText(current, value.Substring(position));
            }
            opened.Clear();
            tokens.Clear();
        }

        void AppendText(Element parent, string value)
        {
            if (value.Length == 0)
            {
                return;
            }
            if (!ReferenceEquals(parent, textNode) && parent.ChildCount == 0)
            {
                parent.Text = value;
                return;
            }
            var run = Stage.CreateHost(Element.Document, "text-run");
            run.Text = value;
            parent.Add(run);
        }

        void ApplyAlignment(global::FUI.Rendering.TextAlignment alignment)
        {
            if (textLayout != null)
            {
                textLayout.Alignment = (UnityEngine.TextAnchor)alignment;
                return;
            }
            var value = alignment switch
            {
                global::FUI.Rendering.TextAlignment.UpperLeft or global::FUI.Rendering.TextAlignment.MiddleLeft or global::FUI.Rendering.TextAlignment.LowerLeft => "left",
                global::FUI.Rendering.TextAlignment.UpperRight or global::FUI.Rendering.TextAlignment.MiddleRight or global::FUI.Rendering.TextAlignment.LowerRight => "right",
                _ => "center",
            };
            Element.Style.Set("text-align", value);
        }

        /// <summary>
        /// 项目可先转换格式模板，再保留参数和文化信息调用通用文本绘制。
        /// </summary>
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
            var args = value.Arguments as object[] ?? value.Arguments.ToArray();
            ApplyText(string.Format(value.FormatProvider, value.Format, args));
        }

        public override void Dispose()
        {
            textFit?.Dispose();
            Text?.Dispose();
            fontSize?.Dispose();
            richText?.Dispose();
            textAlignment?.Dispose();
            formattedTextValue?.Dispose();
            content?.Dispose();
            base.Dispose();
        }
    }
}
