using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI.Control
{
    public class CanvasGroupElement : UIElement<CanvasGroup>
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
            Alpha = new BindableProperty<float>(Component.alpha, (oldValue, newValue) => Component.alpha = newValue);
            Interactable = new BindableProperty<bool>(Component.interactable, (oldValue, newValue) => Component.interactable = newValue);
            BlocksRaycasts = new BindableProperty<bool>(Component.blocksRaycasts, (oldValue, newValue) => Component.blocksRaycasts = newValue);
            IgnoreParentGroups = new BindableProperty<bool>(Component.ignoreParentGroups, (oldValue, newValue) => Component.ignoreParentGroups = newValue);
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            Alpha?.Dispose();
            Interactable?.Dispose();
            BlocksRaycasts?.Dispose();
            IgnoreParentGroups?.Dispose();
        }
    }
}