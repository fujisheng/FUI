using FUI.Binding;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>按钮控件契约的纯托管实现，区分父视图遮挡与自身状态。</summary>
    public sealed class HeadlessButtonElement : HeadlessElement, IButtonElement
    {
        public HeadlessButtonElement(string name) : base(name) { }
        public Command Click { get; } = new Command();
        public BindableProperty<bool> Interactable { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public void Input()
        {
            if (CanInput && Visible.Value && Interactable.Value) { Click.Invoke(); }
        }
        protected override void OnDispose()
        {
            Click.Dispose();Interactable.Dispose();Visible.Dispose();
        }
    }
}
