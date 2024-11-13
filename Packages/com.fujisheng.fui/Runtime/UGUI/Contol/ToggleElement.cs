using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleElement : SelectableElement<Toggle>
    {
        /// <summary>
        /// 值更改参数
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
        /// 是否开启
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        /// <summary>
        /// 值更改事件
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