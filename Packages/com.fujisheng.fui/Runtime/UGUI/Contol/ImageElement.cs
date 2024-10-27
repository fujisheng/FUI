using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Image))]
    public class ImageElement : UGUIView
    {
        Image image;

        public BindableProperty<Sprite> Sprite { get; private set; }
        public BindableProperty<string> SpriteSources { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            image = GetComponent<Image>();

            Sprite = new BindableProperty<Sprite>(image.sprite);
            SpriteSources = new BindableProperty<string>();

            Sprite.PropertySet += OnSetSprite;
            SpriteSources.PropertySet += OnSetSpriteSources;
        }

        void OnSetSprite(Sprite oldValue, Sprite newValue) 
        {
            image.sprite = newValue;
        }

        void OnSetSpriteSources(string oldValue, string newValue)
        {
            image.sprite = this.AssetLoader.Load<Sprite>(newValue);
        }

        public override void Destroy()
        {
            base.Destroy();
            Sprite.ClearEvent();
            SpriteSources.ClearEvent();
        }
    }
}
