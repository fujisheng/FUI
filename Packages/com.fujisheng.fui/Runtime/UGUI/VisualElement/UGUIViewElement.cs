using FUI.Bindable;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// 把一个View作为一个视觉元素
    /// </summary>
    public class UGUIViewElement : UGUIVisualElement<ObservableObject>
    {
        UIEntity entity;
        ObservableObject data;
        IView view;

        protected override void Initialize()
        {
            view = new UGUIView(this.AssetLoader, this.gameObject, gameObject.name);
        }

        public override void UpdateValue(ObservableObject value)
        {
            if(entity == null || value.GetType() != data.GetType())
            {
                entity = UIEntity.Create(view, value);
                entity.SynchronizeProperties();
            }
            else
            {
                entity.SetViewModel(value);
            }
            this.data = value;
        }
    }
}
