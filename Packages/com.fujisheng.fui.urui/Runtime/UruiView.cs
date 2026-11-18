using System;
using System.Collections.Generic;
using FUI.Rendering;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 一个 FUI 页面/弹窗的 URUI 视图。元素名在 Scope 内解析，不依赖全局 id。
    /// </summary>
    public sealed class UruiView : IView, IViewPreparation, IViewUpdate, IViewInputFocus, IViewContentLifecycle, IExternalValueSynchronizationHost, FUI.Transitions.IViewTransitionSource
    {
        readonly UruiScope scope;
        bool ownsRoot;
        bool ownsPresentation = true;
        readonly List<IElement> elements = new List<IElement>();
        readonly List<IDisposable> owned = new List<IDisposable>();
        IReadOnlyList<UruiElementBinding> declaredBindings = Array.Empty<UruiElementBinding>();
        readonly Dictionary<string, List<IElement>> index = new Dictionary<string, List<IElement>>();
        bool initialized;
        bool initializing;
        bool generationPrepared = true;
        bool configuringGeneration;
        bool destroyed;
        bool resumeInitialization;
        bool inputFocusRequested, pendingInputFocus;
        Element previousInputFocus;
        Action<UruiView> configureGeneration;
        public bool IsContentAvailable => initialized && !destroyed && scope.Document.IsLoaded;
        public event Action ContentUnavailable;
        public event Action ContentAvailable;

        /// <summary>只显示作者资源；需要绑定时必须显式传入映射。</summary>
        public UruiView(string name, UruiScope scope) : this(name, scope, Array.Empty<UruiElementBinding>()) { }

        /// <summary>使用独立绑定映射；HTML 无需 FUI 私有标签、属性或类型推断。</summary>
        public UruiView(string name, UruiScope scope, IReadOnlyList<UruiElementBinding> bindings) : this(name, scope, true)
        {
            declaredBindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public Element Root => scope.Root;
        /// <summary>作者节点保留给父控件；内联模型同时保留父控件的显隐和交互状态。</summary>
        internal void RetainRootForOwner(Element authoredRoot, bool preserveOwnerPresentation)
        {
            if(!ReferenceEquals(Root,authoredRoot)){throw new ArgumentException("条目视图必须使用传入的作者槽位。");}
            ownsRoot=false;
            ownsPresentation=!preserveOwnerPresentation;
        }
        public FUI.Transitions.ITransition EnterTransition { get; set; }
        public FUI.Transitions.ITransition ExitTransition { get; set; }

        /// <summary>接管当前文档世代的控制器；重建后由页面定义重新创建。</summary>
        public void Own(IDisposable controller)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(UruiView));
            owned.Add(controller ?? throw new ArgumentNullException(nameof(controller)));
        }

        internal UruiView(string name, UruiScope scope, bool ownsRoot)
        {
            Name = name;
            this.scope = scope ?? throw new ArgumentNullException(nameof(scope));
            this.ownsRoot = ownsRoot;
            if (scope.OwnsDocument)
            {
                scope.Document.Unloaded += SuspendDocument;
                scope.Document.Loaded += RestoreDocument;
            }
        }

        internal void ConfigureGeneration(Action<UruiView> configure)
        {
            configureGeneration = configure ?? throw new ArgumentNullException(nameof(configure));
            generationPrepared = false;
            PrepareGeneration();
        }

        void PrepareGeneration()
        {
            if (generationPrepared || configuringGeneration) return;
            configuringGeneration = true;
            try { configureGeneration(this); generationPrepared = true; }
            finally { configuringGeneration = false; }
        }

        internal void SetBindings(IReadOnlyList<UruiElementBinding> bindings)
            => declaredBindings = bindings ?? throw new ArgumentNullException(nameof(bindings));

        void SuspendDocument(global::URUI.Documents.Document document)
        {
            if (destroyed) return;
            pendingInputFocus |= inputFocusRequested;
            previousInputFocus = null;
            resumeInitialization |= initialized;
            Exception failure = null;
            try { ContentUnavailable?.Invoke(); }
            catch (Exception error) { failure = error; }
            initialized = false;
            failure = ReleaseGeneration(failure);
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        void RestoreDocument(global::URUI.Documents.Document document)
        {
            if (destroyed) return;
            var restoredRoot = scope.Root;
            // Loaded 通知尚处于文档事务中，布局与完整重绑定需等这次发布结束。
            document.Surface.RunAfterEvent(() => {
                if (destroyed || !document.IsLoaded || !ReferenceEquals(scope.Root, restoredRoot)) return;
                var notifyingBindings = false;
                try {
                    PrepareGeneration();
                    if (resumeInitialization) Initialize();
                    notifyingBindings = true;
                    ContentAvailable?.Invoke();
                    resumeInitialization = false;
                }
                catch (Exception error) {
                    // 先通知已恢复的绑定退出，再释放它们持有的本代适配器。
                    if (notifyingBindings) {
                        try { ContentUnavailable?.Invoke(); }
                        catch (Exception cleanupFailure) { error = new AggregateException(error, cleanupFailure); }
                    }
                    initialized = false;
                    var failure = ReleaseGeneration(error);
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                }
            });
        }

        public string Name { get; }
        int layer;
        public int Layer { get => layer; set { layer = value; scope.SetLayer(value); } }
        int order;
        public int Order
        {
            get => order;
            set
            {
                order = value;
                scope.SetOrder(value);
            }
        }
        bool visible = true;
        bool interactable = true;

        public bool Visible
        {
            get => visible;
            set
            {
                if (!value) RememberInputFocus();
                visible = value;
                if (initialized && ownsPresentation) scope.SetVisible(value);
                ApplyInputFocus();
            }
        }

        public bool Interactable
        {
            get => interactable;
            set
            {
                if (!value) RememberInputFocus();
                interactable = value;
                if (initialized && ownsPresentation) scope.SetInteractable(value);
                ApplyInputFocus();
            }
        }

        public void FocusInput()
        {
            if (destroyed || !scope.OwnsDocument) return;
            inputFocusRequested = pendingInputFocus = true;
            ApplyInputFocus();
        }

        void RememberInputFocus()
        {
            if (destroyed || !scope.OwnsDocument || !scope.Document.IsLoaded) return;
            var focused = scope.Document.FocusedElement;
            if (focused != null) previousInputFocus = focused;
        }

        void ApplyInputFocus()
        {
            if (!pendingInputFocus || !initialized || destroyed || !visible || !interactable || !scope.Document.IsLoaded) return;
            scope.Document.FlushPendingGeometry();
            if (!pendingInputFocus || !initialized || destroyed || !visible || !interactable || !scope.Document.IsLoaded) return;
            // 覆盖页面会禁用原节点并释放原生焦点；在解除交互门之后恢复原节点。
            if (previousInputFocus != null && previousInputFocus.IsValid && previousInputFocus.EnabledInHierarchy && previousInputFocus.Focus()
                || scope.Document.Focus()) pendingInputFocus = false;
        }
        public IReadOnlyList<IElement> Elements => elements;

        bool preparedForBinding;
        public void Initialize()
        {
            if (destroyed) throw new ObjectDisposedException(nameof(UruiView));
            if (initializing) return;
            if (initialized)
            {
                if (preparedForBinding)
                {
                    initializing = true;
                    try
                    {
                        scope.Document.SynchronizeGeometry(scope.Root);
                        preparedForBinding = false;
                    }
                    catch (Exception error)
                    {
                        initialized = false;
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ReleaseElements(error)).Throw();
                    }
                    finally { initializing = false; }
                }
                return;
            }
            preparedForBinding = false;
            InitializeProjection(scope.Document.SynchronizeGeometry);
        }

        void IViewPreparation.PrepareForBinding() => PrepareProjection();
        IDisposable IViewUpdate.BeginUpdate() => scope.Document.BeginUpdate();
        internal void PrepareProjection()
        {
            using var sample = initializeMarker.Auto();
            if (destroyed) throw new ObjectDisposedException(nameof(UruiView));
            if (initialized || initializing) return;
            preparedForBinding = true;
            InitializeProjection(scope.Document.RequestGeometry);
        }

        static readonly Unity.Profiling.ProfilerMarker initializeMarker = new("FUI.URUI.View.Initialize");
        static readonly Unity.Profiling.ProfilerMarker adapterMarker = new("FUI.URUI.View.CreateAdapter");
        void InitializeProjection(Action<Element> synchronize)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(UruiView));
            if (initialized || initializing) return;
            if (!scope.Document.IsLoaded) throw new InvalidOperationException("视图初始化需要已加载文档。");
            PrepareGeneration();
            if (initialized || destroyed) return;
            initializing = true;
            try
            {
                using var update = scope.Document.BeginUpdate();
                if (ownsPresentation) scope.SetVisible(visible);
                if (destroyed) return;
                if (ownsPresentation) scope.SetInteractable(interactable);
                if (destroyed) return;
                var snapshot = scope.Document.CreateCompiledQuerySnapshot(scope.Root);
                foreach (var binding in declaredBindings)
                {
                    if (snapshot != null && snapshot.TryQuery(binding.Selector, out var compiledNodes))
                    {
                        if (compiledNodes.Count == 0) throw new InvalidOperationException($"URUI binding '{binding.Name}' has no authored {binding.Selector} node.");
                        foreach (var node in compiledNodes) if (!AddAdapter(binding, node)) return;
                    }
                    else
                    {
                        var nodes = binding.Resolve(scope.Root);
                        if (nodes.Count == 0) throw new InvalidOperationException($"URUI binding '{binding.Name}' has no authored {binding.Selector} node.");
                        foreach (var node in nodes) if (!AddAdapter(binding, node)) return;
                    }
                }
                if (destroyed) return;
                synchronize(scope.Root);
                for (var i = 0; !destroyed && i < elements.Count; i++)
                {
                    if (elements[i] is IExternalValueSource source) source.SynchronizeExternalValues();
                }
                initialized = !destroyed;
                ApplyInputFocus();
            }
            catch (Exception error)
            {
                initialized = false;
                var failure = ReleaseElements(error);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
            finally { initializing = false; }
        }

        bool AddAdapter(UruiElementBinding binding, global::URUI.Elements.Element node)
        {
            using var sample = adapterMarker.Auto();
            var adapter = binding.Create(node, binding.Name) ?? throw new InvalidOperationException($"URUI binding '{binding.Name}' has no adapter.");
            if (destroyed) { (adapter as IDisposable)?.Dispose(); return false; }
            elements.Add(adapter);
            if (!index.TryGetValue(binding.Name, out var list)) { list = new List<IElement>(); index.Add(binding.Name, list); }
            list.Add(adapter);
            return true;
        }

        public T GetElement<T>(string path) where T : IElement
        {
            return (T)GetElement(path, typeof(T));
        }

        public IElement GetElement(string path, Type elementType)
        {
            if (string.IsNullOrEmpty(path) || elementType == null)
            {
                return null;
            }

            if (!index.TryGetValue(path, out var list))
            {
                return null;
            }

            foreach (var element in list)
            {
                if (elementType.IsAssignableFrom(element.GetType()))
                {
                    return element;
                }
            }

            return null;
        }

        readonly List<IExternalValueSource> externalSources = new List<IExternalValueSource>();
        bool externalSubscribed;
        int externalSourcesVersion;
        public bool ExternalValueSynchronizationEnabled { get; set; }

        public void SetExternalValueSources(IReadOnlyList<IExternalValueSource> sources)
        {
            if (destroyed || sources == null) { return; }
            foreach (var source in sources)
            {
                if (source != null && !externalSources.Contains(source)) { externalSources.Add(source); externalSourcesVersion++; }
            }
            if (!externalSubscribed && externalSources.Count > 0)
            {
                scope.Document.LayoutUpdated += SynchronizeExternalValues;
                externalSubscribed = true;
            }
        }

        public void ClearExternalValueSources()
        {
            if (externalSubscribed)
            {
                scope.Document.LayoutUpdated -= SynchronizeExternalValues;
                externalSubscribed = false;
            }
            externalSources.Clear();
            externalSourcesVersion++;
        }

        void SynchronizeExternalValues(global::URUI.Documents.Document document)
        {
            if (destroyed || !ExternalValueSynchronizationEnabled || !visible || !scope.Root.IsValid || !scope.Root.IsAttached || !scope.Root.EnabledInHierarchy) { return; }
            // 同步可能重入解绑；版本检查避免访问释放成员，也不每帧分配快照。
            var version = externalSourcesVersion;
            for (var i = 0; i < externalSources.Count; i++)
            {
                if (destroyed || !ExternalValueSynchronizationEnabled || version != externalSourcesVersion) { break; }
                externalSources[i].SynchronizeExternalValues();
            }
        }

        public void Destroy()
        {
            if (destroyed) return;
            destroyed = true;
            scope.Document.Unloaded -= SuspendDocument;
            scope.Document.Loaded -= RestoreDocument;
            var failure = ReleaseGeneration(null);
            ContentUnavailable = null; ContentAvailable = null;
            try { if (ownsRoot) { scope.Destroy(); } }
            catch (Exception exception) { failure ??= exception; }
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Exception ReleaseGeneration(Exception failure)
        {
            generationPrepared = configureGeneration == null;
            ClearExternalValueSources();
            for (var i = owned.Count - 1; i >= 0; i--)
            {
                try { owned[i].Dispose(); }
                catch (Exception exception) { failure ??= exception; }
            }
            owned.Clear();
            return ReleaseElements(failure);
        }

        Exception ReleaseElements(Exception failure)
        {
            foreach (var element in elements)
            {
                try { (element as IDisposable)?.Dispose(); }
                catch (Exception exception) { failure ??= exception; }
            }
            elements.Clear();
            index.Clear();
            return failure;
        }
    }
}
