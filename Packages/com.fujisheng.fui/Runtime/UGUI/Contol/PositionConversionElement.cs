using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
    [RequireComponent(typeof(RectTransform))]
    public class PositionConversionElement : UIElement<RectTransform>
    {
        [SerializeField]
        RectTransform targetRectTransform;

        public BindableProperty<(Vector2 screenPosition, Camera camera)> ScreenPointToWorldPoint { get; private set; }

        public BindableProperty<(Vector2 screenPosition, Camera camera)> ScreenPointToLocalPoint { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ScreenPointToWorldPoint = new BindableProperty<(Vector2 screenPosition, Camera camera)>();

            ScreenPointToWorldPoint.OnValueChanged += OnSetScreenPointToWorldPoint;

            ScreenPointToLocalPoint = new BindableProperty<(Vector2 screenPosition, Camera camera)>();
            ScreenPointToLocalPoint.OnValueChanged += OnSetScreenPointToLocalPoint;
        }

        void OnSetScreenPointToWorldPoint((Vector2 screenPosition, Camera camera) oldValue, (Vector2 screenPosition, Camera camera) newValue)
        {
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle(targetRectTransform, newValue.screenPosition, newValue.camera, out Vector3 position))
            {
                Position.Value = position;
            }
        }

        void OnSetScreenPointToLocalPoint((Vector2 screenPosition, Camera camera) oldValue, (Vector2 screenPosition, Camera camera) newValue)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRectTransform, newValue.screenPosition, newValue.camera, out Vector2 position))
            {
                LocalPosition.Value = position;
            }
        }
    }
}