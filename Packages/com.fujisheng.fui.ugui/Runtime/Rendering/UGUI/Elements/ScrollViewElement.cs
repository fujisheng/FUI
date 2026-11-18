using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将普通 UGUI ScrollRect 的位置、速度和方向公开为可绑定属性。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("FUI/Elements/Scroll View")]
    public class ScrollViewElement : ComponentElement<ScrollRect>, IScrollViewElement
    {
        bool synchronizing;

        /// <summary>ScrollRect 的归一化位置。</summary>
        public BindableProperty<Vector2> NormalizedPosition { get; private set; }

        /// <summary>Content 的锚点位置。</summary>
        public BindableProperty<Vector2> ContentPosition { get; private set; }

        /// <summary>当前滚动速度。</summary>
        public BindableProperty<Vector2> Velocity { get; private set; }

        /// <summary>是否允许水平滚动。</summary>
        public BindableProperty<bool> Horizontal { get; private set; }

        /// <summary>是否允许垂直滚动。</summary>
        public BindableProperty<bool> Vertical { get; private set; }

        /// <summary>真实 ScrollRect 输入产生的位置变更事件。</summary>
        public Command<Vector2> OnValueChanged { get; private set; }

        /// <summary>原Vector2位置的中性转换；保留原NormalizedPosition对象作为后端状态。</summary>
        public BindableProperty<ScrollPoint> NormalizedScroll { get; private set; }

        /// <summary>原用户位置命令的一次中性转换，不新增ScrollRect监听。</summary>
        public Command<ScrollPoint> ScrollChanged { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (Component.content == null)
            {
                throw new MissingReferenceException($"Element '{Name}' requires ScrollRect.content.");
            }

            NormalizedPosition = CreateProperty(Component.normalizedPosition, OnNormalizedPositionChanged);
            var normalized = NormalizedPosition.Value;
            NormalizedScroll = CreateProperty(new ScrollPoint(normalized.x, normalized.y), (oldValue, newValue) =>
                NormalizedPosition.Value = new Vector2(newValue.x, newValue.y));
            ValueChangedHandler<Vector2> onNormalizedChanged = (oldValue, newValue) =>
                NormalizedScroll.Value = new ScrollPoint(newValue.x, newValue.y);
            NormalizedPosition.OnValueChanged += onNormalizedChanged;
            TrackCleanup(() => NormalizedPosition.OnValueChanged -= onNormalizedChanged);
            ContentPosition = CreateProperty(Component.content.anchoredPosition, OnContentPositionChanged);
            Velocity = CreateProperty(Component.velocity, (oldValue, newValue) => Component.velocity = newValue);
            Horizontal = CreateProperty(Component.horizontal, (oldValue, newValue) => Component.horizontal = newValue);
            Vertical = CreateProperty(Component.vertical, (oldValue, newValue) => Component.vertical = newValue);
            OnValueChanged = Track(new Command<Vector2>());

            ScrollChanged = Track(new Command<ScrollPoint>());
            System.Action<Vector2> onScrollChanged = value => ScrollChanged.Invoke(new ScrollPoint(value.x, value.y));
            OnValueChanged.AddListener(onScrollChanged);
            TrackCleanup(() => OnValueChanged.RemoveListener(onScrollChanged));

            Component.onValueChanged.AddListener(OnScrollValueChanged);
            TrackCleanup(() => Component?.onValueChanged.RemoveListener(OnScrollValueChanged));
        }

        void OnNormalizedPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (synchronizing)
            {
                return;
            }
            Synchronize(() =>
            {
                Component.normalizedPosition = newValue;
                ContentPosition.SetValue(Component.content.anchoredPosition);
            });
        }

        void OnContentPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (synchronizing)
            {
                return;
            }
            Synchronize(() =>
            {
                Component.content.anchoredPosition = newValue;
                NormalizedPosition.SetValue(Component.normalizedPosition);
            });
        }

        void OnScrollValueChanged(Vector2 value)
        {
            if (synchronizing)
            {
                return;
            }
            Synchronize(() =>
            {
                NormalizedPosition.SetValue(value);
                ContentPosition.SetValue(Component.content.anchoredPosition);
                Velocity.SetValue(Component.velocity);
            });
            OnValueChanged.Invoke(value);
        }

        void Synchronize(System.Action action)
        {
            synchronizing = true;
            try
            {
                action();
            }
            finally
            {
                synchronizing = false;
            }
        }
    }
}
