using FUI.Binding;
using UnityEngine;
using UguiMask = UnityEngine.UI.Mask;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI Mask 的运行时状态公开为可绑定属性。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UguiMask))]
    [AddComponentMenu("FUI/Elements/Mask")]
    public class MaskElement : ComponentElement<UguiMask>
    {
        /// <summary>是否显示用于裁剪的 Graphic。</summary>
        public BindableProperty<bool> ShowMaskGraphic { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ShowMaskGraphic = CreateProperty(
                Component.showMaskGraphic,
                (oldValue, newValue) => Component.showMaskGraphic = newValue);
        }
    }
}
