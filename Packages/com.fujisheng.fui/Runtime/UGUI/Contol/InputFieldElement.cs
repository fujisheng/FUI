using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(InputField))]
    public class InputFieldElement : SelectableElement<InputField>
    {
        /// <summary>
        /// ֵ
        /// </summary>
        public BindableProperty<string> Text { get; private set; }

        /// <summary>
        /// ֵ�����¼�
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
        /// ͳһ�ַ���  ����ط������Component.text��ֵΪ���ַ����ᴥ���¼�InputFieldValueChanged��ֵΪnull����������ѭ��
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