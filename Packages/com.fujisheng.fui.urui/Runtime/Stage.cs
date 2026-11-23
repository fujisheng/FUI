using System;
using System.Collections.Generic;
using UnityEngine;
using global::URUI.Animation;
using global::URUI.Documents;
using global::URUI.Elements;
using global::URUI.Hosting;
using global::URUI.Resources;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 调度独立 HTML 页面；文档共享 Surface，页内子视图保留所属文档。
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "FUI.Rendering.URUI", sourceClassName: "UruiStage")]
    public sealed class Stage : MonoBehaviour
    {
        static readonly List<Stage> activeStages = new();
        readonly HashSet<ViewScope> pages = new();
        Surface surface;
        IUIClock uiClock = UnityUIClock.Unscaled;
        int pageOrder;
        [SerializeField]
        ImageAsset[] images = Array.Empty<ImageAsset>();
        [SerializeField]
        TextResource[] textResources = Array.Empty<TextResource>();
        public static Stage Current => activeStages.Count > 0 ? activeStages[activeStages.Count - 1] : null;
        public static IReadOnlyList<Stage> ActiveStages => activeStages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActiveStages()
        {
            activeStages.Clear();
        }
        public Surface Surface
        {
            get
            {
                EnsureSurface();
                return surface;
            }
        }
        public int PageCount => pages.Count;
        /// <summary>
        /// 当前独立页面的只读快照，不表示绘制顺序；关闭仍通过 Scope 或 ClosePage。
        /// </summary>
        public IReadOnlyCollection<ViewScope> Pages => new List<ViewScope>(pages).AsReadOnly();
        /// <summary>
        /// 所有页面共享的时钟；有活动文档时沿用 Surface 的更换约束。
        /// </summary>
        public IUIClock UIClock
        {
            get => uiClock;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (ReferenceEquals(value, uiClock))
                {
                    return;
                }
                if (surface)
                {
                    surface.UIClock = value;
                }
                uiClock = value;
            }
        }
        /// <summary>
        /// 加载前登记每个文档的游戏效果；返回的租约在页面关闭或候选失败时释放。
        /// </summary>
        public Func<Document, IDisposable> ConfigureDocument
        {
            get;
            set;
        }
        public Func<string, ImageLease> ImageResolver
        {
            get;
            set;
        }
        /// <summary>
        /// 后续页面使用的图片映射；已加载页面持有自己的资源代际。
        /// </summary>
        public ImageAsset[] Images
        {
            get => (ImageAsset[])images.Clone();
            set => images = value == null ? Array.Empty<ImageAsset>() : (ImageAsset[])value.Clone();
        }
        /// <summary>
        /// 规范 URL 资源清单；HTML head 和 CSS url() 决定实际依赖。
        /// </summary>
        public TextResource[] TextResources
        {
            get => (TextResource[])textResources.Clone();
            set => textResources = value == null ? Array.Empty<TextResource>() : (TextResource[])value.Clone();
        }
        void Awake()
        {
            if (!activeStages.Contains(this))
            {
                activeStages.Add(this);
            }
            EnsureSurface();
        }
        void EnsureSurface()
        {
            if (surface)
            {
                return;
            }
            surface = GetComponent<Surface>();
            if (!surface)
            {
                surface = gameObject.AddComponent<Surface>();
            }
            surface.UIClock = uiClock;
        }
        /// <summary>
        /// 创建完整页面；普通页面默认非模态，Popup 默认阻断较低页面。
        /// </summary>
        public ViewScope OpenPage(string html, string sourceUrl, FUI.Navigation.Layer layer)
            => OpenDocument(html, sourceUrl, layer, layer == FUI.Navigation.Layer.Popup, null, null, null);

        /// <summary>
        /// 从 Editor 编译资产创建独立页面。
        /// </summary>
        public ViewScope OpenPage(CompiledDocumentAsset compiled, FUI.Navigation.Layer layer)
            => OpenDocument(null, compiled ? compiled.SourceUrl : throw new ArgumentNullException(nameof(compiled)),
                layer, layer == FUI.Navigation.Layer.Popup, null, null, null, compiled);

        internal ViewScope OpenPage(CompiledDocumentAsset compiled, ViewDefinition definition, IViewAssets assets, FUI.Navigation.RoutePolicy policy)
        {
            var pageImages = new List<ImageAsset>();
            foreach (var groupKey in definition.ImageGroupKeys)
            {
                var group = assets.LoadImageGroup(groupKey) ?? throw new InvalidOperationException("缺少图片组：" + groupKey);
                pageImages.AddRange(group);
            }
            // 清单由作者依赖闭包生成，运行时不另写 HTML/CSS 解析器。
            return OpenDocument(null, definition.HtmlUri, policy.Layer, false,
                assets.LoadDependencies(definition.HtmlUri), pageImages, assets.AcquireImage, compiled, visible: false);
        }
        ViewScope OpenDocument(string html, string sourceUrl, FUI.Navigation.Layer layer, bool modal,
            IReadOnlyList<TextResource> dependencies, IReadOnlyList<ImageAsset> pageImages, Func<string, ImageLease> resolver, CompiledDocumentAsset compiled = null, bool visible = true)
        {
            EnsureSurface();
            if (surface.IsExecutingCallback)
            {
                throw new InvalidOperationException("同步页面创建必须在回调外执行，请使用 Provider.CreateAsync 或 Surface.RunAfterEvent。");
            }
            if (pageOrder == int.MaxValue)
            {
                throw new InvalidOperationException("URUI 页面顺序已耗尽。");
            }
            var node = new GameObject("URUI Page: " + sourceUrl);
            node.SetActive(false);
            node.transform.SetParent(transform, false);
            var document = node.AddComponent<Document>();
            ViewScope scope = null;
            IDisposable configuration = null;
            var order = ++pageOrder;
            try
            {
                document.Surface = surface;
                document.SourceUrl = sourceUrl;
                var files = new List<TextResource>(textResources);
                if (dependencies != null)
                {
                    files.AddRange(dependencies);
                }
                var textures = new List<ImageAsset>(images);
                if (pageImages != null)
                {
                    textures.AddRange(pageImages);
                }
                document.TextResources = files.ToArray();
                document.Images = textures.ToArray();
                document.ImageResolver = resolver ?? ImageResolver;
                document.SetPresentation((int)layer, order, visible, modal);
                configuration = ConfigureDocument?.Invoke(document);
                node.SetActive(true);
                if (compiled)
                {
                    document.Load(compiled);
                }
                else
                {
                    document.LoadHtml(html, sourceUrl);
                }
                scope = new ViewScope(document, (int)layer, order, modal, visible, () => ReleasePage(scope, document, configuration));
                pages.Add(scope);
                return scope;
            }
            catch (Exception creationFailure)
            {
                Exception cleanupFailure = null;
                try
                {
                    if (document)
                    {
                        document.Unload();
                    }
                }
                catch (Exception error)
                {
                    cleanupFailure = error;
                }
                try
                {
                    configuration?.Dispose();
                }
                catch (Exception error)
                {
                    cleanupFailure = cleanupFailure == null ? error : new AggregateException(cleanupFailure, error);
                }
                if (node)
                {
                    Destroy(node);
                }
                if (cleanupFailure != null)
                {
                    throw new AggregateException(creationFailure, cleanupFailure);
                }
                throw;
            }
        }
        void ReleasePage(ViewScope scope, Document document, IDisposable configuration)
        {
            // 卸载、配置退役和节点销毁必须处于同一段队列操作，避免原生回调仍在使用配置资源。
            surface.RunAfterEvent(() =>
            {
                if (document)
                {
                    document.Unload();
                }
                try
                {
                    configuration?.Dispose();
                }
                finally
                {
                    pages.Remove(scope);
                    if (document)
                    {
                        Destroy(document.gameObject);
                    }
                }
            });
        }
        public void ClosePage(ViewScope scope) => scope?.Destroy();
        /// <summary>
        /// 为页内的动态片段建立普通 DOM 容器，不依赖全局模板库。
        /// </summary>
        internal static Element CreateHost(Document document, string className)
        {
            var tag = className == "text-break" ? "br" : className == "text-run" ? "span" : "div";
            var element = document.CreateElement(tag);
            element.AddToClassList(className);
            return element;
        }
        void OnDestroy()
        {
            Exception failure = null;
            foreach (var page in new List<ViewScope>(pages))
            {
                try
                {
                    page.Destroy();
                }
                catch (Exception error)
                {
                    failure = failure == null ? error : new AggregateException(failure, error);
                }
            }
            activeStages.Remove(this);
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
