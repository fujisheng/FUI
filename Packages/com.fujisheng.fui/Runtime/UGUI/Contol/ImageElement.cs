﻿using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(Image))]
    public class ImageElement : UIElement<Image>
    {
        /// <summary>
        /// 图片
        /// </summary>
        public BindableProperty<Sprite> Sprite { get; private set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public BindableProperty<string> SpriteSource { get; private set; }

        /// <summary>
        /// 颜色
        /// </summary>
        public BindableProperty<Color> Color { get; private set; }

        /// <summary>
        /// 材质
        /// </summary>
        public BindableProperty<Material> Material { get; private set; }

        /// <summary>
        /// 材质资源路径
        /// </summary>
        public BindableProperty<string> MaterialSource { get; private set; }

        /// <summary>
        /// 填充率
        /// </summary>
        public BindableProperty<float> FillAmount { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Sprite = new BindableProperty<Sprite>(Component.sprite, (oldValue, newValue) => Component.sprite = newValue);
            SpriteSource = new BindableProperty<string>(null, (oldValue, newValue) =>
            {
                if (string.IsNullOrEmpty(newValue))
                {
                    return;
                }

                var sprite = AssetLoader.Load<Sprite>(newValue);
                if(sprite == null)
                {
                    return;
                }
                Sprite.Value = sprite; 
            });

            Color = new BindableProperty<Color>(Component.color, (oldValue, newValue) => Component.color = newValue);
            Material = new BindableProperty<Material>(Component.material, (oldValue, newValue) => Component.material = newValue);
            MaterialSource = new BindableProperty<string>(null, (oldValue, newValue) => Material.Value = AssetLoader.Load<Material>(newValue));
            FillAmount = new BindableProperty<float>(Component.fillAmount, (oldValue, newValue) => Component.fillAmount = newValue);
        }

        protected override void OnRelease()
        {
            base.OnRelease();

            Sprite.Dispose();
            SpriteSource.Dispose();
            Color.Dispose();
            Material.Dispose();
            MaterialSource.Dispose();
            FillAmount.Dispose();
        }
    }
}
