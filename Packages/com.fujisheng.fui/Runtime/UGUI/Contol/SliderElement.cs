using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Slider))]
    public class SliderElement : SelectableElement<Slider>
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

        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<ValueChangedArgs> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            Value = new BindableProperty<float>(Component.value, (oldValue, newValue) => Component.value = newValue);
            OnValueChanged = new Command<ValueChangedArgs>();
            Component.onValueChanged.AddListener(OnSliderValueChanged);
        }

        void OnSliderValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(new ValueChangedArgs(this, value));
        }

        protected override void Destroy()
        {
            base.Destroy();

            this.Component.onValueChanged.RemoveAllListeners();
            Value.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}