using System;
using System.Collections.Generic;
using FUI.Binding;
namespace FUI.Rendering
{
    /// <summary>原始四元数，不执行旋转或坐标系转换。</summary>
    public readonly struct RotationValue : IEquatable<RotationValue>
    {
        public readonly float x,y,z,w;
        public RotationValue(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
        public bool Equals(RotationValue other)=>x.Equals(other.x)&&y.Equals(other.y)&&z.Equals(other.z)&&w.Equals(other.w);
        public override bool Equals(object other)=>other is RotationValue value&&Equals(value);
        public override int GetHashCode(){unchecked{return (((x.GetHashCode()*397)^y.GetHashCode())*397^z.GetHashCode())*397^w.GetHashCode();}}
    }
    /// <summary>绑定原始四元数的控件。</summary>
    public interface IQuaternionRotationElement : IElement {BindableProperty<RotationValue> RotationValue {get;}}
    /// <summary>原始屏幕指针坐标，不包含 Unity 事件对象。</summary>
    public readonly struct PointerPosition : IEquatable<PointerPosition>
    {
        public readonly float x,y;
        public PointerPosition(float x,float y){this.x=x;this.y=y;}
        public bool Equals(PointerPosition other) => x.Equals(other.x) && y.Equals(other.y);
        public override bool Equals(object value) => value is PointerPosition other && Equals(other);
        public override int GetHashCode() => unchecked((x.GetHashCode() * 397) ^ y.GetHashCode());
    }
    /// <summary>公开屏幕指针拖拽三个阶段的控件。</summary>
    public interface IPointerDragElement : IElement
    {
        Command<PointerPosition> DragStarted {get;}
        Command<PointerPosition> DragMoved {get;}
        Command<PointerPosition> DragEnded {get;}
    }
    /// <summary>可选的拖拽取消能力；取消与正常结束分开，避免触发提交业务。</summary>
    public interface IPointerDragCancellationElement : IElement
    {
        Command<PointerPosition> DragCancelled {get;}
    }
    /// <summary>原始三轴端点和颜色；布局、连线绘制由后端执行。</summary>
    public readonly struct LineSegment
    {
        public readonly SpatialValue from,to;
        public readonly Rgba color;
        public LineSegment(SpatialValue from,SpatialValue to,Rgba color){this.from=from;this.to=to;this.color=color;}
    }
    /// <summary>接收中立端点与颜色列表的连线控件。</summary>
    public interface IConnectionLinesElement : IElement {BindableProperty<IReadOnlyList<LineSegment>> LineSegments {get;}}
}
