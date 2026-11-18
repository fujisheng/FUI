using FUI.Binding;

using UnityEngine;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 为具有 RectTransform 的 UGUI 对象公开布局和锚点绑定属性。
    /// </summary>
    public class RectTransformElement : Element, IVisibilityElement, IAnchoredPositionElement, ILocalPositionElement, ILocalScaleElement, ISizeElement
    {
        /// <summary>
        /// RectTransform
        /// </summary>
        protected RectTransform RectTransform { get; private set; }

        #region RectTransform
        /// <summary>
        /// rectTransform.anchorMin
        /// </summary>
        public BindableProperty<Vector2> AnchorMin { get; private set; }

        /// <summary>
        /// rectTransform.anchorMax
        /// </summary>
        public BindableProperty<Vector2> AnchorMax { get; private set; }

        /// <summary>
        /// rectTransform.anchoredPosition
        /// </summary>
        public BindableProperty<Vector2> AnchoredPosition { get; private set; }
        public BindableProperty<ScrollPoint> AnchoredPositionValue { get; private set; }
        public BindableProperty<SpatialValue> LocalPositionValue { get; private set; }
        public BindableProperty<ScrollPoint> SizeValue { get; private set; }
        public BindableProperty<SpatialValue> LocalScaleValue { get; private set; }

        /// <summary>
        /// rectTransform.anchoredPosition3D
        /// </summary>
        public BindableProperty<Vector3> AnchoredPosition3D { get; private set; }

        /// <summary>
        /// rectTransform.sizeDelta
        /// </summary>
        public BindableProperty<Vector2> SizeDelta { get; private set; }

        /// <summary>
        /// rectTransform.pivot
        /// </summary>
        public BindableProperty<Vector2> Pivot { get; private set; }

        /// <summary>
        /// rectTransform.offsetMin
        /// </summary>
        public BindableProperty<Vector2> OffsetMin { get; private set; }

        /// <summary>
        /// rectTransform.offsetMax
        /// </summary>
        public BindableProperty<Vector2> OffsetMax { get; private set; }

        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();

            RectTransform = transform as RectTransform;
            if (RectTransform == null)
            {
                throw new MissingComponentException($"Element '{name}' requires a RectTransform component.");
            }
            TrackCleanup(() => RectTransform = null);

            AnchorMin = CreateProperty(RectTransform.anchorMin, (oldValue, newValue) => RectTransform.anchorMin = newValue);
            AnchorMax = CreateProperty(RectTransform.anchorMax, (oldValue, newValue) => RectTransform.anchorMax = newValue);
            AnchoredPosition = CreateProperty(RectTransform.anchoredPosition, (oldValue, newValue) => RectTransform.anchoredPosition = newValue);
            AnchoredPositionValue = CreateProperty(new ScrollPoint(AnchoredPosition.Value.x, AnchoredPosition.Value.y),
                (oldValue, newValue) => AnchoredPosition.Value = new Vector2(newValue.x, newValue.y));
            ValueChangedHandler<Vector2> anchoredChanged = (oldValue, newValue) =>
                AnchoredPositionValue.Value = new ScrollPoint(newValue.x, newValue.y);
            AnchoredPosition.OnValueChanged += anchoredChanged;
            TrackCleanup(() => AnchoredPosition.OnValueChanged -= anchoredChanged);
            LocalPositionValue = CreateProperty(
                new SpatialValue(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z),
                (oldValue, newValue) => transform.localPosition = new Vector3(newValue.x, newValue.y, newValue.z));
            LocalScaleValue = CreateProperty(
                new SpatialValue(transform.localScale.x, transform.localScale.y, transform.localScale.z),
                (oldValue, newValue) => transform.localScale = new Vector3(newValue.x, newValue.y, newValue.z));
            AnchoredPosition3D = CreateProperty(RectTransform.anchoredPosition3D, (oldValue, newValue) => RectTransform.anchoredPosition3D = newValue);
            SizeDelta = CreateProperty(RectTransform.sizeDelta, (oldValue, newValue) => RectTransform.sizeDelta = newValue);
            SizeValue = CreateProperty(new ScrollPoint(SizeDelta.Value.x, SizeDelta.Value.y),
                (oldValue, newValue) => SizeDelta.Value = new Vector2(newValue.x, newValue.y));
            ValueChangedHandler<Vector2> sizeChanged = (oldValue, newValue) =>
                SizeValue.Value = new ScrollPoint(newValue.x, newValue.y);
            SizeDelta.OnValueChanged += sizeChanged;
            TrackCleanup(() => SizeDelta.OnValueChanged -= sizeChanged);
            Pivot = CreateProperty(RectTransform.pivot, (oldValue, newValue) => RectTransform.pivot = newValue);
            OffsetMin = CreateProperty(RectTransform.offsetMin, (oldValue, newValue) => RectTransform.offsetMin = newValue);
            OffsetMax = CreateProperty(RectTransform.offsetMax, (oldValue, newValue) => RectTransform.offsetMax = newValue);
        }

        protected override void OnSynchronizeExternalValues()
        {
            base.OnSynchronizeExternalValues();
            AnchorMin.SetValue(RectTransform.anchorMin);
            AnchorMax.SetValue(RectTransform.anchorMax);
            AnchoredPosition.SetValue(RectTransform.anchoredPosition);
            AnchoredPosition3D.SetValue(RectTransform.anchoredPosition3D);
            SizeDelta.SetValue(RectTransform.sizeDelta);
            Pivot.SetValue(RectTransform.pivot);
            OffsetMin.SetValue(RectTransform.offsetMin);
            OffsetMax.SetValue(RectTransform.offsetMax);
            LocalPositionValue.SetValue(new SpatialValue(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z));
            LocalScaleValue.SetValue(new SpatialValue(transform.localScale.x, transform.localScale.y, transform.localScale.z));
        }
    }

    /// <summary>
    /// 将一个必需的 Unity 组件适配为可绑定 Element。
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    public abstract class ComponentElement<TComponent> : RectTransformElement
    {
        protected TComponent Component { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Component = GetComponent<TComponent>();
            if (Component == null)
            {
                throw new MissingComponentException(
                    $"Element '{name}' ({GetType().FullName}) requires component '{typeof(TComponent).FullName}'.");
            }
            TrackCleanup(() => Component = default);
        }
    }
}
