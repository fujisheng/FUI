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

        public BindableProperty<ObservableObject> Data { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            //因为在构造View的时候又会调用一次Initialize，所以这里需要判断一下
            if (initialized)
            {
                return;
            }

            initialized = true;
            view = UGUIView.Create(this.AssetLoader, this.gameObject, gameObject.name);

            Data = new BindableProperty<ObservableObject>();
            Data.PropertySet += OnSetData;
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
                UnityEngine.Debug.Log($"Create  {entity}");
            }
            else
            {
                entity.SetViewModel(newValue);
                UnityEngine.Debug.Log($"Set  {newValue}");
            }
            this.data = newValue;
        }
    }
}
