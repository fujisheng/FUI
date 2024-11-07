using FUI.Bindable;

using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Button))]
    public class ButtonElement : UGUIView
    {
        Button button;

        /// <summary>
        /// ����ص�
        /// </summary>
        public BindableProperty<UnityAction> OnClick { get; private set; }

        /// <summary>
        /// ����ص�תAction
        /// </summary>
        public BindableProperty<Action> OnClickAction { get; private set; }

        /// <summary>
        /// �ı�Ԫ�� �����ֻ����
        /// </summary>
        public IReadOnlyBindableProperty<TextElement> TextElement { get; private set; } 

        /// <summary>
        /// �ı�����
        /// </summary>
        public BindableProperty<string> TextValue { get; private set; }

        protected override void Initialize()
        {
            button = GetComponent<Button>();
            OnClick = new BindableProperty<UnityAction>();
            OnClickAction = new BindableProperty<Action>();
            TextElement = new BindableProperty<TextElement>(button.GetComponentInChildren<TextElement>());
            TextValue = new BindableProperty<string>(TextElement.Value?.Text?.Value);

            OnClick.OnValueChanged += OnSetOnClick;
            OnClickAction.OnValueChanged += OnSetOnClickAction;
            TextValue.OnValueChanged += OnSetTextValue;
        }

        void OnSetOnClick(UnityAction oldAction, UnityAction newAction)
        {
            button.onClick.RemoveAllListeners();

            if (newAction != null)
            {
                button.onClick.AddListener(newAction);
            }
        }

        void OnSetOnClickAction(Action oldValue, Action newValue)
        {
            button.onClick.RemoveAllListeners();

            if (newValue != null)
            {
                button.onClick.AddListener(() => newValue.Invoke());
            }
        }

        void OnSetTextValue(string oldValue, string newValue)
        {
            if(TextElement.Value == null)
            {
                return;
            }

            TextElement.Value.Text.Value = newValue;
        }

        protected override void Destroy()
        {
            button.onClick.RemoveAllListeners();
            OnClick.ClearValueChangedEvent();
            OnClickAction.ClearValueChangedEvent();
            TextValue.ClearValueChangedEvent();
        }
    }
}