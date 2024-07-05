using FUI.Bindable;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// 把一个View作为一个视觉元素
    /// </summary>
    public class UGUIViewElement : UGUIVisualElement<ObservableObject>, IContainerElement
    {
        UIEntity entity;
        ObservableObject data;
        IView view;
        bool initialized = false;

        protected override void Initialize()
        {
            //因为在构造View的时候又会调用一次Initialize，所以这里需要判断一下
            if (initialized)
            {
                return;
            }

            initialized = true;
            view = new UGUIView(this.AssetLoader, this.gameObject, gameObject.name);
        }

        public override void UpdateValue(ObservableObject value)
        {
            if(value == null)
            {
                return;
            }

            if(entity == null || value.GetType() != data.GetType())
            {
                entity = UIEntity.Create(view, value);
                entity.Enable();
                UnityEngine.Debug.Log($"Create  {entity}");
            }
            else
            {
                entity.SetViewModel(value);
                UnityEngine.Debug.Log($"Set  {value}");
            }
            this.data = value;
        }
    }
}
