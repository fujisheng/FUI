using FUI.Bindable;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// 把一个View作为一个视觉元素
    /// </summary>
    public class UGUIViewElement : UGUIView, IContainerElement
    {
        UIEntity entity;
        ObservableObject data;
        IView view;
        bool initialized = false;

        /// <summary>
        /// 数据
        /// </summary>
        public BindableProperty<ObservableObject> Data { get; private set; }

        protected override void Initialize()
        {
            //因为在构造View的时候又会调用一次Initialize，所以这里需要判断一下
            if (initialized)
            {
                return;
            }

            initialized = true;
            view = UGUIView.Create(this.AssetLoader, this.gameObject, gameObject.name);

            Data = new BindableProperty<ObservableObject>(null, OnSetData);
        }

        void OnSetData(ObservableObject oldValue, ObservableObject newValue)
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
                entity.SetViewModel(newValue);
            }
            this.data = newValue;
        }

        protected override void Destroy()
        {
            Data.Dispose();
        }
    }
}
