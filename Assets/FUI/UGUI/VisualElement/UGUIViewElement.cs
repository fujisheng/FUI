using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Binding))]
    public class UGUIViewElement : UGUIVisualElement<ObservableObject>
    {
        View view;
        
        void Awake()
        {
            var binding = gameObject.GetComponent<Binding>();
            if(binding == null)
            {
                return;
            }

            if (!UGUIViewTypeCache.TryGetViewType(binding.config.viewName, out var viewType))
            {
                return;
            }

            view = Activator.CreateInstance(viewType, null, this.AssetLoader, this.gameObject, binding.config.viewName) as View;
        }

        public override void UpdateValue(ObservableObject value) => view.UpdateValue(value);
    }
}
