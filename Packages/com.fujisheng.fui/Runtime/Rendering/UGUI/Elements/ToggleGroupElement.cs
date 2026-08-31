using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 ToggleGroup 的公共状态公开为可绑定属性。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ToggleGroup))]
    [AddComponentMenu("FUI/Elements/Toggle Group")]
    public class ToggleGroupElement : ComponentElement<ToggleGroup>
    {
        /// <summary>是否允许组内所有 Toggle 同时关闭。</summary>
        public BindableProperty<bool> AllowSwitchOff { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            AllowSwitchOff = CreateProperty(
                Component.allowSwitchOff,
                (oldValue, newValue) => Component.allowSwitchOff = newValue);
        }
    }
}
