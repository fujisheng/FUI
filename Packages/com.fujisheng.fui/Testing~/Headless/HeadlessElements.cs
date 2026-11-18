using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Rendering;

namespace FUI.Testing
{
    /// <summary>Owns one named managed element in a testing view.</summary>
    public abstract class HeadlessElement : IElement, IDisposable
    {
        protected HeadlessElement(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Element name is required.", nameof(name)); }
            Name = name;
        }
        public string Name { get; }
        public IElement Parent => null;
        public bool IsDisposed { get; private set; }
        internal HeadlessView Owner { get; set; }
        protected bool CanInput => !IsDisposed && (Owner == null || (Owner.Visible && Owner.Interactable));
        public void Dispose()
        {
            if (IsDisposed) { return; }
            IsDisposed = true;
            OnDispose();
        }
        internal virtual void SetVisibility(bool visible) { }
        internal virtual void SetInteractable(bool interactable) { }
        protected virtual void OnDispose() { }
    }

    /// <summary>Provides a backend-independent managed text target.</summary>
    public sealed class HeadlessTextElement : HeadlessElement, ITextElement
    {
        public HeadlessTextElement(string name) : base(name) { }
        public BindableProperty<string> Text { get; } = new BindableProperty<string>("");
        public BindableProperty<float> FontSize { get; } = new BindableProperty<float>(16);
        public BindableProperty<bool> RichText { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<Rgba> Tint { get; } = new BindableProperty<Rgba>();
        public BindableProperty<TextAlignment> TextAlignment { get; } = new BindableProperty<TextAlignment>();
        public BindableProperty<IFormattedTextValue> FormattedTextValue { get; } = new BindableProperty<IFormattedTextValue>();
        protected override void OnDispose() { Text.Dispose();FontSize.Dispose();RichText.Dispose();Visible.Dispose();Tint.Dispose();TextAlignment.Dispose();FormattedTextValue.Dispose(); }
    }

    /// <summary>Provides a click command gated by its testing view.</summary>
    public sealed class HeadlessClickElement : HeadlessElement, IButtonElement
    {
        public HeadlessClickElement(string name) : base(name) { }
        public Command Click { get; } = new Command();
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Interactable { get; } = new BindableProperty<bool>(true);
        public void Input() { if (CanInput && Visible.Value && Interactable.Value) { Click.Invoke(); } }
        protected override void OnDispose() { Click.Dispose();Visible.Dispose();Interactable.Dispose(); }
    }

    /// <summary>Distinguishes simulated user value changes from programmatic writes.</summary>
    public abstract class HeadlessValueElement<T> : HeadlessElement, ISelectableElement
    {
        protected HeadlessValueElement(string name) : base(name) { }
        public BindableProperty<T> Value { get; } = new BindableProperty<T>();
        public Command<T> ValueChanged { get; } = new Command<T>();
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> Interactable { get; } = new BindableProperty<bool>(true);
        public void Input(T value)
        {
            if (!CanInput || !Visible.Value || !Interactable.Value || EqualityComparer<T>.Default.Equals(Value.Value, value)) { return; }
            Value.Value = value;
            ValueChanged.Invoke(value);
        }
        protected override void OnDispose() { Value.Dispose(); ValueChanged.Dispose();Visible.Dispose();Interactable.Dispose(); }
    }

    /// <summary>Provides a float value and user input command without a backend range.</summary>
    public sealed class HeadlessFloatElement : HeadlessValueElement<float>, ISliderElement
    {
        public HeadlessFloatElement(string name) : base(name) { }
        public BindableProperty<float> MinValue { get; } = new BindableProperty<float>(0);
        public BindableProperty<float> MaxValue { get; } = new BindableProperty<float>(1);
        public BindableProperty<bool> WholeNumbers { get; } = new BindableProperty<bool>(false);
        protected override void OnDispose() { base.OnDispose();MinValue.Dispose();MaxValue.Dispose();WholeNumbers.Dispose(); }
    }

    /// <summary>Provides a bool value and user input command.</summary>
    public sealed class HeadlessBoolElement : HeadlessValueElement<bool>, IToggleElement
    {
        public HeadlessBoolElement(string name) : base(name) { }
        public BindableProperty<bool> IsOn => Value;
    }
}
