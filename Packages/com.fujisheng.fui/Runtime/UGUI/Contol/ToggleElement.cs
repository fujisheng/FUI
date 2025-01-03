using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleElement : SelectableElement<Toggle>
    {
        /// <summary>
        /// 是否开启
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<bool> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            IsOn = new BindableProperty<bool>(Component.isOn, (oldValue, newValue) => Component.isOn = newValue);
            OnValueChanged = new Command<bool>();
            Component.onValueChanged.AddListener(OnToggleValueChanged);
        }

        void OnToggleValueChanged(bool isOn)
        {
            this.IsOn.Value = isOn;
            OnValueChanged.Invoke(isOn);
        }

        protected override void OnRelease()
        {
            base.OnRelease();

            this.Component.onValueChanged.RemoveAllListeners();
            IsOn.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}