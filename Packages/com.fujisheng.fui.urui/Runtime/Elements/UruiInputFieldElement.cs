using System;
using FUI.Binding;
using FUI.Rendering;
using URUI.Controls;
using URUI.Events;
namespace FUI.Rendering.URUI
{
    /// <summary>文本输入的静默模型更新、真实用户变更、失焦和提交事件。</summary>
    public sealed class UruiInputFieldElement : UruiValueElement<string>, IInputFieldElement
    {
        readonly TextField field;
        readonly IDisposable blur;
        public UruiInputFieldElement(global::URUI.Elements.Element element, string name) : base(element, name)
        {
            field = element as TextField ?? throw new ArgumentException("输入适配器需要 input 或 textarea 文本控件。", nameof(element));
            ReadOnly = new BindableProperty<bool>(field.IsReadOnly, (_, value) => field.IsReadOnly = value);
            CharacterLimit = new BindableProperty<int>(field.MaxLength, (_, value) => Configure(() => field.MaxLength = value));
            CaretBlinkRate = new BindableProperty<float>(field.CaretBlinkRate, (_, value) => field.CaretBlinkRate = value);
            CaretWidth = new BindableProperty<int>(field.CaretWidth, (_, value) => field.CaretWidth = value);
            EndEdit = new Command<string>(); Submit = new Command<string>();
            field.Submitted += OnSubmit;
            blur = field.RegisterCallback<BlurEvent>(evt => { if (ReferenceEquals(evt.Target, field)) { EndEdit.Invoke(field.Value); } });
        }
        public BindableProperty<string> Text => Value;
        public BindableProperty<bool> ReadOnly { get; }
        public BindableProperty<int> CharacterLimit { get; }
        public BindableProperty<float> CaretBlinkRate { get; }
        public BindableProperty<int> CaretWidth { get; }
        public Command<string> EndEdit { get; }
        public Command<string> Submit { get; }
        void OnSubmit(string value) { Submit.Invoke(value); field.Blur(); }
        public override void Dispose()
        {
            field.Submitted -= OnSubmit; blur.Dispose();
            ReadOnly.Dispose(); CharacterLimit.Dispose(); CaretBlinkRate.Dispose(); CaretWidth.Dispose(); EndEdit.Dispose(); Submit.Dispose(); base.Dispose();
        }
    }
}
