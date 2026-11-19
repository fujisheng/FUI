using System;
using FUI.Binding;
using FUI.Infrastructure;
using URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 为作者已声明的子树绑定独立模型；不创建节点、模板或业务 Presenter。
    /// </summary>
    public sealed class ModelElement : ElementAdapter, IModelElement
    {
        readonly Func<ViewScope, View> factory;
        View view;
        ViewInstance instance;
        bool disposed;
        int version;
        static Element Validate(Element element, Func<ViewScope, View> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            return element;
        }
        public ModelElement(Element element, string name, Func<ViewScope, View> factory) : base(Validate(element, factory), name)
        {
            this.factory = factory;
            ViewModel = new BindableProperty<ObservableObject>(null, (_, value) => Bind(value));
        }
        public BindableProperty<ObservableObject> ViewModel
        {
            get;
        }
        void Check(int current, Element host)
        {
            if (disposed || !ReferenceEquals(Element, host) || !host.IsValid)
            {
                throw new ObjectDisposedException(nameof(ModelElement));
            }
            if (current != version)
            {
                throw new OperationCanceledException("子模型绑定已被新的输入替代。");
            }
        }
        void Bind(ObservableObject model)
        {
            var host = Element;
            var current = ++version;
            Check(current, host);
            if (model == null)
            {
                instance?.Disable();
                return;
            }
            if (instance != null && instance.ViewModel.GetType() == model.GetType())
            {
                var existing = instance;
                existing.UpdateViewModel(model);
                Check(current, host);
                existing.Enable();
                Check(current, host);
                return;
            }
            var previous = instance;
            instance = null;
            previous?.DestroyProjection();
            Check(current, host);
            View candidate = view;
            ViewInstance projection = null;
            var ownsCandidate = false;
            try
            {
                if (candidate == null)
                {
                    candidate = factory(new ViewScope(host)) ?? throw new InvalidOperationException("子视图工厂未返回视图。");
                    ownsCandidate = true;
                    // 即使工厂重入释放了owner，候选仍只拥有控制器，不能销毁作者节点。
                    candidate.RetainRootForOwner(host, preserveOwnerPresentation: true);
                    Check(current, host);
                    candidate.Initialize();
                    Check(current, host);
                }
                projection = ViewInstance.Create(candidate, model);
                Check(current, host);
                projection.Enable();
                Check(current, host);
                view = candidate;
                instance = projection;
                projection = null;
                ownsCandidate = false;
            }
            catch (Exception failure)
            {
                Exception cleanup = null;
                try
                {
                    projection?.DestroyProjection();
                }
                catch (Exception error)
                {
                    cleanup = error;
                }
                try
                {
                    if (ownsCandidate)
                    {
                        candidate?.Destroy();
                    }
                }
                catch (Exception error)
                {
                    cleanup = cleanup == null ? error : new AggregateException(cleanup, error);
                }
                if (cleanup != null)
                {
                    throw new AggregateException(failure, cleanup);
                }
                throw;
            }
        }
        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            version++;
            var projection = instance;
            var owned = view;
            instance = null;
            view = null;
            try
            {
                projection?.DestroyProjection();
            }
            finally
            {
                try
                {
                    owned?.Destroy();
                }
                finally
                {
                    ViewModel.Dispose();
                    base.Dispose();
                }
            }
        }
    }
}
