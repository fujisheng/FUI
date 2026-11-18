using System;
using FUI.Binding;
using UnityEngine;
using URUI.Elements;
using URUI.Events;

namespace FUI.Rendering.URUI
{
    /// <summary>普通 overflow 作者容器的双向归一化滚动位置；轴、皮肤和手势由作者组合提供。</summary>
    public sealed class UruiScrollPositionElement:UruiElement,IScrollPositionElement
    {
        readonly IDisposable subscription;
        bool reading,pending;
        public UruiScrollPositionElement(Element element,string name):base(element,name)
        {
            NormalizedScroll=new BindableProperty<ScrollPoint>(new ScrollPoint(0,1),(_,value)=>
            {
                if(reading){return;}pending=true;Write();
            },value=>float.IsFinite(value.x)&&float.IsFinite(value.y)?new ScrollPoint(Mathf.Clamp01(value.x),Mathf.Clamp01(value.y)):throw new ArgumentOutOfRangeException(nameof(value)));
            subscription=element.RegisterCallback<ScrollEvent>(evt=>{if(ReferenceEquals(evt.Target,element)&&!pending){Read();}});
            element.Document.LayoutUpdated+=Layout;
        }
        public BindableProperty<ScrollPoint> NormalizedScroll{get;}
        Vector2 Range=>Vector2.Max(Vector2.zero,Element.ScrollSize-Element.ContentRect.size);
        void Write()
        {
            if(!pending||!Element.HasLayout){return;}
            var range=Range;var value=NormalizedScroll.Value;
            Element.ScrollOffset=new Vector2(value.x*range.x,(1-value.y)*range.y);pending=false;
        }
        void Read()
        {
            if(!Element.HasLayout){return;}var range=Range;var offset=Element.ScrollOffset;reading=true;
            try{NormalizedScroll.Value=new ScrollPoint(range.x>0?offset.x/range.x:0,range.y>0?1-offset.y/range.y:1);}finally{reading=false;}
        }
        void Layout(global::URUI.Documents.Document owner){if(pending){Write();}else{Read();}}
        public override void Dispose(){Element.Document.LayoutUpdated-=Layout;subscription.Dispose();NormalizedScroll.Dispose();base.Dispose();}
    }
}
