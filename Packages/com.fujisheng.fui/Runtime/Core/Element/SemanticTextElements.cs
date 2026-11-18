using System;
using System.Collections.Generic;
using FUI.Binding;
namespace FUI.Rendering
{
    /// <summary>与原Unity TextAnchor九个数值对应的中性对齐值。</summary>
    public enum TextAlignment
    { UpperLeft=0,UpperCenter=1,UpperRight=2,MiddleLeft=3,MiddleCenter=4,MiddleRight=5,LowerLeft=6,LowerCenter=7,LowerRight=8 }
    /// <summary>格式化文本的只读语义值；后端保留其原格式化与本地化流程。</summary>
    public interface IFormattedTextValue
    { string Format { get; } IReadOnlyList<object> Arguments { get; } IFormatProvider FormatProvider { get; } }
    /// <summary>无后端程序集的文本请求；只保存参数，不执行翻译或绘制。</summary>
    public readonly struct FormattedTextRequest : IFormattedTextValue,IEquatable<FormattedTextRequest>
    {
        readonly object[] arguments;
        public string Format { get; }
        public IReadOnlyList<object> Arguments => arguments ?? Array.Empty<object>();
        public IFormatProvider FormatProvider { get; }
        public FormattedTextRequest(string format,params object[] arguments) : this(format,null,arguments) { }
        public FormattedTextRequest(string format,IFormatProvider provider,params object[] arguments)
        { Format=format;FormatProvider=provider;this.arguments=arguments==null||arguments.Length==0 ? Array.Empty<object>() : (object[])arguments.Clone(); }
        public bool Equals(FormattedTextRequest other)
        {
            if (!string.Equals(Format,other.Format,StringComparison.Ordinal)||!Equals(FormatProvider,other.FormatProvider)||Arguments.Count!=other.Arguments.Count) { return false; }
            for(var i=0;i<Arguments.Count;i++) { if(!Equals(Arguments[i],other.Arguments[i])) { return false; } }return true;
        }
        public override bool Equals(object value) => value is FormattedTextRequest other && Equals(other);
        public override int GetHashCode() { unchecked {var hash=(Format?.GetHashCode() ?? 0)*397 ^ (FormatProvider?.GetHashCode() ?? 0);foreach(var argument in Arguments) {hash=hash*397^(argument?.GetHashCode() ?? 0);}return hash;} }
        public static bool operator ==(FormattedTextRequest left,FormattedTextRequest right) => left.Equals(right);
        public static bool operator !=(FormattedTextRequest left,FormattedTextRequest right) => !left.Equals(right);
    }
    /// <summary>提供文本对齐值，不计算排版。</summary>
    public interface ITextAlignmentElement : IElement { BindableProperty<TextAlignment> TextAlignment { get; } }
    /// <summary>传递格式与参数请求；不规定实际本地化或字体。</summary>
    public interface IFormattedTextElement : IElement { BindableProperty<IFormattedTextValue> FormattedTextValue { get; } }
}
