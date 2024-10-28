using FUI.Bindable;

using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    public class ToggleElement : UGUIView
    {
        Toggle toggle;

        /// <summary>
        /// ÊÇ·ñ¿ªÆô
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        protected override void Initialize()
        {
            toggle = transform.GetComponent<Toggle>();

            IsOn = new BindableProperty<bool>(toggle.isOn);
            toggle.onValueChanged.AddListener((value) =>
            {
                this.IsOn.Value = value;
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
        }
    }
}