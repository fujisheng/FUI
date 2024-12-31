using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : SelectableElement<InputField>
    {
        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<string> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Text = new BindableProperty<string>(Component.text, (oldValue, newValue) => Component.text = newValue);
            OnValueChanged = new Command<string>();
            Component.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        void OnInputFieldValueChanged(string value)
        {
            this.Text.Value = value;
            OnValueChanged.Invoke(value);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            this.Component.onValueChanged.RemoveAllListeners();
            Text.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}