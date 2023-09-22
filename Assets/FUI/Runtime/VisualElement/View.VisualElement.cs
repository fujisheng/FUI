using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// 当一个View作为一个视觉元素的时候
    /// </summary>
    public partial class View : IVisualElement, IVisualElement<ObservableObject>
    {
        void IVisualElement.UpdateValue(object value)
        {
            if(!(value is ObservableObject observable))
            {
                return;
            }

            UpdateValue(observable);
        }

        public void UpdateValue(ObservableObject value)
        {
            if(value == BindingContext)
            {
                return;
            }

            Unbinding();
            Binding(value);
        }
    }
}
