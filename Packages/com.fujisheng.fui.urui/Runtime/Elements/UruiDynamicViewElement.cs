using System;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;
using UnityEngine;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// DynamicView 的 HTML 版本：应用提供 Source 的资源解析和作者 HTML fragment，
    /// 在元素内部建立独立 Scope，并用 ViewInstance 绑定子 ViewModel。
    /// </summary>
    public class UruiDynamicViewElement : UruiElement, IDynamicViewElement, IModelElement
    {
        UruiView nestedView;
        UruiView bindingView;
        string nestedSource;
        global::URUI.Resources.CompiledTemplate nestedTemplate;
        ViewInstance instance;
        bool disposed;
        int version;
        readonly Func<string, string> templateUri;
        readonly Func<string, UruiScope, UruiView> viewFactory;

        /// <summary>资源解析和子视图工厂由应用明确提供，不推断资源目录或私有标记。</summary>
        public UruiDynamicViewElement(global::URUI.Elements.Element element,string name,
            Func<string,string> templateUri,Func<string,UruiScope,UruiView> viewFactory):base(element,name)
        {
            this.templateUri=templateUri??throw new ArgumentNullException(nameof(templateUri));
            this.viewFactory=viewFactory??throw new ArgumentNullException(nameof(viewFactory));
            Source=new BindableProperty<string>(string.Empty,OnSourceChanged);
            ViewModel=new BindableProperty<ObservableObject>(null,OnViewModelChanged);
        }

        public BindableProperty<string> Source { get; }
        public BindableProperty<ObservableObject> ViewModel { get; }

        /// <summary>允许应用按模型类型配置模板；默认原样使用 Source。</summary>
        protected virtual string ResolveTemplate(string source) =>
            source;

        void Check(int current,global::URUI.Elements.Element host)
        {
            if(disposed||!ReferenceEquals(Element,host)||!host.IsValid){throw new ObjectDisposedException(nameof(UruiDynamicViewElement));}
            if(current!=version){throw new OperationCanceledException("动态子视图已被新的输入替代。");}
        }

        static readonly Unity.Profiling.ProfilerMarker sourceMarker = new("FUI.URUI.DynamicView.Source");
        void OnSourceChanged(string oldValue, string newValue)
        {
            using var sample = sourceMarker.Auto();
            var host=Element;var current=++version;Check(current,host);
            var source = ResolveTemplate(newValue);
            Check(current,host);
            if (string.IsNullOrEmpty(source))
            {
                ReleaseNested();
                return;
            }

            var template = LoadTemplate(source);
            Check(current,host);
            if (template == null)
            {
                Debug.LogWarning($"URUI DynamicView '{Name}': template not found for '{source}'.");
                return;
            }

            var container = UruiStage.CreateHost(host.Document, "dynamic-host");
            host.Insert(host.ChildCount, container);
            var candidateScope = new UruiScope(container);
            UruiView candidateView = null;
            ViewInstance candidateInstance = null;
            try
            {
                using (host.Document.BeginUpdate())
                {
                    container.SetInnerHtml(template);
                    candidateView=viewFactory(source,candidateScope)??throw new InvalidOperationException("子视图工厂未返回视图："+source);
                    Check(current,host);
                    candidateView.PrepareProjection();
                    Check(current,host);
                    if (ViewModel.Value != null)
                    {
                        candidateInstance = ViewInstance.Create(candidateView, ViewModel.Value);
                        Check(current,host);
                        candidateInstance?.Enable();
                        Check(current,host);
                    }
                }
                Check(current,host);
                ReleaseNested();Check(current,host);
            }
            catch (Exception failure)
            {
                try
                {
                    if (candidateView != null) { DestroyProjection(candidateInstance, candidateView); }
                    else if (container.IsValid) { container.Destroy(); }
                }
                catch (Exception cleanupFailure) { throw new AggregateException(failure, cleanupFailure); }
                throw;
            }
            nestedView = candidateView;
            instance = candidateInstance;
            nestedSource = source;
            nestedTemplate = template;
        }

        void OnViewModelChanged(ObservableObject oldValue, ObservableObject newValue)
        {
            var host=Element;var current=++version;Check(current,host);
            if (newValue == null)
            {
                instance?.Disable();
                return;
            }

            if (instance == null)
            {
                if (nestedView != null && bindingView == null)
                {
                    var source = ResolveTemplate(Source.Value);
                    Check(current, host);
                    if (source == nestedSource && ReferenceEquals(LoadTemplate(source), nestedTemplate))
                    {
                        Check(current, host);
                        BindPreparedView(current, host, newValue);
                        return;
                    }
                    Check(current, host);
                }
                OnSourceChanged(Source.Value, Source.Value);
                return;
            }

            if (instance.ViewModel.GetType() == newValue.GetType())
            {
                var existing=instance;
                existing.UpdateViewModel(newValue);Check(current,host);
                existing.Enable();Check(current,host);
            }
            else
            {
                OnSourceChanged(Source.Value, Source.Value);
            }
        }

        global::URUI.Resources.CompiledTemplate LoadTemplate(string source) => Element.Document.GetCompiledTemplate(templateUri(source));

        void BindPreparedView(int current, global::URUI.Elements.Element host, ObservableObject model)
        {
            var candidateView = nestedView;
            var previousVisible = candidateView.Visible;
            ViewInstance candidateInstance = null;
            // 回调可替换 Source 或释放 owner；正在绑定的旧 View 由本事务延后回收。
            bindingView = candidateView;
            try
            {
                using (host.Document.BeginUpdate())
                {
                    candidateInstance = ViewInstance.Create(candidateView, model);
                    Check(current, host);
                    candidateInstance?.Enable();
                    Check(current, host);
                }
                Check(current, host);
                instance = candidateInstance;
            }
            catch (Exception failure)
            {
                Exception cleanupFailure = null;
                try { candidateInstance?.DestroyProjection(); }
                catch (Exception exception) { cleanupFailure = exception; }
                if (ReferenceEquals(nestedView, candidateView) && !disposed && host.IsValid)
                {
                    try { candidateView.Visible = previousVisible; }
                    catch (Exception exception) { cleanupFailure ??= exception; }
                }
                // 清理回调也可能重入，必须在其返回后重新核对 View 的所有权。
                if (!ReferenceEquals(nestedView, candidateView) || disposed || !host.IsValid)
                {
                    try { candidateView.Destroy(); }
                    catch (Exception exception) { cleanupFailure ??= exception; }
                }
                if (cleanupFailure != null) throw new AggregateException(failure, cleanupFailure);
                throw;
            }
            finally { bindingView = null; }
        }

        void ReleaseNested()
        {
            var oldInstance = instance;
            var oldView = nestedView;
            instance = null;
            nestedView = null;
            nestedSource = null;
            nestedTemplate = null;
            DestroyProjection(oldInstance, ReferenceEquals(oldView, bindingView) ? null : oldView);
        }

        static void DestroyProjection(ViewInstance projection, UruiView view)
        {
            Exception failure = null;
            try { projection?.DestroyProjection(); }
            catch (Exception exception) { failure = exception; }
            try { view?.Destroy(); }
            catch (Exception exception) { failure ??= exception; }
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        public override void Dispose()
        {
            if(disposed){return;}disposed=true;version++;
            try { ReleaseNested(); }
            finally
            {
                Source?.Dispose();
                ViewModel?.Dispose();
                base.Dispose();
            }
        }
    }
}
