using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using URUI.Elements;
using URUI.Events;

namespace FUI.Rendering.URUI
{
    /// <summary>普通节点的单指针接触；参考平面由组合方提供，取消不触发正常释放。</summary>
    public sealed class UruiPointerElement:UruiElement,IPointerContactElement
    {
        readonly Element plane;
        readonly List<IDisposable> subscriptions=new();
        int? activePointer;
        PointerContact last;
        bool disposed;
        static Element Validate(Element element,Element plane)
        {
            if(element==null){throw new ArgumentNullException(nameof(element));}
            if(plane==null){throw new ArgumentNullException(nameof(plane));}
            if(!ReferenceEquals(element.Document,plane.Document)){throw new ArgumentException("指针参考平面必须属于同一文档。",nameof(plane));}
            return element;
        }
        public UruiPointerElement(Element element,string name,Element plane):base(Validate(element,plane),name)
        {
            this.plane=plane;PointerPressed=new();PointerMoved=new();PointerReleased=new();PointerCancelled=new();
            subscriptions.Add(element.RegisterCallback<PointerDownEvent>(Down));
            subscriptions.Add(element.RegisterCallback<PointerMoveEvent>(Move));
            subscriptions.Add(element.RegisterCallback<PointerUpEvent>(Up));
            subscriptions.Add(element.RegisterCallback<PointerCancelEvent>(evt=>Cancel(evt.PointerId)));
            subscriptions.Add(element.RegisterCallback<PointerCaptureOutEvent>(evt=>Cancel(evt.PointerId)));
            subscriptions.Add(element.RegisterCallback<DetachFromDocumentEvent>(_=>CancelActive()));
        }
        public Command<PointerContact> PointerPressed{get;}
        public Command<PointerContact> PointerMoved{get;}
        public Command<PointerContact> PointerReleased{get;}
        public Command<PointerContact> PointerCancelled{get;}
        PointerContact Read(int id,Vector2 screen)
        {
            var local=plane.WorldToLocal(screen);
            return new PointerContact(id,new PointerPosition(screen.x,Screen.height-screen.y),
                new PointerPosition(local.x-plane.Layout.width*ReadPivot(plane,"x"),plane.Layout.height*(1-ReadPivot(plane,"y"))-local.y));
        }
        void Publish(Command<PointerContact> command,PointerContact value)
        {Element.Document.RunAfterEvent(()=>{if(!disposed){command.Invoke(value);}});}
        void Down(PointerDownEvent evt)
        {
            if(disposed||activePointer.HasValue||evt.Button!=0||evt.IsDefaultPrevented||!plane.HasLayout||!Element.EnabledInHierarchy){return;}
            activePointer=evt.PointerId;last=Read(evt.PointerId,evt.Position);Element.CapturePointer(evt.PointerId);
            evt.PreventDefault();evt.StopPropagation();Publish(PointerPressed,last);
        }
        void Move(PointerMoveEvent evt)
        {
            if(activePointer!=evt.PointerId){return;}last=Read(evt.PointerId,evt.Position);
            evt.PreventDefault();evt.StopPropagation();Publish(PointerMoved,last);
        }
        void Up(PointerUpEvent evt)
        {
            if(activePointer!=evt.PointerId){return;}last=Read(evt.PointerId,evt.Position);activePointer=null;
            Element.ReleasePointer(evt.PointerId);evt.PreventDefault();evt.StopPropagation();Publish(PointerReleased,last);
        }
        void Cancel(int id)
        {
            if(activePointer!=id){return;}activePointer=null;if(Element.IsValid){Element.ReleasePointer(id);}Publish(PointerCancelled,last);
        }
        void CancelActive(){if(activePointer.HasValue){Cancel(activePointer.Value);}}
        public override void Dispose()
        {
            if(disposed){return;}disposed=true;var pointer=activePointer;activePointer=null;
            try{if(pointer.HasValue){PointerCancelled.Invoke(last);}}
            finally
            {
                if(pointer.HasValue&&Element.IsValid){Element.ReleasePointer(pointer.Value);}
                foreach(var subscription in subscriptions){subscription.Dispose();}subscriptions.Clear();
                PointerPressed.Dispose();PointerMoved.Dispose();PointerReleased.Dispose();PointerCancelled.Dispose();base.Dispose();
            }
        }
    }
}
