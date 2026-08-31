using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 AspectRatioFitter 适配为可绑定 Element。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AspectRatioFitter))]
    [AddComponentMenu("FUI/Elements/Aspect Ratio")]
    public class AspectRatioElement : ComponentElement<AspectRatioFitter>
    {
        /// <summary>宽高比适配模式。</summary>
        public BindableProperty<AspectRatioFitter.AspectMode> Mode { get; private set; }

        /// <summary>目标宽高比。</summary>
        public BindableProperty<float> AspectRatio { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Mode = CreateProperty(Component.aspectMode, (oldValue, newValue) => Component.aspectMode = newValue);
            AspectRatio = CreateProperty(Component.aspectRatio, (oldValue, newValue) => Component.aspectRatio = newValue);
        }
    }
}
