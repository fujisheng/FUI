
using UnityEngine;
using UnityEngine.UI;
using System;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Button))]
    public class ButtonEventElement : UGUIVisualElement<Action>
    {
        Button button;

        protected override void Initialize()
        {
            button = transform.GetComponent<Button>();
        }

        public override void UpdateValue(Action value)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { value.Invoke(); });
        }
    }
}
