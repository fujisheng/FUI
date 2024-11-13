using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : SelectableElement<InputField>
    {
        /// <summary>
        /// 值更改参数
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
        /// 值
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// 值更改事件
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