using FUI.Binding;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 为可交互 UGUI 控件公开通用状态。
    /// </summary>
    public abstract class SelectableElement : RectTransformElement
    {
        bool synchronizingColors;

        /// <summary>获取当前 Element 适配的 Selectable。</summary>
        protected abstract Selectable SelectableComponent { get; }

        /// <summary>
        /// 可交互
        /// </summary>
        public BindableProperty<bool> Interactable { get; private set; }

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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (SelectableComponent == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) does not provide a selectable component.");
            }

            Interactable = CreateProperty(
                SelectableComponent.interactable,
                (oldValue, newValue) => SelectableComponent.interactable = newValue
            );

            Colors = CreateProperty(
                SelectableComponent.colors,
                OnColorsChanged
            );

            NormalColor = CreateProperty(
                SelectableComponent.colors.normalColor,
                (oldValue, newValue) => UpdateColors(block => { block.normalColor = newValue; return block; })
            );

            HighlightedColor = CreateProperty(
                SelectableComponent.colors.highlightedColor,
                (oldValue, newValue) => UpdateColors(block => { block.highlightedColor = newValue; return block; })
            );

            PressedColor = CreateProperty(
                SelectableComponent.colors.pressedColor,
                (oldValue, newValue) => UpdateColors(block => { block.pressedColor = newValue; return block; })
            );

            SelectedColor = CreateProperty(
                SelectableComponent.colors.selectedColor,
                (oldValue, newValue) => UpdateColors(block => { block.selectedColor = newValue; return block; })
            );

            DisabledColor = CreateProperty(
                SelectableComponent.colors.disabledColor,
                (oldValue, newValue) => UpdateColors(block => { block.disabledColor = newValue; return block; })
            );

            ColorMultiplier = CreateProperty(
                SelectableComponent.colors.colorMultiplier,
                (oldValue, newValue) => UpdateColors(block => { block.colorMultiplier = newValue; return block; })
            );

            FadeDuration = CreateProperty(
                SelectableComponent.colors.fadeDuration,
                (oldValue, newValue) => UpdateColors(block => { block.fadeDuration = newValue; return block; })
            );
        }

        void OnColorsChanged(ColorBlock oldValue, ColorBlock newValue)
        {
            if (synchronizingColors)
            {
                return;
            }

            synchronizingColors = true;
            try
            {
                SelectableComponent.colors = newValue;
                SynchronizeColorProjections(newValue);
            }
            finally
            {
                synchronizingColors = false;
            }
        }

        void UpdateColors(System.Func<ColorBlock, ColorBlock> update)
        {
            if (synchronizingColors)
            {
                return;
            }

            var colors = update(SelectableComponent.colors);
            SelectableComponent.colors = colors;
            Colors.SetValue(colors);
            SynchronizeColorProjections(colors);
        }

        void SynchronizeColorProjections(ColorBlock colors)
        {
            synchronizingColors = true;
            try
            {
                NormalColor.SetValue(colors.normalColor);
                HighlightedColor.SetValue(colors.highlightedColor);
                PressedColor.SetValue(colors.pressedColor);
                SelectedColor.SetValue(colors.selectedColor);
                DisabledColor.SetValue(colors.disabledColor);
                ColorMultiplier.SetValue(colors.colorMultiplier);
                FadeDuration.SetValue(colors.fadeDuration);
            }
            finally
            {
                synchronizingColors = false;
            }
        }
    }

    /// <summary>
    /// 将一个必需的 Selectable 组件适配为可绑定 Element。
    /// </summary>
    /// <typeparam name="TSelectable">被适配的 Selectable 类型。</typeparam>
    public abstract class SelectableElement<TSelectable> : SelectableElement where TSelectable : Selectable
    {
        /// <summary>获取当前 Element 适配的强类型 Selectable。</summary>
        protected TSelectable Component { get; private set; }

        /// <inheritdoc />
        protected sealed override Selectable SelectableComponent => Component;

        protected override void OnInitialize()
        {
            Component = GetComponent<TSelectable>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TSelectable).FullName}'.");
            }

            TrackCleanup(() => Component = null);
            base.OnInitialize();
        }
    }
}
