using FUI;
using FUI.Bindable;

public class PropertyBinder
{
    public TElementType GetElementProperty<TElementType>(IView view, string elementPath) where TElementType : IElement
    {
        if(!(view is FUI.IElement e))
        {
            throw new System.Exception($"{view.Name} not FUI.IElement");
        }

        var element = e.GetChild<TElementType>(elementPath);
        if(element == null)
        {
            throw new System.Exception($"{view.Name} not found {elementPath}:{typeof(TElementType)}");
        }

        return element;
    }
}