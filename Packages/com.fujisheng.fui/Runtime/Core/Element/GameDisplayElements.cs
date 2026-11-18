using FUI.Binding;
namespace FUI.Rendering
{
    /// <summary>保持原遮罩洞口的可空矩形坐标。</summary>
    public readonly struct DisplayRect
    {
        public readonly float x,y,width,height;
        public DisplayRect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}
    }
    /// <summary>引导遮罩洞口；裁剪算法由后端保留。</summary>
    public interface IHoleMaskElement : IImageElement { BindableProperty<DisplayRect?> Hole { get; } }
    /// <summary>小地图资源名；保持后端原有缩放规则。</summary>
    public interface IMapImageElement : IImageElement { BindableProperty<string> MapName { get; } }
}
