using FUI.Binding;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI Legacy InputField 适配为可绑定 Element。</summary>
    [RequireComponent(typeof(InputField))]
    [AddComponentMenu("FUI/Elements/Input Field/Legacy Input Field")]
    public class LegacyInputFieldElement : InputFieldElement<InputField>
    {
        /// <summary>Legacy InputField 的字符验证策略。</summary>
        public BindableProperty<InputField.CharacterValidation> CharacterValidation { get; private set; }

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
            set => Component.contentType = (InputField.ContentType)value;
        }

        /// <inheritdoc />
        protected override InputLineType LineTypeValue
        {
            get => (InputLineType)Component.lineType;
            set => Component.lineType = (InputField.LineType)value;
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
        }
    }
}
