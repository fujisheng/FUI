using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI
{
    /// <summary>公开 Shadow 与 Outline 共享的图形效果属性。</summary>
    /// <typeparam name="TEffect">被适配的图形效果类型。</typeparam>
    public abstract class GraphicEffectElement<TEffect> : ComponentElement<TEffect> where TEffect : Shadow
    {
        /// <summary>效果颜色。</summary>
        public BindableProperty<Color> EffectColor { get; private set; }

        /// <summary>效果偏移。</summary>
        public BindableProperty<Vector2> EffectDistance { get; private set; }

        /// <summary>效果透明度是否与 Graphic 透明度相乘。</summary>
        public BindableProperty<bool> UseGraphicAlpha { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            EffectColor = CreateProperty(Component.effectColor, (oldValue, newValue) => Component.effectColor = newValue);
            EffectDistance = CreateProperty(Component.effectDistance, (oldValue, newValue) => Component.effectDistance = newValue);
            UseGraphicAlpha = CreateProperty(Component.useGraphicAlpha, (oldValue, newValue) => Component.useGraphicAlpha = newValue);
        }
    }
}
