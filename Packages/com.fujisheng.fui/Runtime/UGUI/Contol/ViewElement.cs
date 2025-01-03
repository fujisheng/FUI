using FUI.Bindable;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 把一个View作为一个视觉元素
    /// </summary>
    public class ViewElement : View, IContainerElement
    {
        UIEntity entity;
        ObservableObject data;

        /// <summary>
        /// 数据
        /// </summary>
        public BindableProperty<ObservableObject> Data { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Data = new BindableProperty<ObservableObject>(null, OnUpdateData);
        }

        void OnUpdateData(ObservableObject oldValue, ObservableObject newValue)
        {
            if (newValue == null)
            {
                return;
            }

            //如果这个Entity不为空且要更新的数据类型不是之前的数据类型  则需要先禁用之前的Entity
            if(entity != null && newValue.GetType() != data.GetType())
            {
                entity.Disable();
                entity = null;
            }

            if (entity == null || newValue.GetType() != data.GetType())
            {
                entity = UIEntity.Create(this, newValue);
                entity.Enable();
            }
            else
            {
                entity.UpdateViewModel(newValue);
            }
            this.data = newValue;
        }

        protected override void OnRelease()
        {
            Data.Dispose();
            entity.Disable();
            entity = null;
        }
    }
}
