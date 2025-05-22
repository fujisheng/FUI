using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI.Control
{
    public class PrototypeElement : ViewElement, IViewPrototype
    {
        public BindableProperty<IViewPrototype> Prototype { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Prototype = new BindableProperty<IViewPrototype>(this, null);
            gameObject.SetActive(false);
        }

        IView IViewPrototype.Clone()
        {
            if (Prototype.Value != null)
            {
                var gameObject = Instantiate(this.gameObject);
                gameObject.transform.SetParent(this.transform.parent, false);
                return View.Create(this.AssetLoader, gameObject, gameObject.name);
            }

            return null;
        }
    }
}