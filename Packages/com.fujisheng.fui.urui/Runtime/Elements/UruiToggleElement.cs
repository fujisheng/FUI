using FUI.Binding;
using FUI.Rendering;

namespace FUI.Rendering.URUI
{
    public class UruiToggleElement : UruiValueElement<bool>, IToggleElement, IBoolValueElement
    {
        readonly global::URUI.Controls.ToggleVisual visual;
        public UruiToggleElement(global::URUI.Elements.Element element, string name) : this(element, name, null) { }
        public UruiToggleElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.ToggleVisual visual)
            : base(element, name, Source(element, visual)) { this.visual = visual; }
        static global::URUI.Controls.INotifyValueChanged<bool> Source(global::URUI.Elements.Element element, global::URUI.Controls.ToggleVisual visual)
        {
            if (visual != null && !ReferenceEquals(visual.Control, element)) { throw new System.ArgumentException("开关外观必须使用同一个输入节点。", nameof(visual)); }
            return visual ?? element as global::URUI.Controls.INotifyValueChanged<bool>;
        }
        public BindableProperty<bool> IsOn => Value;
        public override void Dispose() { visual?.Dispose(); base.Dispose(); }
    }
}
