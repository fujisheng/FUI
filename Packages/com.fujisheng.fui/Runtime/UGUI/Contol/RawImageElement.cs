using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(RawImage))]
    public class RawImageElement : UGUIView
    {
        RawImage image;

        /// <summary>
        /// 图片
        /// </summary>
        public BindableProperty<Texture> Texture { get; private set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public BindableProperty<string> TextureSources { get; private set; }

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


        protected override void Initialize()
        {
            image = GetComponent<RawImage>();

            Texture = new BindableProperty<Texture>(image.texture);
            TextureSources = new BindableProperty<string>();
            Color = new BindableProperty<Color>(image.color);
            Material = new BindableProperty<Material>(image.material);
            MaterialSources = new BindableProperty<string>();

            Texture.OnValueChanged += (oldValue, newValue) => image.texture = newValue;
            TextureSources.OnValueChanged += (oldValue, newValue) => image.texture = AssetLoader.Load<Texture>(newValue);
            Color.OnValueChanged += (oldValue, newValue) => image.color = newValue;
            Material.OnValueChanged += (oldValue, newValue) => image.material = newValue;
            MaterialSources.OnValueChanged += (oldValue, newValue) => image.material = AssetLoader.Load<Material>(newValue);
        }

        protected override void Destroy()
        {
            Texture.Dispose();
            TextureSources.Dispose();
            Color.Dispose();
            Material.Dispose();
            MaterialSources.Dispose();
        }
    }
}
