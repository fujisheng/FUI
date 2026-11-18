using FUI.Binding;
using FUI.Rendering;

namespace FUI.Testing
{
    /// <summary>记录节点自身可见性，不模拟布局或视觉效果。</summary>
    public sealed class HeadlessVisibilityElement : HeadlessElement, IVisibilityElement
    {
        public HeadlessVisibilityElement(string name) : base(name) { }
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>();
        protected override void OnDispose() { Visible.Dispose(); }
    }

    /// <summary>记录资源键及真实VM意图，不加载资源、创建子投影或拥有VM。</summary>
    public sealed class HeadlessDynamicElement : HeadlessElement, IDynamicViewElement
    {
        public HeadlessDynamicElement(string name) : base(name) { }
        public BindableProperty<string> Source { get; } = new BindableProperty<string>("");
        public BindableProperty<ObservableObject> ViewModel { get; } = new BindableProperty<ObservableObject>();
        protected override void OnDispose() { Source.Dispose(); ViewModel.Dispose(); }
    }
}
