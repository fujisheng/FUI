namespace FUI
{
    public interface IVisualElement<TValue>
    {
        void UpdateValue(TValue value);
    }

    public interface IVisualElement
    {
        void UpdateValue(object value);
    }
}
