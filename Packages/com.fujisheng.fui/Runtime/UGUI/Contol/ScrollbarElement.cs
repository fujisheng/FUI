using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarElement : SelectableElement<Scrollbar>
    {
        /// <summary>
        /// ֵ
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// ֵ�����¼�
        /// </summary>
        public Command<float> OnValueChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Value = new BindableProperty<float>(Component.value, (oldValue, newValue) => Component.value = newValue);
            OnValueChanged = new Command<float>();
            Component.onValueChanged.AddListener(OnScrollbarValueChanged);
        }

        void OnScrollbarValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(value);
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