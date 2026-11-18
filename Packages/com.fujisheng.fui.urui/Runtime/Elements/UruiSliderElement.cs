using System;
using FUI.Binding;
using FUI.Rendering;

namespace FUI.Rendering.URUI
{
    public class UruiSliderElement : UruiValueElement<float>, ISliderElement, IFloatValueElement
    {
        readonly global::URUI.Controls.Slider slider;
        readonly float floatingStep;
        readonly global::URUI.Controls.SliderVisual visual;

        public UruiSliderElement(global::URUI.Elements.Element element, string name) : this(element, name, null) { }

        public UruiSliderElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.SliderVisual visual)
            : base(element, name, Source(element, visual))
        {
            this.visual = visual;
            slider = element as global::URUI.Controls.Slider;
            if (slider != null)
            {
                var authored = element.GetAttribute("value");
                floatingStep = slider.HasAttribute("step") ? slider.Step :
                    Math.Max(float.Epsilon, (float)(((double)slider.HighValue - slider.LowValue) / 1000000d));
                Configure(() =>
                {
                    slider.Step = floatingStep;
                    if (float.TryParse(authored, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var initial)) { slider.SetValueWithoutNotify(initial); }
                });
            }
            MinValue = new BindableProperty<float>(slider != null ? slider.LowValue : 0f, (_, value) => ApplyRange());
            MaxValue = new BindableProperty<float>(slider != null ? slider.HighValue : 100f, (_, value) => ApplyRange());
            WholeNumbers = new BindableProperty<bool>(false, (_, value) => ApplyRange());
        }

        static global::URUI.Controls.INotifyValueChanged<float> Source(global::URUI.Elements.Element element, global::URUI.Controls.SliderVisual visual)
        {
            if (visual != null && !ReferenceEquals(visual.Control, element)) { throw new ArgumentException("滑块外观必须使用同一个输入节点。", nameof(visual)); }
            return visual ?? element as global::URUI.Controls.INotifyValueChanged<float>;
        }

        public BindableProperty<float> MinValue { get; }
        public BindableProperty<float> MaxValue { get; }
        public BindableProperty<bool> WholeNumbers { get; }

        void ApplyRange()
        {
            if (slider == null) return;
            var integer = WholeNumbers?.Value == true;
            var low = integer ? UnityEngine.Mathf.Round(MinValue.Value) : MinValue.Value;
            var high = integer ? UnityEngine.Mathf.Round(MaxValue.Value) : MaxValue.Value;
            if (low > high) throw new ArgumentOutOfRangeException(nameof(MinValue), "滑块最小值不能大于最大值。");
            Configure(() =>
            {
                if (low > slider.HighValue)
                {
                    slider.HighValue = high;
                    slider.LowValue = low;
                }
                else
                {
                    slider.LowValue = low;
                    slider.HighValue = high;
                }
                slider.Step = integer ? 1 : floatingStep;
            });
        }

        public override void Dispose()
        {
            visual?.Dispose();
            MinValue?.Dispose();
            MaxValue?.Dispose();
            WholeNumbers?.Dispose();
            base.Dispose();
        }
    }
}
