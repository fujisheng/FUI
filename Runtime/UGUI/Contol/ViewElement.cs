using FUI.Bindable;

using System;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 把一个静态的View作为一个视觉元素
    /// </summary>
    public class ViewElement : UIElement, IContainerElement
    {
        protected UIEntity entity;
        protected View view;

        /// <summary>
        /// 数据
        /// </summary>
        public BindableProperty<ObservableObject> Data { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Data = new BindableProperty<ObservableObject>(null, OnUpdateData);
        }

        protected virtual void OnUpdateData(ObservableObject oldValue, ObservableObject newValue)
        {
            if (newValue == null)
            {
                return;
            }

            //如果这个Entity不为空且要更新的数据类型不是之前的数据类型  则需要先禁用之前的Entity
            if (entity != null && newValue.GetType() != oldValue?.GetType())
            {
                entity.Disable();
                entity = null;
            }

            if (entity == null || newValue.GetType() != oldValue.GetType())
            {
                view = CreateView() as View;
                entity = UIEntity.Create(view, newValue);
                entity.Enable();
            }
            else
            {
                entity.UpdateViewModel(newValue);
            }
        }

        protected virtual IView CreateView()
        {
            return View.Create(AssetLoader, gameObject);
        }

        protected override void OnRelease()
        {
            Data.Dispose();
            entity?.Disable();
            entity = null;
            view?.Release();
            view = null;
        }
    }
}
