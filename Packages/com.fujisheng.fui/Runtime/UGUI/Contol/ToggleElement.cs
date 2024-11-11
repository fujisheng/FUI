using FUI.Bindable;

using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    public class ToggleElement : UGUIView
    {
        /// <summary>
        /// ֵ���Ĳ���
        /// </summary>
        public class ValueChangedArgs : CommandArgs
        {
            public bool IsOn { get; }
            public ValueChangedArgs(object sender, bool isOn) : base(sender)
            {
                this.IsOn = isOn;
            }
        }

        Toggle toggle;

        /// <summary>
        /// �Ƿ���
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        /// <summary>
        /// ֵ�����¼�
        /// </summary>
        public Command<ValueChangedArgs> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            toggle = transform.GetComponent<Toggle>();

            IsOn = new BindableProperty<bool>(toggle.isOn);
            OnValueChanged = new Command<ValueChangedArgs>();
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            IsOn.OnValueChanged += OnSetValue;
        }

        void OnSetValue(bool oldValue, bool newValue)
        {
            toggle.isOn = newValue;
        }

        void OnToggleValueChanged(bool isOn)
        {
            this.IsOn.Value = isOn;
            OnValueChanged.Invoke(new ValueChangedArgs(this, isOn));
        }

        protected override void Destroy()
        {
            this.toggle.onValueChanged.RemoveAllListeners();
            IsOn.ClearValueChangedEvent();
            OnValueChanged.ClearListener();
        }
    }
}