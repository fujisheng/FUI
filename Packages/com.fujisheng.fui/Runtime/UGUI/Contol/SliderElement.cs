using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Slider))]
    public class SliderElement : SelectableElement<Slider>
    {
        /// <summary>
        /// ֵ
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// ֵ�����¼�
        /// </summary>
        public Command<float> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            Value = new BindableProperty<float>(Component.value, (oldValue, newValue) => Component.value = newValue);
            OnValueChanged = new Command<float>();
            Component.onValueChanged.AddListener(OnSliderValueChanged);
        }

        void OnSliderValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(value);
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