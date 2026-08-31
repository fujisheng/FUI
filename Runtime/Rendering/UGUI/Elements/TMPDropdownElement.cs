using System.Collections.Generic;
using FUI.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 TMP_Dropdown 适配为可绑定 Element。</summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    [AddComponentMenu("FUI/Elements/Dropdown/TextMeshPro Dropdown")]
    public class TMPDropdownElement : DropdownElement<TMP_Dropdown>
    {
        /// <summary>
        /// 是否允许多选；启用后 Value 使用 TMP_Dropdown 定义的位掩码语义。
        /// </summary>
        public BindableProperty<bool> MultiSelect { get; private set; }

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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            MultiSelect = CreateProperty(Component.MultiSelect, (oldValue, newValue) => Component.MultiSelect = newValue);
        }
    }
}
