using FUI.Binding;
using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>指定屏幕坐标转换后的输出空间。</summary>
    public enum ScreenCoordinateSpace
    {
        /// <summary>输出世界坐标。</summary>
        World,
        /// <summary>输出目标 RectTransform 的本地坐标。</summary>
        Local,
    }

    /// <summary>
    /// 将屏幕坐标转换为指定 RectTransform 空间中的位置。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/Screen Position")]
    public class ScreenPositionElement : RectTransformElement, IScreenPositionElement
    {
        [SerializeField]
        RectTransform targetRectTransform;

        /// <summary>待转换的屏幕坐标。</summary>
        public BindableProperty<Vector2> ScreenPosition { get; private set; }

        /// <summary>坐标转换使用的相机；Overlay Canvas 可为 null。</summary>
        public BindableProperty<Camera> Camera { get; private set; }

        /// <summary>转换结果写入的坐标空间。</summary>
        public BindableProperty<ScreenCoordinateSpace> CoordinateSpace { get; private set; }

        public BindableProperty<PointerPosition> ScreenPositionValue {get;private set;}

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (targetRectTransform == null)
            {
                targetRectTransform = transform.parent as RectTransform;
            }
            if (targetRectTransform == null)
            {
                throw new MissingReferenceException(
                    $"Element '{Name}' requires a target RectTransform or a RectTransform parent.");
            }

            ScreenPositionValue = CreateProperty(default(PointerPosition), (_, value) =>
            {
                var canvas = GetComponentInParent<Canvas>();
                var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRectTransform, new Vector2(value.x,value.y), camera, out var point))
                { LocalPosition.Value = point; }
            });
            ScreenPosition = CreateProperty(Vector2.zero, (oldValue, newValue) => UpdatePosition());
            Camera = CreateProperty<Camera>(null, (oldValue, newValue) => UpdatePosition());
            CoordinateSpace = CreateProperty(ScreenCoordinateSpace.World, (oldValue, newValue) => UpdatePosition());
        }

        void UpdatePosition()
        {
            if (CoordinateSpace.Value == ScreenCoordinateSpace.World)
            {
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    targetRectTransform, ScreenPosition.Value, Camera.Value, out var worldPosition))
                {
                    Position.Value = worldPosition;
                }
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRectTransform, ScreenPosition.Value, Camera.Value, out var localPosition))
            {
                LocalPosition.Value = localPosition;
            }
        }
    }
}
