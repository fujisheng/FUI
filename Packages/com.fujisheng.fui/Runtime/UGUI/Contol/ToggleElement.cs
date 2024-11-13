using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleElement : SelectableElement<Toggle>
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
            base.Initialize();

            IsOn = new BindableProperty<bool>(Component.isOn, (oldValue, newValue) => Component.isOn = newValue);
            OnValueChanged = new Command<ValueChangedArgs>();
            Component.onValueChanged.AddListener(OnToggleValueChanged);
        }

        void OnToggleValueChanged(bool isOn)
        {
            this.IsOn.Value = isOn;
            OnValueChanged.Invoke(new ValueChangedArgs(this, isOn));
        }

        protected override void Destroy()
        {
            base.Destroy();

            this.Component.onValueChanged.RemoveAllListeners();
            IsOn.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}