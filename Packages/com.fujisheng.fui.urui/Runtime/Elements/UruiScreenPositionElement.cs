using System;
using FUI.Binding;
using FUI.Rendering;
using URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>屏幕位置复用世界UI平面的布局等待、祖先变换及所有权处理。</summary>
    public sealed class UruiScreenPositionElement:UruiElement,IScreenPositionElement
    {
        bool initialized;
        public UruiScreenPositionElement(Element element,string name):base(element,name)
        {
            ScreenPositionValue=new BindableProperty<PointerPosition>(default,null,value=>
            {
                if(!float.IsFinite(value.x)||!float.IsFinite(value.y)){throw new ArgumentOutOfRangeException(nameof(value));}
                if(initialized){WorldPositionValue.Value=new SpatialValue(value.x,value.y,0);}
                return value;
            });
            initialized=true;
        }
        public BindableProperty<PointerPosition> ScreenPositionValue{get;}
        public override void Dispose(){ScreenPositionValue.Dispose();base.Dispose();}
    }
}
