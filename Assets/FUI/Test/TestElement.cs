using FUI;
using FUI.Bindable;
using FUI.UGUI;

using System;

using UnityEngine;

namespace Assets.FUI.Test
{
    [ObservableObject]
    public class TextData : ObservableObject
    {
        [ObservableProperty]
        public string Text { get; set; }

        [ObservableProperty]
        public string Font { get; set; }

        [ObservableProperty]
        public Color Color { get; set; }
    }

    internal class TestElement : UGUIVisualElement<TextData>
    {
        View view;
        
        void Awake()
        {
            var binding = gameObject.GetComponent<Binding>();
            if(binding == null)
            {
                return;
            }

            var type = Type.GetType(Utility.UI.GetViewTypeFullName(binding.config.viewName));
            if(type == null)
            {
                return;
            }

            view = Activator.CreateInstance(type) as View;
        }

        public override void UpdateValue(TextData value) => view?.UpdateValue(value);
    }
}
