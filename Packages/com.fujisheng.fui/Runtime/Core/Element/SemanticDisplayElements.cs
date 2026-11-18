using System;
using FUI.Binding;
namespace FUI.Rendering
{
    /// <summary>不依赖渲染后端的颜色通道；不执行颜色空间转换。</summary>
    public readonly struct Rgba : IEquatable<Rgba>
    {
        public readonly float r,g,b,a;
        public Rgba(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public bool Equals(Rgba other) => r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
        public override bool Equals(object other) => other is Rgba value && Equals(value);
        public override int GetHashCode() { unchecked { return (((r.GetHashCode()*397)^g.GetHashCode())*397^b.GetHashCode())*397^a.GetHashCode(); } }
    }
    /// <summary>归一化滚动位置值；不包含屏幕或坐标转换。</summary>
    public readonly struct ScrollPoint : IEquatable<ScrollPoint>
    {
        public readonly float x,y;
        public ScrollPoint(float x,float y) { this.x=x;this.y=y; }
        public bool Equals(ScrollPoint other) => x.Equals(other.x) && y.Equals(other.y);
        public override bool Equals(object other) => other is ScrollPoint point && Equals(point);
        public override int GetHashCode() { unchecked { return (x.GetHashCode()*397)^y.GetHashCode(); } }
        public static bool operator ==(ScrollPoint left,ScrollPoint right) => left.Equals(right);
        public static bool operator !=(ScrollPoint left,ScrollPoint right) => !left.Equals(right);
    }
    /// <summary>提供图片资源键，不承担资源实例化。</summary>
    public interface IImageSourceElement : IElement { BindableProperty<string> SpriteSource { get; } }
    /// <summary>保留后端加载材质所用的原始资源键。</summary>
    public interface IMaterialSourceElement : IElement { BindableProperty<string> MaterialSource { get; } }
    /// <summary>提供填充比例，不引入值域或布局策略。</summary>
    public interface IFillElement : IElement { BindableProperty<float> FillAmount { get; } }
    /// <summary>提供真实子视图模型，具体投影由后端拥有。</summary>
    public interface IModelElement : IElement { BindableProperty<ObservableObject> ViewModel { get; } }
    /// <summary>提供中性颜色通道，不转换颜色空间。</summary>
    public interface IColorElement : IElement { BindableProperty<Rgba> Tint { get; } }
    /// <summary>提供归一化滚动值，不计算屏幕坐标。</summary>
    public interface IScrollPositionElement : IElement { BindableProperty<ScrollPoint> NormalizedScroll { get; } }
    /// <summary>提供动画序号与完成命令，不规定动画实现。</summary>
    public interface IAnimationElement : IElement { BindableProperty<int?> PlayingIndex { get; } Command<int> OnPlayComplete { get; } }
}
