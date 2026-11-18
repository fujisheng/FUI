using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Presentation;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>通过真实Core模板/生成绑定创建受控子投影，拥有其View及释放回调。</summary>
    public sealed class HeadlessPrototypeElement : HeadlessElement,IProjectionTemplateElement
    {
        readonly Func<ObservableObject,HeadlessView> createView;
        readonly Action<HeadlessView> releaseView;
        readonly Dictionary<ViewInstance,ChildView> owned=new Dictionary<ViewInstance,ChildView>();
        public HeadlessPrototypeElement(string name,Func<ObservableObject,HeadlessView> createView,Action<HeadlessView> releaseView=null,ProjectionLayout layout=default) : base(name)
        {
            this.createView=createView??throw new ArgumentNullException(nameof(createView));this.releaseView=releaseView;
            Template=new BindableProperty<ProjectionTemplate>(new ProjectionTemplate(CreateProjection,layout,null));
        }
        public BindableProperty<ProjectionTemplate> Template {get;}
        ProjectionCreation CreateProjection(ObservableObject vm)
        {
            if(IsDisposed) {throw new ObjectDisposedException(Name);}
            var view=createView(vm)??throw new InvalidOperationException("Projection factory returned a null view.");
            if(view.Destroyed||view.Initialized||view.HasRowOwner||ReferenceEquals(view,Owner)) {throw new InvalidOperationException("Projection factory must create an independent live view.");}
            view.HasRowOwner=true;ViewInstance instance=null;
            try
            {
                if(IsDisposed) {throw new ObjectDisposedException(Name);}
                var child=new ChildView(view,this);
                instance=ViewInstance.Create(child,vm);
                if(IsDisposed) {throw new ObjectDisposedException(Name);}
                owned.Add(instance,child);
                return new ProjectionCreation(instance,ReleaseProjection);
            }
            catch(Exception failure)
            {
                var cleanup=HeadlessCleanup.Collect(new Action[] {()=>instance?.DestroyProjection(),view.Destroy,()=>releaseView?.Invoke(view)});
                if(cleanup!=null) {throw new AggregateException("Child template projection creation and cleanup failed.",failure,cleanup);}throw;
            }
        }
        void ReleaseProjection(ViewInstance instance)
        {
            if(!owned.TryGetValue(instance,out var child)) {return;}
            owned.Remove(instance);var view=child.View;
            var failure=HeadlessCleanup.Collect(new Action[] {instance.DestroyProjection,view.Destroy,()=>releaseView?.Invoke(view)});
            if(failure!=null) {System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        }
        internal override void SetInteractable(bool value) {ApplyParentState();}
        internal override void SetVisibility(bool value) {ApplyParentState();}
        void ApplyParentState()
        {foreach(var child in new List<ChildView>(owned.Values)) {child.ApplyParentState();}}

        // Core激活设置的是子View自身请求；实际可见与交互仍受父层级约束。
        internal sealed class ChildView : IView
        {
            readonly HeadlessElement parent;
            bool requestedVisible,requestedInteractable=true;
            internal ChildView(HeadlessView view,HeadlessElement parent)
            {View=view;this.parent=parent;ApplyParentState();}
            internal HeadlessView View {get;}
            public string Name=>View.Name;
            public int Layer {get=>View.Layer;set=>View.Layer=value;}
            public int Order {get=>View.Order;set=>View.Order=value;}
            public bool Visible {get=>View.Visible;set {requestedVisible=value;ApplyParentState();}}
            public bool Interactable {get=>View.Interactable;set {requestedInteractable=value;ApplyParentState();}}
            public IReadOnlyList<IElement> Elements=>View.Elements;
            public T GetElement<T>(string path) where T:IElement=>View.GetElement<T>(path);
            public IElement GetElement(string path,Type type)=>View.GetElement(path,type);
            public void Initialize()=>View.Initialize();
            public void Destroy()=>View.Destroy();
            internal void ApplyParentState()
            {
                View.Visible=requestedVisible&&(parent.Owner?.Visible??true)&&!parent.IsDisposed;
                View.Interactable=requestedInteractable&&(parent.Owner?.Interactable??true)&&!parent.IsDisposed;
            }
        }
        protected override void OnDispose()
        {
            Template.Dispose();var actions=new List<Action>();
            foreach(var instance in new List<ViewInstance>(owned.Keys)) {var current=instance;actions.Add(()=>ReleaseProjection(current));}
            var failure=HeadlessCleanup.Collect(actions);
            if(failure!=null) {System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        }
    }
}
