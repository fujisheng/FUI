using FUI.Bindable;

using System;

using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    public class ToggleElement : UGUIView
    {
        Toggle toggle;

        /// <summary>
        /// 是否开启
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<bool> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            toggle = transform.GetComponent<Toggle>();

            IsOn = new BindableProperty<bool>(toggle.isOn);
            OnValueChanged = new Command<bool>();
            toggle.onValueChanged.AddListener((value) =>
            {
                this.IsOn.Value = value;
                OnValueChanged.Execute(value);
            });

            IsOn.OnValueChanged += OnSetValue;
        }

        void OnSetValue(bool oldValue, bool newValue)
        {
            toggle.isOn = newValue;
        }

        protected override void Destroy()
        {
            this.toggle.onValueChanged.RemoveAllListeners();
            IsOn.ClearEvent();
            OnValueChanged.ClearListener();
        }
    }
}