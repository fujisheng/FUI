using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>输入内容的稳定公共类型。</summary>
    public enum InputContentType
    {
        /// <summary>允许任意输入。</summary>
        Standard,
        /// <summary>允许任意输入并启用平台自动纠正。</summary>
        Autocorrected,
        /// <summary>整数。</summary>
        IntegerNumber,
        /// <summary>小数。</summary>
        DecimalNumber,
        /// <summary>字母和数字。</summary>
        Alphanumeric,
        /// <summary>姓名。</summary>
        Name,
        /// <summary>电子邮件地址。</summary>
        EmailAddress,
        /// <summary>密码。</summary>
        Password,
        /// <summary>数字密码。</summary>
        Pin,
        /// <summary>由具体后端定义的自定义类型。</summary>
        Custom,
    }

    /// <summary>输入框的稳定公共行模式。</summary>
    public enum InputLineType
    {
        /// <summary>单行输入。</summary>
        SingleLine,
        /// <summary>多行输入，回车提交。</summary>
        MultiLineSubmit,
        /// <summary>多行输入，回车换行。</summary>
        MultiLineNewline,
    }

    /// <summary>
    /// 定义 Legacy 与 TMP 输入框共享的绑定契约。
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class InputFieldElement : SelectableElement, IInputFieldElement
    {
        bool suppressCommand;
        bool synchronizingConfiguration;

        /// <summary>读取后端文本。</summary>
        protected abstract string TextValue { get; }

        /// <summary>读取或写入后端只读状态。</summary>
        protected abstract bool ReadOnlyValue { get; set; }

        /// <summary>读取或写入后端字符限制。</summary>
        protected abstract int CharacterLimitValue { get; set; }

        /// <summary>读取或写入后端公共内容类型。</summary>
        protected abstract InputContentType ContentTypeValue { get; set; }

        /// <summary>读取或写入后端公共行模式。</summary>
        protected abstract InputLineType LineTypeValue { get; set; }

        /// <summary>读取或写入后端光标闪烁速率。</summary>
        protected abstract float CaretBlinkRateValue { get; set; }

        /// <summary>读取或写入后端光标宽度。</summary>
        protected abstract int CaretWidthValue { get; set; }

        /// <summary>读取或写入后端选区颜色。</summary>
        protected abstract Color SelectionColorValue { get; set; }

        /// <summary>以不发送 Unity 事件的方式写入文本。</summary>
        protected abstract void SetTextWithoutNotify(string value);

        /// <summary>订阅后端文本变更事件。</summary>
        protected abstract void AddValueChangedListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>取消订阅后端文本变更事件。</summary>
        protected abstract void RemoveValueChangedListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>订阅后端编辑结束事件。</summary>
        protected abstract void AddEndEditListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>取消订阅后端编辑结束事件。</summary>
        protected abstract void RemoveEndEditListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>订阅后端提交事件。</summary>
        protected abstract void AddSubmitListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>取消订阅后端提交事件。</summary>
        protected abstract void RemoveSubmitListener(UnityEngine.Events.UnityAction<string> listener);

        /// <summary>文本值。</summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>是否只读。</summary>
        public BindableProperty<bool> ReadOnly { get; private set; }

        /// <summary>最大字符数；零表示不限制。</summary>
        public BindableProperty<int> CharacterLimit { get; private set; }

        /// <summary>公共输入内容类型。</summary>
        public BindableProperty<InputContentType> ContentType { get; private set; }

        /// <summary>公共输入行模式。</summary>
        public BindableProperty<InputLineType> LineType { get; private set; }

        /// <summary>光标闪烁速率。</summary>
        public BindableProperty<float> CaretBlinkRate { get; private set; }

        /// <summary>光标宽度。</summary>
        public BindableProperty<int> CaretWidth { get; private set; }

        /// <summary>文本选区颜色。</summary>
        public BindableProperty<Color> SelectionColor { get; private set; }

        /// <summary>用户修改文本时触发。</summary>
        public Command<string> OnValueChanged { get; private set; }

        /// <summary>用户结束编辑时触发。</summary>
        public Command<string> OnEndEdit { get; private set; }

        /// <summary>用户提交输入时触发。</summary>
        public Command<string> OnSubmit { get; private set; }

        Command<string> IInputFieldElement.ValueChanged => OnValueChanged;
        Command<string> IInputFieldElement.EndEdit => OnEndEdit;
        Command<string> IInputFieldElement.Submit => OnSubmit;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Text = CreateProperty(
                TextValue ?? string.Empty,
                (oldValue, newValue) => ApplyWithoutCommand(() => SetTextWithoutNotify(Normalize(newValue))));
            ReadOnly = CreateProperty(ReadOnlyValue, (oldValue, newValue) => ApplyWithoutCommand(() => ReadOnlyValue = newValue));
            CharacterLimit = CreateProperty(CharacterLimitValue, (oldValue, newValue) => ApplyWithoutCommand(() => CharacterLimitValue = newValue));
            ContentType = CreateProperty(ContentTypeValue, (oldValue, newValue) => ApplyWithoutCommand(() =>
            {
                ContentTypeValue = newValue;
                SynchronizeConfiguration();
            }));
            LineType = CreateProperty(LineTypeValue, (oldValue, newValue) => ApplyWithoutCommand(() =>
            {
                LineTypeValue = newValue;
                SynchronizeConfiguration();
            }));
            CaretBlinkRate = CreateProperty(CaretBlinkRateValue, (oldValue, newValue) => ApplyWithoutCommand(() => CaretBlinkRateValue = newValue));
            CaretWidth = CreateProperty(CaretWidthValue, (oldValue, newValue) => ApplyWithoutCommand(() => CaretWidthValue = newValue));
            SelectionColor = CreateProperty(SelectionColorValue, (oldValue, newValue) => ApplyWithoutCommand(() => SelectionColorValue = newValue));
            OnValueChanged = Track(new Command<string>());
            OnEndEdit = Track(new Command<string>());
            OnSubmit = Track(new Command<string>());

            AddValueChangedListener(HandleValueChanged);
            AddEndEditListener(HandleEndEdit);
            AddSubmitListener(HandleSubmit);
            TrackCleanup(() =>
            {
                RemoveValueChangedListener(HandleValueChanged);
                RemoveEndEditListener(HandleEndEdit);
                RemoveSubmitListener(HandleSubmit);
            });
        }

        void HandleValueChanged(string value)
        {
            var normalized = Normalize(value);
            Text.SetValue(normalized);
            if (!suppressCommand)
            {
                OnValueChanged.Invoke(normalized);
            }
        }

        void HandleEndEdit(string value)
        {
            OnEndEdit.Invoke(Normalize(value));
        }

        void HandleSubmit(string value)
        {
            OnSubmit.Invoke(Normalize(value));
        }

        void ApplyWithoutCommand(System.Action action)
        {
            var previous = suppressCommand;
            suppressCommand = true;
            try
            {
                action();
            }
            finally
            {
                suppressCommand = previous;
            }
        }

        void SynchronizeConfiguration()
        {
            if (synchronizingConfiguration)
            {
                return;
            }

            synchronizingConfiguration = true;
            try
            {
                ContentType.SetValue(ContentTypeValue);
                LineType.SetValue(LineTypeValue);
            }
            finally
            {
                synchronizingConfiguration = false;
            }
        }

        static string Normalize(string value) => value ?? string.Empty;
    }

    /// <summary>
    /// 为自定义输入框后端提供强类型组件缓存和 Selectable 适配。
    /// </summary>
    /// <typeparam name="TInputField">Prefab 中预置的输入框组件类型。</typeparam>
    public abstract class InputFieldElement<TInputField> : InputFieldElement where TInputField : Selectable
    {
        /// <summary>获取当前 Element 适配的强类型输入框组件。</summary>
        protected TInputField Component { get; private set; }

        /// <inheritdoc />
        protected sealed override Selectable SelectableComponent => Component;

        protected override void OnInitialize()
        {
            Component = GetComponent<TInputField>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TInputField).FullName}'.");
            }

            TrackCleanup(() => Component = null);
            base.OnInitialize();
        }
    }
}
