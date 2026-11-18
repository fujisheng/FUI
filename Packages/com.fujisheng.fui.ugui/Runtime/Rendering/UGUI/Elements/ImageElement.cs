using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Image 适配为可绑定图形，并以事务方式管理 Sprite 和 Material Lease。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("FUI/Elements/Image")]
    public class ImageElement : GraphicElement<Image>, IImageElement, IMaterialSourceElement
    {
        AssetSourceSlot<Sprite> spriteSlot;
        AssetSourceSlot<Material> materialSlot;

        /// <summary>当前 Sprite；直接赋值会清除 SpriteSource 及其 Lease。</summary>
        public BindableProperty<Sprite> Sprite { get; private set; }

        /// <summary>由 AssetLoader 事务加载的 Sprite 资源键。</summary>
        public BindableProperty<string> SpriteSource { get; private set; }

        /// <summary>由 AssetLoader 事务加载的 Material 资源键。</summary>
        public BindableProperty<string> MaterialSource { get; private set; }

        /// <summary>Image 的绘制类型。</summary>
        public BindableProperty<Image.Type> ImageType { get; private set; }

        /// <summary>是否保持 Sprite 原始宽高比。</summary>
        public BindableProperty<bool> PreserveAspect { get; private set; }

        /// <summary>Sliced 或 Tiled 模式下是否绘制中心区域。</summary>
        public BindableProperty<bool> FillCenter { get; private set; }

        /// <summary>Filled 模式使用的填充方法。</summary>
        public BindableProperty<Image.FillMethod> FillMethod { get; private set; }

        /// <summary>Filled 模式使用的填充起点。</summary>
        public BindableProperty<int> FillOrigin { get; private set; }

        /// <summary>Filled 模式是否顺时针填充。</summary>
        public BindableProperty<bool> FillClockwise { get; private set; }

        /// <summary>Filled 模式的填充比例。</summary>
        public BindableProperty<float> FillAmount { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            spriteSlot = Track(new AssetSourceSlot<Sprite>(AssetLoader, Name));
            materialSlot = Track(new AssetSourceSlot<Material>(AssetLoader, Name));

            Sprite = CreateProperty(Component.sprite, (oldValue, newValue) =>
            {
                Component.sprite = newValue;
                spriteSlot.OnDirectAssetChanged();
            });
            SpriteSource = CreateProperty<string>(null, spriteSlot.OnSourceChanged);
            MaterialSource = CreateProperty<string>(null, materialSlot.OnSourceChanged);
            spriteSlot.Attach(Sprite, SpriteSource);
            materialSlot.Attach(Material, MaterialSource);

            ImageType = CreateProperty(Component.type, (oldValue, newValue) => Component.type = newValue);
            PreserveAspect = CreateProperty(Component.preserveAspect, (oldValue, newValue) => Component.preserveAspect = newValue);
            FillCenter = CreateProperty(Component.fillCenter, (oldValue, newValue) => Component.fillCenter = newValue);
            FillMethod = CreateProperty(Component.fillMethod, (oldValue, newValue) => Component.fillMethod = newValue);
            FillOrigin = CreateProperty(Component.fillOrigin, (oldValue, newValue) => Component.fillOrigin = newValue);
            FillClockwise = CreateProperty(Component.fillClockwise, (oldValue, newValue) => Component.fillClockwise = newValue);
            FillAmount = CreateProperty(Component.fillAmount, (oldValue, newValue) => Component.fillAmount = newValue);
        }

        protected override void OnMaterialChanged(Material oldValue, Material newValue)
        {
            materialSlot?.OnDirectAssetChanged();
        }
    }
}
