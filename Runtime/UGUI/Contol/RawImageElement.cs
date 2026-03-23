using FUI.Bindable;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI.Control
{
    [RequireComponent(typeof(RawImage))]
    public class RawImageElement : UIElement<RawImage>
    {
        /// <summary>
        /// 图片
        /// </summary>
        public BindableProperty<Texture> Texture { get; private set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public BindableProperty<string> TextureSource { get; private set; }

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


        protected override void OnInitialize()
        {
            base.OnInitialize();

            Texture = new BindableProperty<Texture>(Component.texture, (oldValue, newValue) => Component.texture = newValue);
            TextureSource = new BindableProperty<string>(null, (oldValue, newValue) => Texture.Value = AssetLoader.Load<Texture>(newValue));
            Color = new BindableProperty<Color>(Component.color, (oldValue, newValue) => Component.color = newValue);
            Material = new BindableProperty<Material>(Component.material, (oldValue, newValue) => Component.material = newValue);
            MaterialSource = new BindableProperty<string>(null, (oldValue, newValue) => Material.Value = AssetLoader.Load<Material>(newValue));
        }

        protected override void OnRelease()
        {
            base.OnRelease();

            Texture.Dispose();
            TextureSource.Dispose();
            Color.Dispose();
            Material.Dispose();
            MaterialSource.Dispose();
        }
    }
}
