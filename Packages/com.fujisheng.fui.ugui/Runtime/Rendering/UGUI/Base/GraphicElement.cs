using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 为 UGUI 图形组件公开通用的颜色、材质、射线和遮罩绑定属性。
    /// </summary>
    public abstract class GraphicElement : RectTransformElement, IGraphicElement
    {
        /// <summary>获取当前 Element 适配的图形组件。</summary>
        protected abstract MaskableGraphic GraphicComponent { get; }

        /// <summary>图形颜色。</summary>
        public BindableProperty<Color> Color { get; private set; }

        public BindableProperty<Rgba> Tint { get; private set; }

        /// <summary>图形材质。</summary>
        public BindableProperty<Material> Material { get; private set; }

        /// <summary>是否参与射线检测。</summary>
        public BindableProperty<bool> RaycastTarget { get; private set; }

        /// <summary>是否受父级遮罩影响。</summary>
        public BindableProperty<bool> Maskable { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (GraphicComponent == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) does not provide a graphic component.");
            }

            Color = CreateProperty(GraphicComponent.color, (oldValue, newValue) => GraphicComponent.color = newValue);
            var color = Color.Value;
            Tint = CreateProperty(new Rgba(color.r,color.g,color.b,color.a), (oldValue,newValue) =>
                Color.Value = new Color(newValue.r,newValue.g,newValue.b,newValue.a));
            ValueChangedHandler<Color> onColorChanged = (oldValue,newValue) =>
                Tint.Value = new Rgba(newValue.r,newValue.g,newValue.b,newValue.a);
            Color.OnValueChanged += onColorChanged;
            TrackCleanup(() => Color.OnValueChanged -= onColorChanged);
            Material = CreateProperty(GraphicComponent.material, OnMaterialChangedInternal);
            RaycastTarget = CreateProperty(GraphicComponent.raycastTarget, (oldValue, newValue) => GraphicComponent.raycastTarget = newValue);
            Maskable = CreateProperty(GraphicComponent.maskable, (oldValue, newValue) => GraphicComponent.maskable = newValue);
        }

        void OnMaterialChangedInternal(Material oldValue, Material newValue)
        {
            GraphicComponent.material = newValue;
            OnMaterialChanged(oldValue, newValue);
        }

        /// <summary>在材质属性被直接修改后通知派生类更新资源所有权。</summary>
        protected virtual void OnMaterialChanged(Material oldValue, Material newValue)
        {
        }
    }

    /// <summary>
    /// 将一个必需的 UGUI 图形组件适配为可绑定 Element。
    /// </summary>
    /// <typeparam name="TGraphic">被适配的图形组件类型。</typeparam>
    public abstract class GraphicElement<TGraphic> : GraphicElement where TGraphic : MaskableGraphic
    {
        /// <summary>获取当前 Element 适配的强类型图形组件。</summary>
        protected TGraphic Component { get; private set; }

        /// <inheritdoc />
        protected sealed override MaskableGraphic GraphicComponent => Component;

        protected override void OnInitialize()
        {
            Component = GetComponent<TGraphic>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TGraphic).FullName}'.");
            }

            TrackCleanup(() => Component = null);
            base.OnInitialize();
        }
    }
}
