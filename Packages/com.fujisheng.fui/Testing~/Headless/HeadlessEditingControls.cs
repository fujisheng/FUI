using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Rendering;

namespace FUI.Testing
{
    /// <summary>与视图所有权独立的控件自身门控；覆盖解除不覆盖自身禁用状态。</summary>
    public abstract class HeadlessSelectableControl : HeadlessElement, ISelectableElement
    {
        protected HeadlessSelectableControl(string name) : base(name) { }
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Interactable { get; } = new BindableProperty<bool>(true);
        protected bool CanInteract => CanInput && Visible.Value && Interactable.Value;
        protected override void OnDispose() { Visible.Dispose(); Interactable.Dispose(); }
    }

    /// <summary>接收后端选项事件；程序属性赋值不发用户命令，也不夹紧多选整数掩码。</summary>
    public class HeadlessDropdownElement : HeadlessSelectableControl, IDropdownElement
    {
        public HeadlessDropdownElement(string name) : base(name) { }
        public BindableProperty<int> Value { get; } = new BindableProperty<int>();
        public BindableProperty<IReadOnlyList<string>> Options { get; } = new BindableProperty<IReadOnlyList<string>>(Array.Empty<string>());
        public BindableProperty<float> AlphaFadeSpeed { get; } = new BindableProperty<float>();
        public Command<int> OnValueChanged { get; } = new Command<int>();
        public Command<int> ValueChanged => OnValueChanged;
        public void Input(int value)
        {
            if (!CanInteract) { return; }
            Value.Value = value;
            OnValueChanged.Invoke(value);
        }
        protected override void OnDispose()
        {
            Value.Dispose(); Options.Dispose(); AlphaFadeSpeed.Dispose(); OnValueChanged.Dispose(); base.OnDispose();
        }
    }

    /// <summary>只在支持多选的后端公开MultiSelect，Value继续使用同一整数属性。</summary>
    public sealed class HeadlessMultiSelectDropdownElement : HeadlessDropdownElement, IMultiSelectDropdownElement
    {
        public HeadlessMultiSelectDropdownElement(string name) : base(name) { }
        public BindableProperty<bool> MultiSelect { get; } = new BindableProperty<bool>();
        protected override void OnDispose() { MultiSelect.Dispose(); base.OnDispose(); }
    }

    /// <summary>模拟输入事件而不模拟键盘校验算法；结束/提交负载不擅自改变当前文本。</summary>
    public sealed class HeadlessInputFieldElement : HeadlessSelectableControl, IInputFieldElement
    {
        public HeadlessInputFieldElement(string name) : base(name) { }
        public BindableProperty<string> Text { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<bool> ReadOnly { get; } = new BindableProperty<bool>();
        public BindableProperty<int> CharacterLimit { get; } = new BindableProperty<int>();
        public BindableProperty<float> CaretBlinkRate { get; } = new BindableProperty<float>();
        public BindableProperty<int> CaretWidth { get; } = new BindableProperty<int>();
        public Command<string> OnValueChanged { get; } = new Command<string>();
        public Command<string> OnEndEdit { get; } = new Command<string>();
        public Command<string> OnSubmit { get; } = new Command<string>();
        public Command<string> ValueChanged => OnValueChanged;
        public Command<string> EndEdit => OnEndEdit;
        public Command<string> Submit => OnSubmit;
        public void Input(string value)
        {
            if (!CanInteract || ReadOnly.Value) { return; }
            var normalized = value ?? string.Empty;
            Text.Value = normalized;
            OnValueChanged.Invoke(normalized);
        }
        public void EndEditing(string value) { if (CanInteract) { OnEndEdit.Invoke(value ?? string.Empty); } }
        public void SubmitInput(string value) { if (CanInteract) { OnSubmit.Invoke(value ?? string.Empty); } }
        protected override void OnDispose()
        {
            Text.Dispose(); ReadOnly.Dispose(); CharacterLimit.Dispose(); CaretBlinkRate.Dispose(); CaretWidth.Dispose();
            OnValueChanged.Dispose(); OnEndEdit.Dispose(); OnSubmit.Dispose(); base.OnDispose();
        }
    }

    /// <summary>模拟滚动条后端值事件；保留事件负载，不另实现Unity离散化/夹紧算法。</summary>
    public sealed class HeadlessScrollbarElement : HeadlessSelectableControl, IScrollbarElement
    {
        public HeadlessScrollbarElement(string name) : base(name) { }
        public BindableProperty<float> Value { get; } = new BindableProperty<float>();
        public BindableProperty<float> Size { get; } = new BindableProperty<float>(1);
        public BindableProperty<int> NumberOfSteps { get; } = new BindableProperty<int>();
        public Command<float> OnValueChanged { get; } = new Command<float>();
        public Command<float> ValueChanged => OnValueChanged;
        public void Input(float value)
        {
            if (!CanInteract) { return; }
            Value.Value = value;
            OnValueChanged.Invoke(value);
        }
        protected override void OnDispose()
        {
            Value.Dispose(); Size.Dispose(); NumberOfSteps.Dispose(); OnValueChanged.Dispose(); base.OnDispose();
        }
    }
}
