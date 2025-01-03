using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// 适用于UGUI的元素基类
    /// </summary>
    public abstract class UIElement : Element
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
        public BindableProperty<Vector2> AnchoredPostion { get; private set; }

        /// <summary>
        /// rectTransform.anchoredPosition3D
        /// </summary>
        public BindableProperty<Vector3> AnvhoredPosition3D { get; private set; }

        /// <summary>
        /// rectTransform.sizeDelta
        /// </summary>
        public BindableProperty<Vector2> SizeDelta { get; private set; }

        /// <summary>
        /// rectTransform.pivot
        /// </summary>
        public BindableProperty<Vector2> Piovt { get; private set; }

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

            RectTransform = transform.GetComponent<RectTransform>();

            AnchorMin = new BindableProperty<Vector2>(RectTransform.anchorMin, (oldValue, newValue) => RectTransform.anchorMin = newValue);
            AnchorMax = new BindableProperty<Vector2>(RectTransform.anchorMax, (oldValue, newValue) => RectTransform.anchorMax = newValue);
            AnchoredPostion = new BindableProperty<Vector2>(RectTransform.anchoredPosition, (oldValue, newValue) => RectTransform.anchoredPosition = newValue);
            AnvhoredPosition3D = new BindableProperty<Vector3>(RectTransform.anchoredPosition3D, (oldValue, newValue) => RectTransform.anchoredPosition3D = newValue);
            SizeDelta = new BindableProperty<Vector2>(RectTransform.sizeDelta, (oldValue, newValue) => RectTransform.sizeDelta = newValue);
            Piovt = new BindableProperty<Vector2>(RectTransform.pivot, (oldValue, newValue) => RectTransform.pivot = newValue);
            OffsetMin = new BindableProperty<Vector2>(RectTransform.offsetMin, (oldValue, newValue) => RectTransform.offsetMin = newValue);
            OffsetMax = new BindableProperty<Vector2>(RectTransform.offsetMax, (oldValue, newValue) => RectTransform.offsetMax = newValue);
        }

        protected override void OnRelease()
        {
            base.OnRelease();

            AnchorMin.Dispose();
            AnchorMax.Dispose();
            AnchoredPostion.Dispose();
            AnvhoredPosition3D.Dispose();
            SizeDelta.Dispose();
            Piovt.Dispose();
            OffsetMin.Dispose();
            OffsetMax.Dispose();
        }
    }

    /// <summary>
    /// 适用于UGUI的元素基类
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    public abstract class UIElement<TComponent> : UIElement where TComponent : Component
    {
        protected TComponent Component { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Component = GetComponent<TComponent>();
            if(Component == null)
            {
                throw new System.Exception($"{name} {this.GetType()} RequireComponent {typeof(TComponent)} is null!!!");
            }
        }
    }
}