using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Dropdown))]
    public class DropdownElement : UGUIView
    {
        /// <summary>
        /// 值更改参数
        /// </summary>
        public class ValueChangedArgs : CommandArgs
        {
            public float Value { get; }
            public ValueChangedArgs(object sender, float value) : base(sender)
            {
                this.Value = value;
            }
        }

        Dropdown dropdown;

        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<int> Value { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<ValueChangedArgs> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            dropdown = transform.GetComponent<Dropdown>();

            Value = new BindableProperty<int>(dropdown.value);
            OnValueChanged = new Command<ValueChangedArgs>();
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            Value.OnValueChanged += (oldValue, newValue) => dropdown.value = newValue;
        }

        void OnDropdownValueChanged(int value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(new ValueChangedArgs(this, value));
        }

        protected override void Destroy()
        {
            this.dropdown.onValueChanged.RemoveAllListeners();
            Value.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}