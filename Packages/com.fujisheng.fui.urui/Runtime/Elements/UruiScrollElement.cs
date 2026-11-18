using System;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using global::URUI.Documents;
using global::URUI.Elements;
using global::URUI.Events;

namespace FUI.Rendering.URUI
{
    /// <summary>绑定普通 overflow 节点；内容、尺寸、滚动条和皮肤均由作者资源定义。</summary>
    public sealed class UruiScrollElement:UruiElement,IScrollViewElement
    {
        readonly Element content;
        readonly Document document;
        readonly IDisposable subscription;
        Vector2? pendingNormalized,expectedProgrammatic;
        int pendingIndex=-1,programmaticDepth;
        bool reading,initialized,disposed,horizontalWritten,verticalWritten;
        public UruiScrollElement(Element element,string name,Element content=null):base(ValidateContent(element,content),name)
        {
            this.content=content??element;document=element.Document;
            Horizontal=new BindableProperty<bool>(Axis("overflow-x"),null,value=>SetAxis(value,true));
            Vertical=new BindableProperty<bool>(Axis("overflow-y"),null,value=>SetAxis(value,false));
            NormalizedScroll=new BindableProperty<ScrollPoint>(new ScrollPoint(0,1),(_,value)=>
            {
                if(!reading){pendingNormalized=new Vector2(value.x,value.y);}
            },value=>
            {
                if(!float.IsFinite(value.x)||!float.IsFinite(value.y)){throw new ArgumentOutOfRangeException(nameof(value));}
                return new ScrollPoint(Mathf.Clamp01(value.x),Mathf.Clamp01(value.y));
            });
            ScrollChanged=new Command<ScrollPoint>();initialized=true;
            subscription=element.RegisterCallback<ScrollEvent>(evt=>
            {
                if(evt.Target!=element||programmaticDepth!=0||!IsLive){return;}
                Synchronize();if(!IsLive){return;}
                var expected=expectedProgrammatic;expectedProgrammatic=null;
                if(expected.HasValue&&Vector2.SqrMagnitude(element.ScrollOffset-expected.Value)<.01f){return;}
                document.RunAfterEvent(()=>{if(!disposed&&Element!=null&&Element.IsValid){ScrollChanged.Invoke(NormalizedScroll.Value);}});
            });
            document.LayoutUpdated+=Layout;
        }
        static Element ValidateContent(Element element,Element content)
        {
            if(element==null){throw new ArgumentNullException(nameof(element));}
            if(content!=null&&!ReferenceEquals(content,element)&&!element.Contains(content)){throw new ArgumentException("内容必须属于滚动节点。",nameof(content));}
            return element;
        }
        bool Axis(string property)=>(Element.HasLayout?Element.ResolvedStyle.Get(property):Element.Style.Get(property)) is "auto" or "scroll";
        bool SetAxis(bool value,bool horizontal)
        {
            if(initialized&&!reading)
            {
                if(horizontal){horizontalWritten=true;Element.Style.OverflowX=value?Overflow.Auto:Overflow.Hidden;}
                else{verticalWritten=true;Element.Style.OverflowY=value?Overflow.Auto:Overflow.Hidden;}
            }
            return value;
        }
        public BindableProperty<ScrollPoint> NormalizedScroll{get;}
        public BindableProperty<bool> Horizontal{get;}
        public BindableProperty<bool> Vertical{get;}
        public Command<ScrollPoint> ScrollChanged{get;}
        bool IsLive=>!disposed&&Element!=null&&Element.IsValid&&Element.HasLayout;
        public void RequestScrollToIndex(int index){if(index>=0){pendingIndex=index;}}
        Vector2 Range=>Vector2.Max(Vector2.zero,Element.ScrollSize-Element.ViewportRect.size);
        void ApplyOffset(Vector2 value)
        {
            var range=Range;var next=new Vector2(Horizontal.Value?Mathf.Clamp(value.x,0,range.x):0,Vertical.Value?Mathf.Clamp(value.y,0,range.y):0);
            if(Vector2.SqrMagnitude(Element.ScrollOffset-next)<.01f){return;}
            programmaticDepth++;
            try{Element.ScrollOffset=next;}
            finally{programmaticDepth--;}
            if(!IsLive){return;}
            expectedProgrammatic=Element.ScrollOffset;
        }
        void Layout(Document owner)
        {
            if(!IsLive){return;}
            reading=true;
            try
            {
                if(!horizontalWritten){Horizontal.Value=Axis("overflow-x");}
                if(!IsLive){return;}
                if(!verticalWritten){Vertical.Value=Axis("overflow-y");}
            }
            finally{reading=false;}
            if(!IsLive){return;}
            if(pendingNormalized.HasValue)
            {
                var desired=pendingNormalized.Value;pendingNormalized=null;var range=Range;
                ApplyOffset(new Vector2(desired.x*range.x,(1-desired.y)*range.y));
                if(!IsLive){return;}
            }
            if(content.IsValid&&pendingIndex>=0&&pendingIndex<content.ChildCount&&content[pendingIndex].HasLayout)
            {
                var target=content[pendingIndex];var center=Element.WorldToLocal(target.LocalToWorld(target.Layout.size*.5f));
                ApplyOffset(Element.ScrollOffset+center-Element.ViewportRect.center);pendingIndex=-1;
                if(!IsLive){return;}
            }
            Synchronize();
        }
        void Synchronize()
        {
            if(!IsLive){return;}
            var range=Range;var offset=Element.ScrollOffset;reading=true;
            try{NormalizedScroll.Value=new ScrollPoint(range.x>0?offset.x/range.x:0,range.y>0?1-offset.y/range.y:1);}
            finally{reading=false;}
        }
        public override void Dispose()
        {
            if(disposed){return;}disposed=true;subscription.Dispose();document.LayoutUpdated-=Layout;
            NormalizedScroll.Dispose();Horizontal.Dispose();Vertical.Dispose();ScrollChanged.Dispose();base.Dispose();
        }
    }
}
