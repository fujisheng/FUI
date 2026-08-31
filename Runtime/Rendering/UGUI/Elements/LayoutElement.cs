using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI LayoutElement 的运行时布局约束公开为可绑定属性。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.LayoutElement))]
    [AddComponentMenu("FUI/Elements/Layout")]
    public class LayoutElement : ComponentElement<UnityEngine.UI.LayoutElement>
    {
        /// <summary>是否从父布局计算中排除。</summary>
        public BindableProperty<bool> IgnoreLayout { get; private set; }

        /// <summary>最小宽度。</summary>
        public BindableProperty<float> MinWidth { get; private set; }

        /// <summary>最小高度。</summary>
        public BindableProperty<float> MinHeight { get; private set; }

        /// <summary>首选宽度。</summary>
        public BindableProperty<float> PreferredWidth { get; private set; }

        /// <summary>首选高度。</summary>
        public BindableProperty<float> PreferredHeight { get; private set; }

        /// <summary>可伸缩宽度权重。</summary>
        public BindableProperty<float> FlexibleWidth { get; private set; }

        /// <summary>可伸缩高度权重。</summary>
        public BindableProperty<float> FlexibleHeight { get; private set; }

        /// <summary>布局优先级。</summary>
        public BindableProperty<int> LayoutPriority { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            IgnoreLayout = CreateProperty(Component.ignoreLayout, (oldValue, newValue) => Component.ignoreLayout = newValue);
            MinWidth = CreateProperty(Component.minWidth, (oldValue, newValue) => Component.minWidth = newValue);
            MinHeight = CreateProperty(Component.minHeight, (oldValue, newValue) => Component.minHeight = newValue);
            PreferredWidth = CreateProperty(Component.preferredWidth, (oldValue, newValue) => Component.preferredWidth = newValue);
            PreferredHeight = CreateProperty(Component.preferredHeight, (oldValue, newValue) => Component.preferredHeight = newValue);
            FlexibleWidth = CreateProperty(Component.flexibleWidth, (oldValue, newValue) => Component.flexibleWidth = newValue);
            FlexibleHeight = CreateProperty(Component.flexibleHeight, (oldValue, newValue) => Component.flexibleHeight = newValue);
            LayoutPriority = CreateProperty(Component.layoutPriority, (oldValue, newValue) => Component.layoutPriority = newValue);
        }
    }
}
