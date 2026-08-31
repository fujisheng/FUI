using FUI.Binding;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Slider 的当前值与变更事件公开为 FUI 可绑定成员。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    [AddComponentMenu("FUI/Elements/Slider")]
    public class SliderElement : SelectableElement<Slider>
    {
        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<float> OnValueChanged { get; private set; }

        /// <summary>允许的最小值。</summary>
        public BindableProperty<float> MinValue { get; private set; }

        /// <summary>允许的最大值。</summary>
        public BindableProperty<float> MaxValue { get; private set; }

        /// <summary>是否将值限制为整数。</summary>
        public BindableProperty<bool> WholeNumbers { get; private set; }

        /// <summary>滑动方向。</summary>
        public BindableProperty<Slider.Direction> Direction { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Value = CreateProperty(Component.value, (oldValue, newValue) => Component.SetValueWithoutNotify(newValue));
            MinValue = CreateProperty(Component.minValue, (oldValue, newValue) => Component.minValue = newValue);
            MaxValue = CreateProperty(Component.maxValue, (oldValue, newValue) => Component.maxValue = newValue);
            WholeNumbers = CreateProperty(Component.wholeNumbers, (oldValue, newValue) => Component.wholeNumbers = newValue);
            Direction = CreateProperty(Component.direction, (oldValue, newValue) => Component.direction = newValue);
            OnValueChanged = Track(new Command<float>());
            Component.onValueChanged.AddListener(OnSliderValueChanged);
            TrackCleanup(() => Component?.onValueChanged.RemoveListener(OnSliderValueChanged));
        }

        void OnSliderValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(value);
        }

    }
}
