using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;
namespace FUI.Testing
{
    /// <summary>原始source工厂与真实Core子Projection；不加载资源或复制UI算法。</summary>
    public sealed class HeadlessDynamicProjectionElement : HeadlessElement, IDynamicViewElement
    {
        sealed class Entry
        {
            internal HeadlessView Raw;
            internal HeadlessPrototypeElement.ChildView View;
            internal ViewInstance Projection;
            internal bool Applying,Released;
            internal long SourceVersion;
        }
        readonly Func<string,HeadlessView> createView;
        readonly Action<HeadlessView> releaseView;
        Entry current;
        long version,modelVersion;
        bool restoringSource;
        public HeadlessDynamicProjectionElement(string name,Func<string,HeadlessView> createView,Action<HeadlessView> releaseView=null) : base(name)
        {
            this.createView=createView??throw new ArgumentNullException(nameof(createView));this.releaseView=releaseView;
            Source=new BindableProperty<string>("",ChangeSource);
            ViewModel=new BindableProperty<ObservableObject>(null,ChangeViewModel);
        }
        public BindableProperty<string> Source {get;}
        public BindableProperty<ObservableObject> ViewModel {get;}
        public HeadlessView CurrentView=>current?.Raw;
        void ChangeSource(string oldValue,string value)
        {
            if(restoringSource){return;}
            var serial=++version;
            if(string.IsNullOrEmpty(value)) {var previous=current;current=null;Release(previous);return;}
            Entry candidate=null;
            try
            {
                if(IsDisposed) {throw new ObjectDisposedException(Name);}
                var view=createView(value)??throw new InvalidOperationException("Dynamic factory returned a null view.");
                if(view.Destroyed||view.Initialized||view.HasRowOwner||ReferenceEquals(view,Owner))
                {throw new InvalidOperationException("Dynamic factory must create an independent live view.");}
                view.HasRowOwner=true;candidate=new Entry{Raw=view,View=new HeadlessPrototypeElement.ChildView(view,this),SourceVersion=serial};
                CheckCurrentRequest(serial);
                SynchronizeViewModel(candidate);
                CheckCurrentRequest(serial);
            }
            catch(Exception failure)
            {
                var cleanup=HeadlessCleanup.Collect(new Action[]{()=>Release(candidate),()=>{if(!IsDisposed&&serial==version){restoringSource=true;try{Source.SetValue(oldValue);}finally{restoringSource=false;}}}});
                if(cleanup!=null){throw new AggregateException("Dynamic child projection creation and cleanup failed.",failure,cleanup);}throw;
            }
            var previousEntry=current;current=candidate;
            // 候选已成功后交接；先摘除旧所有权，清理异常不丢失新/旧资源。
            Release(previousEntry);
        }
        void CheckCurrentRequest(long serial)
        {if(IsDisposed){throw new ObjectDisposedException(Name);}if(serial!=version){throw new OperationCanceledException("Dynamic source changed during creation.");}}
        void ChangeViewModel(ObservableObject previous,ObservableObject value)
        {modelVersion++;if(current!=null){SynchronizeViewModel(current);}}
        void SynchronizeViewModel(Entry entry)
        {
            if(entry.Applying||entry.Released){return;}
            entry.Applying=true;
            try
            {
                long revision;
                do
                {
                    revision=modelVersion;
                    ApplyViewModel(entry,ViewModel.Value);
                    if(entry.Released||IsDisposed){return;}
                }while(revision!=modelVersion);
            }
            finally{entry.Applying=false;}
        }
        void ApplyViewModel(Entry entry,ObservableObject vm)
        {
            if(vm==null)
            {
                entry.Projection?.Disable();entry.View.Visible=true;return;
            }
            if(entry.Projection!=null&&entry.Projection.ViewModel.GetType()!=vm.GetType())
            {var old=entry.Projection;entry.Projection=null;old.DestroyProjection();CheckEntry(entry);}
            if(entry.Projection==null)
            {
                var candidate=ViewInstance.Create(entry.View,vm);
                try{CheckEntry(entry);}
                catch(Exception cancelled)
                {
                    try{candidate.DestroyProjection();}catch(Exception cleanup){throw new AggregateException(cancelled,cleanup);}throw;
                }
                entry.Projection=candidate;
            }
            else {entry.Projection.UpdateViewModel(vm);CheckEntry(entry);}
            entry.Projection.Enable();
        }
        void CheckEntry(Entry entry)
        {
            if(IsDisposed){throw new ObjectDisposedException(Name);}
            if(entry.Released||entry.Raw.Destroyed||(!ReferenceEquals(entry,current)&&entry.SourceVersion!=version))
            {throw new OperationCanceledException("Dynamic entry was released during projection operation.");}
        }
        void Release(Entry entry)
        {
            if(entry==null||entry.Released){return;}
            entry.Released=true;
            var projection=entry.Projection;entry.Projection=null;
            var failure=HeadlessCleanup.Collect(new Action[]{()=>projection?.DestroyProjection(),entry.Raw.Destroy,()=>releaseView?.Invoke(entry.Raw)});
            if(failure!=null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        }
        internal override void SetVisibility(bool value){current?.View.ApplyParentState();}
        internal override void SetInteractable(bool value){current?.View.ApplyParentState();}
        protected override void OnDispose()
        {
            version++;var previous=current;current=null;
            var failure=HeadlessCleanup.Collect(new Action[]{Source.Dispose,ViewModel.Dispose,()=>Release(previous)});
            if(failure!=null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        }
    }
}
