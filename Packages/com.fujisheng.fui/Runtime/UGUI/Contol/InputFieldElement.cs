using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : SelectableElement<InputField>
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
            base.Initialize();

            Text = new BindableProperty<string>(Component.text, (oldValue, newValue) => Component.text = newValue);
            OnValueChanged = new Command<ValueChangedArgs>();
            Component.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        void OnInputFieldValueChanged(string value)
        {
            this.Text.Value = value;
            OnValueChanged.Invoke(new ValueChangedArgs(this, value));
        }

        protected override void Destroy()
        {
            base.Destroy();

            this.Component.onValueChanged.RemoveAllListeners();
            Text.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}