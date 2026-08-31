using FUI.Binding;

using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 CanvasGroup 的透明度、交互和射线设置公开为可绑定属性。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("FUI/Elements/Canvas Group")]
    public class CanvasGroupElement : ComponentElement<CanvasGroup>
    {
        /// <summary>
        /// CanvasGroup.alpha
        /// </summary>
        public BindableProperty<float> Alpha { get; private set; }

        /// <summary>
        /// CanvasGroup.interactable
        /// </summary>
        public BindableProperty<bool> Interactable { get; private set; }

        /// <summary>
        /// CanvasGroup.blocksRaycasts
        /// </summary>
        public BindableProperty<bool> BlocksRaycasts { get; private set; }

        /// <summary>
        /// CanvasGroup.ignoreParentGroups
        /// </summary>
        public BindableProperty<bool> IgnoreParentGroups { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Alpha = CreateProperty(Component.alpha, (oldValue, newValue) => Component.alpha = newValue);
            Interactable = CreateProperty(Component.interactable, (oldValue, newValue) => Component.interactable = newValue);
            BlocksRaycasts = CreateProperty(Component.blocksRaycasts, (oldValue, newValue) => Component.blocksRaycasts = newValue);
            IgnoreParentGroups = CreateProperty(Component.ignoreParentGroups, (oldValue, newValue) => Component.ignoreParentGroups = newValue);
        }
    }
}
