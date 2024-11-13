using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 可选择元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public abstract class SelectableElement<T> : UIElement<T> where T : Selectable
    {
        /// <summary>
        /// selectable.colors
        /// </summary>
        public BindableProperty<ColorBlock> Colors { get; private set; }

        /// <summary>
        /// selectable.colors.normalColor
        /// </summary>
        public BindableProperty<Color> NormalColor { get; private set; }

        /// <summary>
        /// selectable.colors.highlightedColor
        /// </summary>
        public BindableProperty<Color> HighlightedColor { get; private set; }

        /// <summary>
        /// selectable.colors.pressedColor
        /// </summary>
        public BindableProperty<Color> PressedColor { get; private set; }

        /// <summary>
        /// selectable.colors.selectedColor
        /// </summary>
        public BindableProperty<Color> SelectedColor { get; private set; }

        /// <summary>
        /// selectable.colors.disabledColor
        /// </summary>
        public BindableProperty<Color> DisabledColor { get; private set; }

        /// <summary>
        /// selectable.colors.colorMultiplier
        /// </summary>
        public BindableProperty<float> ColorMultiplier { get; private set; }

        /// <summary>
        /// selectable.colors.fadeDuration
        /// </summary>
        public BindableProperty<float> FadeDuration { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            Colors = new BindableProperty<ColorBlock>(
                Component.colors,
                (oldValue, newValue) => Component.colors = newValue
            );

            NormalColor = new BindableProperty<Color>(
                Component.colors.normalColor,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = newValue,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            HighlightedColor = new BindableProperty<Color>(
                Component.colors.highlightedColor,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = newValue,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            PressedColor = new BindableProperty<Color>(
                Component.colors.pressedColor,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = newValue,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            SelectedColor = new BindableProperty<Color>(
                Component.colors.selectedColor,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = newValue,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            DisabledColor = new BindableProperty<Color>(
                Component.colors.disabledColor,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = newValue,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            ColorMultiplier = new BindableProperty<float>(
                Component.colors.colorMultiplier,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = newValue,
                    fadeDuration = Component.colors.fadeDuration
                }
            );

            FadeDuration = new BindableProperty<float>(
                Component.colors.fadeDuration,
                (oldValue, newValue) => Component.colors = new ColorBlock
                {
                    normalColor = Component.colors.normalColor,
                    highlightedColor = Component.colors.highlightedColor,
                    pressedColor = Component.colors.pressedColor,
                    selectedColor = Component.colors.selectedColor,
                    disabledColor = Component.colors.disabledColor,
                    colorMultiplier = Component.colors.colorMultiplier,
                    fadeDuration = newValue
                }
            );
        }

        protected sealed override void SetInteractable(bool oldInteractable, bool interactable)
        {
            Component.interactable = interactable;
        }

        protected override void Destroy()
        {
            base.Destroy();

            Colors.Dispose();
            NormalColor.Dispose();
            HighlightedColor.Dispose();
            PressedColor.Dispose();
            SelectedColor.Dispose();
            DisabledColor.Dispose();
            ColorMultiplier.Dispose();
            FadeDuration.Dispose();
        }
    }
}