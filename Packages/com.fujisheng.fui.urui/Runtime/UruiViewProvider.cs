using System;
using System.Threading;
using System.Threading.Tasks;
using FUI.Navigation;
using FUI.Resources;
using FUI.Rendering;
using global::URUI.Animation;
using global::URUI.Resources;

namespace FUI.Rendering.URUI
{
    /// <summary>从显式页面目录和宿主资源构造视图，不参与业务绑定或 Presenter 生命周期。</summary>
    public sealed class UruiViewProvider : IViewProvider
    {
        readonly UruiStage stage;
        readonly UruiViewCatalog catalog;
        readonly IUruiViewAssets assets;
        readonly IUiClock clock;
        public UruiViewProvider(UruiStage stage, UruiViewCatalog catalog, IUruiViewAssets assets, IUiClock clock)
        {
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            stage.UiClock=clock;
        }
        public IViewLease Create(RouteDescriptor route, IPreloadLease preload = null)
        {
            if (preload != null && preload is not UruiPreloadLease)
            { throw new ArgumentException("预载租约不是 URUI 页面资源。", nameof(preload)); }
            var prepared = preload as UruiPreloadLease;
            var definition = catalog.Get(route);
            if (prepared != null && (!prepared.Route.Equals(route) || !ReferenceEquals(prepared.Definition, definition)))
            { throw new InvalidOperationException("预载租约与页面定义不匹配：" + route); }
            var document = prepared?.Document ?? RequireCompiled(definition.HtmlUri);
            var scope = stage.OpenPage(document, definition, assets, route.Settings);
            try
            {
                var view = definition.Compose(scope, assets, clock);
                view.Layer = (int)route.Settings.Layer;
                return new UruiViewLease(view, scope);
            }
            catch { stage.ClosePage(scope); throw; }
        }
        public ValueTask<IViewLease> CreateAsync(RouteDescriptor route, IPreloadLease preload, CancellationToken token)
        {
            IViewLease CreateChecked()
            {
                token.ThrowIfCancellationRequested();
                var lease = Create(route, preload);
                if (token.IsCancellationRequested) { lease.Dispose(); token.ThrowIfCancellationRequested(); }
                return lease;
            }
            if (!stage.Surface.IsExecutingCallback) return new ValueTask<IViewLease>(CreateChecked());
            var completion = new TaskCompletionSource<IViewLease>(TaskCreationOptions.RunContinuationsAsynchronously);
            stage.Surface.RunAfterEvent(() => {
                try { completion.TrySetResult(CreateChecked()); }
                catch (OperationCanceledException) { completion.TrySetCanceled(token); }
                catch (Exception error) { completion.TrySetException(error); }
            });
            return new ValueTask<IViewLease>(completion.Task);
        }

        public ValueTask<IPreloadLease> PreloadAsync(RouteDescriptor route, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var definition = catalog.Get(route);
            return new ValueTask<IPreloadLease>(new UruiPreloadLease(route, definition, RequireCompiled(definition.HtmlUri)));
        }
        CompiledDocumentAsset RequireCompiled(string uri)
        {
            var compiled = assets.LoadCompiled(uri);
            if (!compiled) { throw new InvalidOperationException("缺少 URUI 编译页面资源：" + uri); }
            return compiled;
        }
    }
    public sealed class UruiPreloadLease : IPreloadLease
    {
        public RouteDescriptor Route { get; }
        public UruiViewDefinition Definition { get; }
        public CompiledDocumentAsset Document { get; }
        public UruiPreloadLease(RouteDescriptor route, UruiViewDefinition definition, CompiledDocumentAsset document)
        { Route = route ?? throw new ArgumentNullException(nameof(route)); Definition = definition ?? throw new ArgumentNullException(nameof(definition)); Document = document ? document : throw new ArgumentNullException(nameof(document)); }
        public void Dispose() { }
    }

    public sealed class UruiViewLease : IViewLease
    {
        static readonly IAssetScope EmptyAssets = new EmptyAssetScope();
        readonly UruiScope scope;

        public UruiViewLease(IView view, UruiScope scope)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            this.scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public IView View { get; }
        public IAssetScope Assets => EmptyAssets;

        public void Dispose()
        {
            View.Destroy();
        }

        sealed class EmptyAssetScope : IAssetScope
        {
            public IAssetLease<T> Load<T>(string key) => throw new NotSupportedException("URUI view assets are loaded by the provider.");
            public ValueTask<IAssetLease<T>> LoadAsync<T>(string key, CancellationToken token) => throw new NotSupportedException("URUI view assets are loaded by the provider.");
            public IAssetLease<T> Instantiate<T>(string key) => throw new NotSupportedException("URUI view assets are loaded by the provider.");
            public ValueTask<IAssetLease<T>> InstantiateAsync<T>(string key, CancellationToken token) => throw new NotSupportedException("URUI view assets are loaded by the provider.");
            public void Dispose() { }
        }
    }
}
