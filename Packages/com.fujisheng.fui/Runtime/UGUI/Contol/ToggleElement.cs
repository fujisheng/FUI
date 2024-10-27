using FUI.Bindable;

using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    public class ToggleElement : UGUIView
    {
        Toggle toggle;

        public BindableProperty<bool> IsOn { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            toggle = transform.GetComponent<Toggle>();

            IsOn = new BindableProperty<bool>(toggle.isOn);
            toggle.onValueChanged.AddListener((value) =>
            {
                this.IsOn.Value = value;
            });

            IsOn.PropertySet += OnSetValue;
        }

        void OnSetValue(bool oldValue, bool newValue)
        {
            toggle.isOn = newValue;
        }

        public override void Destroy()
        {
            base.Destroy();

            IsOn.ClearEvent();
        }
    }
}