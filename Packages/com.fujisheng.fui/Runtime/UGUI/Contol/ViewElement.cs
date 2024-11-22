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
        IView view;

        /// <summary>
        /// 数据
        /// </summary>
        public BindableProperty<ObservableObject> Data { get; private set; }

        protected override void Initialize()
        {
            view = View.Create(this.AssetLoader, this.gameObject, gameObject.name, true);

            Data = new BindableProperty<ObservableObject>(null, OnUpdateData);
        }

        void OnUpdateData(ObservableObject oldValue, ObservableObject newValue)
        {
            if (newValue == null)
            {
                return;
            }

            if (entity == null || newValue.GetType() != data.GetType())
            {
                entity = UIEntity.Create(view, newValue);
                entity.Enable();
            }
            else
            {
                entity.UpdateViewModel(newValue);
            }
            this.data = newValue;
        }

        protected override void Destroy()
        {
            Data.Dispose();
            entity.Disable();
            entity = null;
        }
    }
}
