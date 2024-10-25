using FUI.Bindable;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ButtonElement : UGUIViewElement
    {
        public BindableProperty<UnityAction> OnClick { get; } = new BindableProperty<UnityAction>();
        public BindableProperty<string> Text { get; } = new BindableProperty<string>();

        protected override void Initialize()
        {
            base.Initialize();

            var button = GetComponent<UnityEngine.UI.Button>();
            OnClick.PropertyChanged += (sender, preValue, newValue) =>
            {
                if(preValue == newValue)
                {
                    return;
                }

                
                button.onClick.RemoveAllListeners();

                if(newValue != null)
                {
                    button.onClick.AddListener(newValue);
                }
            };

            Text.PropertyChanged += (sender, preValue, newValue) =>
            {
                if (preValue == newValue)
                {
                    return;
                }

                button.GetComponentInChildren<Text>().text = newValue;
            };
        }
    }
}