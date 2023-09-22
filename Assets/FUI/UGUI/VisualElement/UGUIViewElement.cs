using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI.VisualElement
{
    [RequireComponent(typeof(Binding))]
    public class UGUIViewElement : UGUIVisualElement<ObservableObject>
    {
        View view;
        Type viewType;
        Binding binding;
        
        void Awake()
        {
            binding = gameObject.GetComponent<Binding>();
            if(binding == null)
            {
                return;
            }

            if (!UGUIViewTypeCache.TryGetViewType(binding.config.viewName, out var viewType))
            {
                return;
            }

            this.viewType = viewType;
            
        }

        public override void UpdateValue(ObservableObject value)
        {
            if (view == null)
            {
                if(viewType == null)
                {
                    return;
                }

                view = Activator.CreateInstance(viewType, value, this.AssetLoader, this.gameObject, binding.config.viewName) as View;
            }
            else
            {
                view.UpdateValue(value);
            }

            SynchronizeProperties(value);
        }

        void SynchronizeProperties(ObservableObject viewModel)
        {
            if(viewModel is ISynchronizeProperties synchronizeProperties)
            {
                synchronizeProperties.Synchronize();
            }
        }
    }
}
