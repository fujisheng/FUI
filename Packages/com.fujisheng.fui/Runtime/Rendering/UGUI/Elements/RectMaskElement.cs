using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI RectMask2D 的裁剪边距和柔化范围公开为可绑定属性。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectMask2D))]
    [AddComponentMenu("FUI/Elements/Rect Mask")]
    public class RectMaskElement : ComponentElement<RectMask2D>
    {
        /// <summary>裁剪矩形四边的内边距。</summary>
        public BindableProperty<Vector4> Padding { get; private set; }

        /// <summary>水平和垂直方向的柔化像素数。</summary>
        public BindableProperty<Vector2Int> Softness { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Padding = CreateProperty(Component.padding, (oldValue, newValue) => Component.padding = newValue);
            Softness = CreateProperty(Component.softness, (oldValue, newValue) => Component.softness = newValue);
        }
    }
}
