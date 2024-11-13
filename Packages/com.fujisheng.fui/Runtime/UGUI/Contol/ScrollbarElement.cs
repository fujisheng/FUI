using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarElement : UGUIView
    {
        /// <summary>
        /// ֵ���Ĳ���
        /// </summary>
        public class ValueChangedArgs : CommandArgs
        {
            public float Value { get; }
            public ValueChangedArgs(object sender, float value) : base(sender)
            {
                this.Value = value;
            }
        }

        Scrollbar scrollbar;

        /// <summary>
        /// ֵ
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// ֵ�����¼�
        /// </summary>
        public Command<ValueChangedArgs> OnValueChanged { get; private set; }

        protected override void Initialize()
        {
            scrollbar = transform.GetComponent<Scrollbar>();

            Value = new BindableProperty<float>(scrollbar.value);
            OnValueChanged = new Command<ValueChangedArgs>();
            scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
            Value.OnValueChanged += (oldValue, newValue) => scrollbar.value = newValue;
        }

        void OnScrollbarValueChanged(float value)
        {
            this.Value.Value = value;
            OnValueChanged.Invoke(new ValueChangedArgs(this, value));
        }

        protected override void Destroy()
        {
            this.scrollbar.onValueChanged.RemoveAllListeners();
            Value.Dispose();
            OnValueChanged.ClearListeners();
        }
    }
}