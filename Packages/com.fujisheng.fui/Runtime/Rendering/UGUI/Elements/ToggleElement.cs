using FUI.Binding;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Toggle 的选中状态与变更事件公开为 FUI 可绑定成员。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("FUI/Elements/Toggle")]
    public class ToggleElement : SelectableElement<Toggle>
    {
        /// <summary>
        /// 是否开启
        /// </summary>
        public BindableProperty<bool> IsOn { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<bool> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            IsOn = CreateProperty(Component.isOn, (oldValue, newValue) => Component.SetIsOnWithoutNotify(newValue));
            OnValueChanged = Track(new Command<bool>());
            Component.onValueChanged.AddListener(OnToggleValueChanged);
            TrackCleanup(() => Component?.onValueChanged.RemoveListener(OnToggleValueChanged));
        }

        void OnToggleValueChanged(bool isOn)
        {
            this.IsOn.Value = isOn;
            OnValueChanged.Invoke(isOn);
        }

    }
}
