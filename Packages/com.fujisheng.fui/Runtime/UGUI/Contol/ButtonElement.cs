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

        public BindableProperty<UnityAction> OnClick { get; private set; }
        public BindableProperty<Action> OnClickAction { get; private set; }
        public BindableProperty<TextElement> TextElement { get; private set; } 
        public BindableProperty<string> TextValue { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            button = GetComponent<Button>();
            OnClick = new BindableProperty<UnityAction>();
            OnClickAction = new BindableProperty<Action>();
            TextElement = new BindableProperty<TextElement>(button.GetComponentInChildren<TextElement>());
            TextValue = new BindableProperty<string>(TextElement.Value?.Text?.Value);

            OnClick.PropertySet += OnSetOnClick;
            OnClickAction.PropertySet += OnSetOnClickAction;
            TextElement.PropertySet += OnSetTextElement;
            TextValue.PropertySet += OnSetTextValue;
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

        void OnSetTextElement(TextElement oldText, TextElement newText)
        {
            throw new System.Exception($"can not set ButtonElement.Text");
        }

        void OnSetTextValue(string oldValue, string newValue)
        {
            if(TextElement.Value == null)
            {
                return;
            }

            TextElement.Value.Text.Value = newValue;
        }

        public override void Destroy()
        {
            base.Destroy();

            button.onClick.RemoveAllListeners();
            OnClick.ClearEvent();
            OnClickAction.ClearEvent();
            TextElement.ClearEvent();
        }
    }
}