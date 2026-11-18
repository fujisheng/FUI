using FUI.Binding;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>管理显示值和动作意图，不加载图片或播放实际动画。</summary>
    public sealed class HeadlessGraphicElement : HeadlessElement,IImageElement,ITextElement,ITextContentElement,IClickElement,IVisibilityElement,IImageSourceElement,IFillElement,IColorElement,IAnimationElement,ITextAlignmentElement,IFormattedTextElement,ILocalPositionElement,ILocalScaleElement,IDynamicViewElement,IMapImageElement,IHoleMaskElement
    {
        public HeadlessGraphicElement(string name) : base(name)
        { Content=new BindableProperty<object>("",(oldValue,newValue)=>Text.Value=newValue is System.IFormattable formattable?formattable.ToString(null,System.Globalization.CultureInfo.CurrentCulture):newValue?.ToString()??""); }
        public BindableProperty<float> FontSize { get; } = new BindableProperty<float>(16);
        public BindableProperty<bool> RichText { get; } = new BindableProperty<bool>(true);
        public BindableProperty<string> Text { get; } = new BindableProperty<string>("");
        public BindableProperty<object> Content { get; }
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<string> SpriteSource { get; } = new BindableProperty<string>();
        public BindableProperty<float> FillAmount { get; } = new BindableProperty<float>();
        public BindableProperty<Rgba> Tint { get; } = new BindableProperty<Rgba>();
        public BindableProperty<bool> PreserveAspect { get; } = new BindableProperty<bool>();
        public BindableProperty<bool> FillCenter { get; } = new BindableProperty<bool>();
        public BindableProperty<int> FillOrigin { get; } = new BindableProperty<int>();
        public BindableProperty<bool> FillClockwise { get; } = new BindableProperty<bool>();
        public BindableProperty<int?> PlayingIndex { get; } = new BindableProperty<int?>();
        public Command<int> OnPlayComplete { get; } = new Command<int>();
        public BindableProperty<TextAlignment> TextAlignment { get; } = new BindableProperty<TextAlignment>();
        public BindableProperty<IFormattedTextValue> FormattedTextValue { get; } = new BindableProperty<IFormattedTextValue>();
        public void InputAlignment(TextAlignment value) { if(CanInput && Visible.Value) { TextAlignment.Value=value; } }
        public BindableProperty<SpatialValue> LocalPositionValue { get; } = new BindableProperty<SpatialValue>();
        public BindableProperty<SpatialValue> LocalScaleValue { get; } = new BindableProperty<SpatialValue>(SpatialValue.one);
        public BindableProperty<string> MapName { get; } = new BindableProperty<string>();
        public BindableProperty<DisplayRect?> Hole { get; } = new BindableProperty<DisplayRect?>();
        public BindableProperty<string> Source { get; } = new BindableProperty<string>();
        public BindableProperty<ObservableObject> ViewModel { get; } = new BindableProperty<ObservableObject>();
        public Command Click { get; } = new Command();
        public void Input() { if (CanInput && Visible.Value) { Click.Invoke(); } }
        public void CompleteAnimation(int index) { if (CanInput) { OnPlayComplete.Invoke(index); } }
        protected override void OnDispose()
        { PreserveAspect.Dispose();FillCenter.Dispose();FillOrigin.Dispose();FillClockwise.Dispose();Text.Dispose();Content.Dispose();FontSize.Dispose();RichText.Dispose();Visible.Dispose();SpriteSource.Dispose();FillAmount.Dispose();Tint.Dispose();PlayingIndex.Dispose();OnPlayComplete.Dispose();TextAlignment.Dispose();FormattedTextValue.Dispose();LocalPositionValue.Dispose();LocalScaleValue.Dispose();Source.Dispose();ViewModel.Dispose();MapName.Dispose();Hole.Dispose();Click.Dispose(); }
    }
}
