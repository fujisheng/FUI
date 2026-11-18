using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;

using System;
using UnityEngine;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 管理父 View 所持有静态节点上的嵌套 ViewModel 投影。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("FUI/Elements/Nested View")]
    public class NestedViewElement : RectTransformElement, IModelElement, IContainerElement
    {
        private protected ViewInstance instance;
        protected View view;

        /// <summary>
        /// 当前嵌套投影使用的 ViewModel；设置为 null 时停用投影。
        /// </summary>
        public BindableProperty<ObservableObject> ViewModel { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ViewModel = CreateProperty<ObservableObject>(null, OnUpdateViewModel);
            TrackCleanup(ReleaseProjection);
        }

        protected virtual void OnUpdateViewModel(ObservableObject oldValue, ObservableObject newValue)
        {
            if (newValue == null)
            {
                // 空数据表示当前子投影不显示，但保留静态 View 以便后续复用。
                instance?.Disable();
                return;
            }

            if (instance != null && instance.ViewModel.GetType() == newValue.GetType())
            {
                instance.UpdateViewModel(newValue);
                instance.Enable();
                return;
            }

            // 数据类型变化意味着 BindingContext 类型也变化；完整结束旧投影后复用父节点持有的 GameObject。
            ReleaseProjection();
            view = CreateView() as View;
            instance = ViewInstance.Create(view, newValue);
            instance.Enable();
        }

        protected virtual IView CreateView()
        {
            return View.CreateProjection(AssetLoader, gameObject, this);
        }

        protected void ReleaseProjection()
        {
            instance?.DestroyProjection();
            instance = null;
            view?.Release();
            view = null;
        }
    }
}
