namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 实现 View 作为根 IElement 时的名称和父级语义。
    /// </summary>
    public partial class View : IElement
    {
        string IElement.Name => (this as IView).Name;

        IElement IElement.Parent => null;
    }
}
