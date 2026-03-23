namespace FUI.UGUI
{
    public partial class View : IElement
    {
        string IElement.Name => (this as IView).Name;

        IElement IElement.Parent => null;
    }
}