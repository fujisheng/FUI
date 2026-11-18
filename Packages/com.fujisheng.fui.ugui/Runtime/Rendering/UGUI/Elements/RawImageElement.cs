using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI RawImage 适配为可绑定图形，并以事务方式管理 Texture 和 Material Lease。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("FUI/Elements/Raw Image")]
    public class RawImageElement : GraphicElement<RawImage>, IRawImageElement
    {
        AssetSourceSlot<Texture> textureSlot;
        AssetSourceSlot<Material> materialSlot;

        /// <summary>当前 Texture；直接赋值会清除 TextureSource 及其 Lease。</summary>
        public BindableProperty<Texture> Texture { get; private set; }

        /// <summary>由 AssetLoader 事务加载的 Texture 资源键。</summary>
        public BindableProperty<string> TextureSource { get; private set; }

        /// <summary>由 AssetLoader 事务加载的 Material 资源键。</summary>
        public BindableProperty<string> MaterialSource { get; private set; }

        /// <summary>Texture 的 UV 采样区域。</summary>
        public BindableProperty<Rect> UVRect { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            textureSlot = Track(new AssetSourceSlot<Texture>(AssetLoader, Name));
            materialSlot = Track(new AssetSourceSlot<Material>(AssetLoader, Name));

            Texture = CreateProperty(Component.texture, (oldValue, newValue) =>
            {
                Component.texture = newValue;
                textureSlot.OnDirectAssetChanged();
            });
            TextureSource = CreateProperty<string>(null, textureSlot.OnSourceChanged);
            MaterialSource = CreateProperty<string>(null, materialSlot.OnSourceChanged);
            textureSlot.Attach(Texture, TextureSource);
            materialSlot.Attach(Material, MaterialSource);
            UVRect = CreateProperty(Component.uvRect, (oldValue, newValue) => Component.uvRect = newValue);
        }

        protected override void OnMaterialChanged(Material oldValue, Material newValue)
        {
            materialSlot?.OnDirectAssetChanged();
        }
    }
}
