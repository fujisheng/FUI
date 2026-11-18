using FUI.Binding;
namespace FUI.Rendering
{
    /// <summary>图片控件复用原资源键、填充与图形状态；资源实例属于后端。</summary>
    public interface IImageElement : IGraphicElement, IImageSourceElement, IFillElement
    {
        BindableProperty<bool> PreserveAspect { get; }
        BindableProperty<bool> FillCenter { get; }
        BindableProperty<int> FillOrigin { get; }
        BindableProperty<bool> FillClockwise { get; }
    }
    /// <summary>纹理控件沿用原string资源键，不把Unity纹理或UV类型带入Core。</summary>
    public interface IRawImageElement : IGraphicElement
    {
        BindableProperty<string> TextureSource { get; }
    }
    /// <summary>普通滚动控件使用现有ScrollPoint；用户事件与程序写值区分。</summary>
    public interface IScrollViewElement : IVisibilityElement, IScrollPositionElement
    {
        BindableProperty<bool> Horizontal { get; }
        BindableProperty<bool> Vertical { get; }
        Command<ScrollPoint> ScrollChanged { get; }
    }
}
