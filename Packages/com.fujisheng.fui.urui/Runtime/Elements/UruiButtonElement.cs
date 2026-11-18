using System;
using FUI.Binding;
using FUI.Rendering;

namespace FUI.Rendering.URUI
{
    /// <summary>明确的按钮能力，桥接原生指针及键盘激活，隔离嵌套按钮的事件冒泡。</summary>
    public class UruiButtonElement : UruiElement, IButtonElement, IClickElement
    {
        readonly IDisposable subscription;
        public Command Click{get;}
        static global::URUI.Elements.Element RequireButton(global::URUI.Elements.Element element)
        {
            if(element is not global::URUI.Controls.Button){throw new ArgumentException("按钮适配需要原生 button 节点。",nameof(element));}
            return element;
        }
        public UruiButtonElement(global::URUI.Elements.Element element,string name):base(RequireButton(element),name)
        {
            Click=new Command();
            try
            {
                subscription=element.RegisterCallback<global::URUI.Events.ClickEvent>(evt=>
                {
                    if(!IsOwnClick(evt)){return;}
                    element.Document.RunAfterEvent(()=>
                    {
                        if(Element!=null&&Element.IsValid&&Element.EnabledInHierarchy){Click.Invoke();}
                    });
                });
            }
            catch{Click.Dispose();base.Dispose();throw;}
        }
        /// <summary>只接受实际被激活的按钮，项目音效与命令使用相同规则。</summary>
        protected bool IsOwnClick(global::URUI.Events.ClickEvent evt)
        {
            for(var target=evt.Target;target!=null;target=target.Parent)
            {if(target is global::URUI.Controls.Button){return ReferenceEquals(target,Element);}}
            return false;
        }
        public override void Dispose(){subscription?.Dispose();Click.Dispose();base.Dispose();}
    }
}
