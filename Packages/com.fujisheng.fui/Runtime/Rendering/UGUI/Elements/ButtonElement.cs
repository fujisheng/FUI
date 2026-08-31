using FUI.Binding;

using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 将 UGUI Button 的交互状态和点击事件公开为可绑定成员。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("FUI/Elements/Button")]
    public class ButtonElement : SelectableElement<Button>
    {
        /// <summary>
        /// 点击回调
        /// </summary>
        public Command OnClick { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            OnClick = Track(new Command());
            Component.onClick.AddListener(OnButtonClick);
            TrackCleanup(() => Component?.onClick.RemoveListener(OnButtonClick));
        }

        void OnButtonClick()
        {
            OnClick?.Invoke();
        }

    }
}
