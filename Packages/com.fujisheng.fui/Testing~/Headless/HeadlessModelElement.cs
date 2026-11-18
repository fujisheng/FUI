using System;
using FUI.Binding;
using FUI.Presentation;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>创建真实生成子Projection；只拥有子视图与投影，不拥有传入VM。</summary>
    public sealed class HeadlessModelElement : HeadlessElement,IModelElement,IVisibilityElement
    {
        readonly Func<ObservableObject,HeadlessView> createView;
        ProjectionHandle<ObservableObject> projection;
        bool ownerVisible=true;
        public HeadlessModelElement(string name,Func<ObservableObject,HeadlessView> createView) : base(name)
        { this.createView=createView ?? throw new ArgumentNullException(nameof(createView));ViewModel.OnValueChanged+=Rebind;Visible.OnValueChanged+=(previous,value)=>ApplyVisibility(); }
        public BindableProperty<ObservableObject> ViewModel { get; } = new BindableProperty<ObservableObject>();
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public HeadlessView ChildView { get; private set; }
        void Rebind(ObservableObject oldValue,ObservableObject newValue)
        {
            projection?.Dispose();projection=null;ChildView=null;
            if (newValue==null) { return; }
            var view=createView(newValue);ChildView=view;
            try { projection=Projection.Create(view,newValue);if (ownerVisible && Visible.Value) { projection.Activate(); }view.Interactable=Owner?.Interactable ?? true; }
            catch (Exception failure)
            {
                var cleanup=HeadlessCleanup.Collect(new Action[] { () => projection?.Dispose(),view.Destroy });
                projection=null;ChildView=null;
                if (cleanup!=null) { throw new AggregateException("Child projection opening and cleanup failed.",failure,cleanup); }
                throw;
            }
        }
        internal override void SetVisibility(bool value)
        { ownerVisible=value;ApplyVisibility(); }
        void ApplyVisibility()
        { if (ownerVisible && Visible.Value) { projection?.Activate(); }else { projection?.Deactivate(); } }
        internal override void SetInteractable(bool value) { if (ChildView!=null) { ChildView.Interactable=value; } }
        protected override void OnDispose() { Visible.Dispose();ViewModel.Dispose();projection?.Dispose();projection=null;ChildView=null; }
    }
}
