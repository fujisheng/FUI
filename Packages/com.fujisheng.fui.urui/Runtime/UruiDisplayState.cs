using System;
using System.Runtime.CompilerServices;
using FUI.Binding;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>同一节点的多个能力适配器共享逻辑显隐，最后一个适配器释放时才解除订阅。</summary>
    internal sealed class UruiVisibilityLease:IDisposable
    {
        sealed class State
        {
            readonly Element element;
            readonly UruiDisplayState display=new UruiDisplayState();
            BindableProperty<bool> property;
            int references;
            internal State(Element element){this.element=element;}
            internal BindableProperty<bool> Acquire()
            {
                if(references++==0)
                {
                    var visible=!element.HasAttribute("hidden")&&element.Style.Display!=DisplayStyle.None;
                    property=new BindableProperty<bool>(visible,(_,value)=>Apply(value));
                    // 初值false不会触发BindableProperty事件，必须在首个租约建立时落实隐藏。
                    if(!visible){Apply(false);}
                }
                return property;
            }
            void Apply(bool value)
            {
                display.Apply(element,value);
                if(!value){element.Document.Surface.Input.CancelPointers(element);}
            }
            internal void Release()
            {
                if(--references==0){property.Dispose();property=null;}
            }
        }
        static readonly ConditionalWeakTable<Element,State> States=new ConditionalWeakTable<Element,State>();
        State owner;
        internal BindableProperty<bool> Property{get;}
        internal UruiVisibilityLease(Element element)
        {
            if(!element.IsValid){throw new ArgumentException("显隐适配需要有效节点。",nameof(element));}
            owner=States.GetValue(element,key=>new State(key));Property=owner.Acquire();
        }
        public void Dispose(){var previous=owner;owner=null;previous?.Release();}
    }
    /// <summary>只覆盖隐藏状态，恢复显示时保留作者的行内布局或样式表级联。</summary>
    internal sealed class UruiDisplayState
    {
        string previousDisplay;

        internal void Apply(Element element, bool visible)
        {
            // HTML hidden supplies an authored initial state before the first style cascade.
            if (visible && element.HasAttribute("hidden")) { element.RemoveAttribute("hidden"); }
            var current = element.Style.Get("display");
            if (!visible)
            {
                if (current != "none") { previousDisplay = current; }
                element.Style.Set("display", "none");
            }
            else if (current == "none")
            {
                if (previousDisplay == null) { element.Style.Remove("display"); }
                else { element.Style.Set("display", previousDisplay); }
            }
        }
    }
}
