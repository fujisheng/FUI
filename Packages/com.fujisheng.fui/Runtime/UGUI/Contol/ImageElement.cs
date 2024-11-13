﻿using FUI.Bindable;

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
        public BindableProperty<string> MaterialSources { get; private set; }

        /// <summary>
        /// 填充率
        /// </summary>
        public BindableProperty<float> FillAmount { get; private set; }

        protected override void Initialize()
        {
            image = GetComponent<Image>();

            Sprite = new BindableProperty<Sprite>(image.sprite);
            SpriteSources = new BindableProperty<string>();
            Color = new BindableProperty<Color>(image.color);
            Material = new BindableProperty<Material>(image.material);
            MaterialSources = new BindableProperty<string>();
            FillAmount = new BindableProperty<float>(image.fillAmount);

            Sprite.OnValueChanged += (oldValue, newValue) => image.sprite = newValue;
            SpriteSources.OnValueChanged += (oldValue, newValue) => image.sprite = AssetLoader.Load<Sprite>(newValue);
            Color.OnValueChanged += (oldValue, newValue) => image.color = newValue;
            Material.OnValueChanged += (oldValue, newValue) => image.material = newValue;
            MaterialSources.OnValueChanged += (oldValue, newValue) => image.material = AssetLoader.Load<Material>(newValue);
            FillAmount.OnValueChanged += (oldValue, newValue) => image.fillAmount = newValue;
        }

        protected override void Destroy()
        {
            Sprite.Dispose();
            SpriteSources.Dispose();
            Color.Dispose();
            Material.Dispose();
            MaterialSources.Dispose();
            FillAmount.Dispose();
        }
    }
}
