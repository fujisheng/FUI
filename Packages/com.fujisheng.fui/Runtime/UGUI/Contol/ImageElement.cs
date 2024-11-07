using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Image))]
    public class ImageElement : UGUIView
    {
        Image image;

        /// <summary>
        /// 图片
        /// </summary>
        public BindableProperty<Sprite> Sprite { get; private set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public BindableProperty<string> SpriteSources { get; private set; }

        protected override void Initialize()
        {
            image = GetComponent<Image>();

            Sprite = new BindableProperty<Sprite>(image.sprite);
            SpriteSources = new BindableProperty<string>();

            Sprite.OnValueChanged += OnSetSprite;
            SpriteSources.OnValueChanged += OnSetSpriteSources;
        }

        void OnSetSprite(Sprite oldValue, Sprite newValue) 
        {
            image.sprite = newValue;
        }

        void OnSetSpriteSources(string oldValue, string newValue)
        {
            image.sprite = this.AssetLoader.Load<Sprite>(newValue);
        }

        protected override void Destroy()
        {
            Sprite.ClearValueChangedEvent();
            SpriteSources.ClearValueChangedEvent();
        }
    }
}
