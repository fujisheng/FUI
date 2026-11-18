using FUI.Binding;
using UnityEngine;
namespace FUI.Rendering.UGUI
{
    /// <summary>以原Vector3属性适配三轴语义值，保持原Transform同步及生命周期。</summary>
    public partial class Element
    {
        /// <summary>原Position的世界三轴载荷。</summary>
        public BindableProperty<SpatialValue> WorldPositionValue {get;private set;}
        /// <summary>原LocalPosition的原始三轴载荷。</summary>
        public BindableProperty<SpatialValue> LocalPositionValue {get;private set;}
        /// <summary>原LocalScale的原始三轴载荷。</summary>
        public BindableProperty<SpatialValue> LocalScaleValue {get;private set;}
        public BindableProperty<RotationValue> RotationValue {get;private set;}
        public BindableProperty<RotationValue> LocalRotationValue {get;private set;}
        partial void InitializeSpatialValues()
        {
            WorldPositionValue=CreateProperty(ToSpatial(Position.Value),(oldValue,newValue)=>Position.Value=ToNative(newValue));
            ValueChangedHandler<Vector3> worldChanged=(oldValue,newValue)=>WorldPositionValue.Value=ToSpatial(newValue);
            Position.OnValueChanged+=worldChanged;
            TrackCleanup(()=>Position.OnValueChanged-=worldChanged);
            LocalPositionValue=CreateProperty(ToSpatial(LocalPosition.Value),(oldValue,newValue)=>LocalPosition.Value=ToNative(newValue));
            ValueChangedHandler<Vector3> positionChanged=(oldValue,newValue)=>LocalPositionValue.Value=ToSpatial(newValue);
            LocalPosition.OnValueChanged+=positionChanged;
            TrackCleanup(()=>LocalPosition.OnValueChanged-=positionChanged);
            RotationValue=CreateProperty(ToRotation(Rotation.Value),(oldValue,newValue)=>Rotation.Value=ToNative(newValue));
            ValueChangedHandler<Quaternion> rotationChanged=(oldValue,newValue)=>RotationValue.Value=ToRotation(newValue);
            Rotation.OnValueChanged+=rotationChanged;
            TrackCleanup(()=>Rotation.OnValueChanged-=rotationChanged);
            LocalRotationValue=CreateProperty(ToRotation(LocalRotation.Value),(oldValue,newValue)=>LocalRotation.Value=ToNative(newValue));
            ValueChangedHandler<Quaternion> localRotationChanged=(oldValue,newValue)=>LocalRotationValue.Value=ToRotation(newValue);
            LocalRotation.OnValueChanged+=localRotationChanged;
            TrackCleanup(()=>LocalRotation.OnValueChanged-=localRotationChanged);
            LocalScaleValue=CreateProperty(ToSpatial(LocalScale.Value),(oldValue,newValue)=>LocalScale.Value=ToNative(newValue));
            ValueChangedHandler<Vector3> scaleChanged=(oldValue,newValue)=>LocalScaleValue.Value=ToSpatial(newValue);
            LocalScale.OnValueChanged+=scaleChanged;
            TrackCleanup(()=>LocalScale.OnValueChanged-=scaleChanged);
        }
        static RotationValue ToRotation(Quaternion value)=>new RotationValue(value.x,value.y,value.z,value.w);
        static Quaternion ToNative(RotationValue value)=>new Quaternion(value.x,value.y,value.z,value.w);
        static SpatialValue ToSpatial(Vector3 value)=>new SpatialValue(value.x,value.y,value.z);
        static Vector3 ToNative(SpatialValue value)=>new Vector3(value.x,value.y,value.z);
    }
}
