namespace FUI
{
    public interface IVisualElement<in TValue>
    {
        void UpdateValue(TValue value);
    }

    public interface IVisualElement
    {
        void UpdateValue(object value);
    }
}
