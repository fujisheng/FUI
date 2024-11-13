using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarElement : UGUIView
    {
        /// <summary>
        /// 值更改参数
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
        /// 值
        /// </summary>
        public BindableProperty<float> Value { get; private set; }

        /// <summary>
        /// 值更改事件
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