using System;
using FUI.Binding;
using FUI.Presentation;
namespace FUI.Rendering
{
    /// <summary>原始三轴显示载荷；不转换坐标、布局或渲染。</summary>
    public readonly struct SpatialValue : IEquatable<SpatialValue>
    {
        public readonly float x,y,z;
        public SpatialValue(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public static SpatialValue zero => new SpatialValue(0f,0f,0f);
        public static SpatialValue one => new SpatialValue(1f,1f,1f);
        public bool Equals(SpatialValue other) => x.Equals(other.x)&&y.Equals(other.y)&&z.Equals(other.z);
        public override bool Equals(object value) => value is SpatialValue other&&Equals(other);
        public override int GetHashCode() {unchecked{return ((x.GetHashCode()*397)^y.GetHashCode())*397^z.GetHashCode();}}
    }
    /// <summary>保留原始世界位置和三轴值，不执行坐标转换。</summary>
    public interface IWorldPositionElement : IElement {BindableProperty<SpatialValue> WorldPositionValue {get;}}
    /// <summary>提供原始局部位置，不计算坐标转换。</summary>
    public interface ILocalPositionElement : IElement {BindableProperty<SpatialValue> LocalPositionValue {get;}}
    /// <summary>提供原始局部缩放，不计算布局。</summary>
    public interface ILocalScaleElement : IElement {BindableProperty<SpatialValue> LocalScaleValue {get;}}
    /// <summary>原始二维尺寸；不执行布局换算。</summary>
    public interface ISizeElement : IElement {BindableProperty<ScrollPoint> SizeValue {get;}}
    /// <summary>原始二维锚点位置；不执行坐标或布局转换。</summary>
    public interface IAnchoredPositionElement : IElement {BindableProperty<ScrollPoint> AnchoredPositionValue {get;}}
    /// <summary>通过既有真实模板创建受控子投影，后端拥有实例资源。</summary>
    public interface IProjectionTemplateElement : IElement {BindableProperty<ProjectionTemplate> Template {get;}}
}
