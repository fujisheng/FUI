using FUI.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 TMP_InputField 适配为可绑定 Element。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_InputField))]
    [AddComponentMenu("FUI/Elements/Input Field/TextMeshPro Input Field")]
    public class TMPInputFieldElement : InputFieldElement<TMP_InputField>
    {
        /// <summary>TMP_InputField 的字符验证策略。</summary>
        public BindableProperty<TMP_InputField.CharacterValidation> CharacterValidation { get; private set; }

        /// <summary>是否允许输入富文本标签。</summary>
        public BindableProperty<bool> RichText { get; private set; }

        /// <summary>编辑时是否允许光标穿过和修改富文本标签。</summary>
        public BindableProperty<bool> RichTextEditingAllowed { get; private set; }

        /// <summary>失去激活状态时是否重置输入状态。</summary>
        public BindableProperty<bool> ResetOnDeactivation { get; private set; }

        /// <summary>按 Escape 时是否恢复原始文本。</summary>
        public BindableProperty<bool> RestoreOriginalTextOnEscape { get; private set; }

        /// <inheritdoc />
        protected override string TextValue => Component.text;

        /// <inheritdoc />
        protected override bool ReadOnlyValue
        {
            get => Component.readOnly;
            set => Component.readOnly = value;
        }

        /// <inheritdoc />
        protected override int CharacterLimitValue
        {
            get => Component.characterLimit;
            set => Component.characterLimit = value;
        }

        /// <inheritdoc />
        protected override InputContentType ContentTypeValue
        {
            get => (InputContentType)Component.contentType;
            set => Component.contentType = (TMP_InputField.ContentType)value;
        }

        /// <inheritdoc />
        protected override InputLineType LineTypeValue
        {
            get => (InputLineType)Component.lineType;
            set => Component.lineType = (TMP_InputField.LineType)value;
        }

        /// <inheritdoc />
        protected override float CaretBlinkRateValue
        {
            get => Component.caretBlinkRate;
            set => Component.caretBlinkRate = value;
        }

        /// <inheritdoc />
        protected override int CaretWidthValue
        {
            get => Component.caretWidth;
            set => Component.caretWidth = value;
        }

        /// <inheritdoc />
        protected override Color SelectionColorValue
        {
            get => Component.selectionColor;
            set => Component.selectionColor = value;
        }

        /// <inheritdoc />
        protected override void SetTextWithoutNotify(string value) => Component.SetTextWithoutNotify(value);

        /// <inheritdoc />
        protected override void AddValueChangedListener(UnityAction<string> listener) => Component.onValueChanged.AddListener(listener);

        /// <inheritdoc />
        protected override void RemoveValueChangedListener(UnityAction<string> listener) => Component?.onValueChanged.RemoveListener(listener);

        /// <inheritdoc />
        protected override void AddEndEditListener(UnityAction<string> listener) => Component.onEndEdit.AddListener(listener);

        /// <inheritdoc />
        protected override void RemoveEndEditListener(UnityAction<string> listener) => Component?.onEndEdit.RemoveListener(listener);

        /// <inheritdoc />
        protected override void AddSubmitListener(UnityAction<string> listener) => Component.onSubmit.AddListener(listener);

        /// <inheritdoc />
        protected override void RemoveSubmitListener(UnityAction<string> listener) => Component?.onSubmit.RemoveListener(listener);

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CharacterValidation = CreateProperty(
                Component.characterValidation,
                (oldValue, newValue) => Component.characterValidation = newValue);
            RichText = CreateProperty(Component.richText, (oldValue, newValue) => Component.richText = newValue);
            RichTextEditingAllowed = CreateProperty(
                Component.isRichTextEditingAllowed,
                (oldValue, newValue) => Component.isRichTextEditingAllowed = newValue);
            ResetOnDeactivation = CreateProperty(
                Component.resetOnDeActivation,
                (oldValue, newValue) => Component.resetOnDeActivation = newValue);
            RestoreOriginalTextOnEscape = CreateProperty(
                Component.restoreOriginalTextOnEscape,
                (oldValue, newValue) => Component.restoreOriginalTextOnEscape = newValue);
        }
    }
}
