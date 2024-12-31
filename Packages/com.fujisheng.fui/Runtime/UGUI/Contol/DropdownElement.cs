using FUI.Bindable;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Dropdown))]
    public class DropdownElement : SelectableElement<Dropdown>
    {
        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<int> Value { get; private set; }

        /// <summary>
        /// 选项
        /// </summary>
        public BindableProperty<List<string>> Options { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<int> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Value = new BindableProperty<int>(Component.value);
            Options = new BindableProperty<List<string>>(new List<string>(), OnSetOptions);
            OnValueChanged = new Command<int>();
            Component.onValueChanged.AddListener(OnDropdownValueChanged);
            Value.OnValueChanged += (oldValue, newValue) => Component.value = newValue;
        }

        void OnDropdownValueChanged(int value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(value);
        }

        void OnSetOptions(List<string> oldValue, List<string> newValue)
        {
            Component.ClearOptions();
            Component.AddOptions(newValue);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            this.Component.onValueChanged.RemoveAllListeners();
            Value.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}