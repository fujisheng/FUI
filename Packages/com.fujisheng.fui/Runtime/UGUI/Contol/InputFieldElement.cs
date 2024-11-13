using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : UGUIView
    {
        /// <summary>
        /// ֵ���Ĳ���
        /// </summary>
        public class ValueChangedArgs : CommandArgs
        {
            public string Value { get; }
            public ValueChangedArgs(object sender, string value) : base(sender)
            {
                this.Value = value;
            }
        }

        InputField inputField;

        /// <summary>
        /// ֵ
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// ֵ�����¼�
        /// </summary>
        public Command<ValueChangedArgs> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            inputField = transform.GetComponent<InputField>();

            Text = new BindableProperty<string>(inputField.text);
            OnValueChanged = new Command<ValueChangedArgs>();
            inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            Text.OnValueChanged += (oldValue, newValue) => inputField.text = newValue;
        }

        void OnInputFieldValueChanged(string value)
        {
            this.Text.Value = value;
            OnValueChanged.Invoke(new ValueChangedArgs(this, value));
        }

        protected override void Destroy()
        {
            this.inputField.onValueChanged.RemoveAllListeners();
            Text.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}