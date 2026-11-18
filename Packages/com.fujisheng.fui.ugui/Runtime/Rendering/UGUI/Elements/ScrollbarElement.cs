using FUI.Binding;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Scrollbar 的当前值与变更事件公开为 FUI 可绑定成员。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Scrollbar))]
    [AddComponentMenu("FUI/Elements/Scrollbar")]
    public class ScrollbarElement : SelectableElement<Scrollbar>, IScrollbarElement
    {
        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<float> OnValueChanged { get; private set; }

        /// <summary>滚动条手柄占轨道的比例。</summary>
        public BindableProperty<float> Size { get; private set; }

        /// <summary>可选择的离散步数；零表示连续值。</summary>
        public BindableProperty<int> NumberOfSteps { get; private set; }

        /// <summary>滚动条方向。</summary>
        public BindableProperty<Scrollbar.Direction> Direction { get; private set; }

        Command<float> IFloatValueElement.ValueChanged => OnValueChanged;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Value = CreateProperty(Component.value, (oldValue, newValue) => Component.SetValueWithoutNotify(newValue));
            Size = CreateProperty(Component.size, (oldValue, newValue) => Component.size = newValue);
            NumberOfSteps = CreateProperty(Component.numberOfSteps, (oldValue, newValue) => Component.numberOfSteps = newValue);
            Direction = CreateProperty(Component.direction, (oldValue, newValue) => Component.direction = newValue);
            OnValueChanged = Track(new Command<float>());
            Component.onValueChanged.AddListener(OnScrollbarValueChanged);
            TrackCleanup(() => Component?.onValueChanged.RemoveListener(OnScrollbarValueChanged));
        }

        void OnScrollbarValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(value);
        }

    }
}
