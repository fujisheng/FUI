using FUI.Binding;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>记录纹理键与图形状态，不额外发明资源加载器。</summary>
    public sealed class HeadlessRawImageElement : HeadlessElement, IRawImageElement
    {
        public HeadlessRawImageElement(string name) : base(name) { }
        public BindableProperty<string> TextureSource { get; } = new BindableProperty<string>();
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<Rgba> Tint { get; } = new BindableProperty<Rgba>();
        protected override void OnDispose() { TextureSource.Dispose(); Visible.Dispose(); Tint.Dispose(); }
    }
    /// <summary>接收后端滚动事件，不模拟布局/惯性算法；事件重复与属性同值去重分离。</summary>
    public sealed class HeadlessScrollViewElement : HeadlessElement, IScrollViewElement
    {
        public HeadlessScrollViewElement(string name) : base(name) { }
        public BindableProperty<ScrollPoint> NormalizedScroll { get; } = new BindableProperty<ScrollPoint>(new ScrollPoint(0,1));
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Horizontal { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Vertical { get; } = new BindableProperty<bool>(true);
        public Command<ScrollPoint> ScrollChanged { get; } = new Command<ScrollPoint>();
        public void InputScroll(ScrollPoint value)
        {
            if (!CanInput || !Visible.Value) { return; }
            NormalizedScroll.Value = value;
            ScrollChanged.Invoke(value);
        }
        protected override void OnDispose() { NormalizedScroll.Dispose(); Visible.Dispose(); Horizontal.Dispose(); Vertical.Dispose(); ScrollChanged.Dispose(); }
    }
}
