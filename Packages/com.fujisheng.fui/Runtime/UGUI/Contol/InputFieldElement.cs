using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : SelectableElement<InputField>
    {
        /// <summary>
        /// 值
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// 值更改事件
        /// </summary>
        public Command<string> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Text = new BindableProperty<string>(Component.text, (oldValue, newValue) => Component.text = UnifiedString(newValue));
            OnValueChanged = new Command<string>();
            Component.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        void OnInputFieldValueChanged(string value)
        {
            this.Text.Value = UnifiedString(value);
            OnValueChanged.Invoke(UnifiedString(value));
        }

        /// <summary>
        /// 统一字符串  这个地方如果给Component.text赋值为空字符串会触发事件InputFieldValueChanged，值为null，导致无限循环
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string UnifiedString(string value)=> string.IsNullOrEmpty(value) ? string.Empty : value;

        protected override void OnRelease()
        {
            base.OnRelease();

            this.Component.onValueChanged.RemoveAllListeners();
            Text.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}