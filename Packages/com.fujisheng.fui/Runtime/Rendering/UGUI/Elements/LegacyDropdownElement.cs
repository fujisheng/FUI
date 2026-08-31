using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI Legacy Dropdown 适配为可绑定 Element。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Dropdown))]
    [AddComponentMenu("FUI/Elements/Dropdown/Legacy Dropdown")]
    public class LegacyDropdownElement : DropdownElement<Dropdown>
    {
        /// <inheritdoc />
        protected override int ValueValue => Component.value;

        /// <inheritdoc />
        protected override float AlphaFadeSpeedValue
        {
            get => Component.alphaFadeSpeed;
            set => Component.alphaFadeSpeed = value;
        }

        /// <inheritdoc />
        protected override IReadOnlyList<string> ReadOptions()
        {
            var options = new List<string>(Component.options.Count);
            for (var index = 0; index < Component.options.Count; index++)
            {
                options.Add(Component.options[index].text);
            }
            return options;
        }

        /// <inheritdoc />
        protected override void SetValueWithoutNotify(int value) => Component.SetValueWithoutNotify(value);

        /// <inheritdoc />
        protected override void ReplaceOptions(IReadOnlyList<string> options)
        {
            Component.ClearOptions();
            if (options == null || options.Count == 0)
            {
                return;
            }

            var values = new List<string>(options.Count);
            for (var index = 0; index < options.Count; index++)
            {
                values.Add(options[index] ?? string.Empty);
            }
            Component.AddOptions(values);
            Component.RefreshShownValue();
        }

        /// <inheritdoc />
        protected override void AddValueChangedListener(UnityAction<int> listener) => Component.onValueChanged.AddListener(listener);

        /// <inheritdoc />
        protected override void RemoveValueChangedListener(UnityAction<int> listener) => Component?.onValueChanged.RemoveListener(listener);
    }
}
