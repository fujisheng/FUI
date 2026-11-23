using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;
using UnityEngine;
using Element = global::URUI.Elements.Element;
using Template = global::URUI.Controls.Template;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// ListView 的 HTML 版本：编译模板实例拥有独立 Scope/ViewInstance；集合变化保留未变条目。
    /// </summary>
    public class ListElement : ElementAdapter, IListView
    {
        sealed class ReferenceEqualityComparer : IEqualityComparer<ObservableObject>
        {
            internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(ObservableObject x, ObservableObject y) => ReferenceEquals(x, y);
            public int GetHashCode(ObservableObject obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        readonly struct ParkedItem
        {
            internal readonly ViewScope Scope;
            internal readonly View View;
            internal readonly ViewInstance Instance;
            internal readonly ObservableObject Model;
            internal ParkedItem(ViewScope scope, View view, ViewInstance instance, ObservableObject model)
            {
                Scope = scope;
                View = view;
                Instance = instance;
                Model = model;
            }
        }

        readonly List<ViewScope> scopes = new List<ViewScope>();
        readonly List<View> views = new List<View>();
        readonly List<ViewInstance> instances = new List<ViewInstance>();
        readonly List<ObservableObject> models = new List<ObservableObject>();
        readonly List<ObservableObject> desired = new List<ObservableObject>();
        readonly Dictionary<ObservableObject, int> desiredCounts = new Dictionary<ObservableObject, int>(ReferenceEqualityComparer.Instance);
        readonly Dictionary<ObservableObject, int> modelPositions = new Dictionary<ObservableObject, int>(ReferenceEqualityComparer.Instance);
        readonly List<ParkedItem> recyclable = new List<ParkedItem>();
        readonly Action applyRebuild;
        bool rebuilding;
        bool rebuildAgain;
        bool rebuildQueued;
        bool disposed;
        bool ownsContent;
        int firstItem;
        readonly string templateUri;
        readonly Template itemTemplate;
        readonly Func<ViewScope, View> viewFactory;
        Element prototypeHost;
        Element inlinePrototype;
        Element externalPrototype;

        /// <summary>
        /// 克隆外置作者片段；布局由普通流式样式决定，条目绑定由工厂明确提供。
        /// </summary>
        public ListElement(global::URUI.Elements.Element element, string name,
            string templateUri, Func<ViewScope, View> viewFactory)
            : this(element, name, templateUri ?? throw new ArgumentNullException(nameof(templateUri)), null, viewFactory)
        {
        }

        /// <summary>实例化同一文档中的 HTML template，并为每个条目创建独立绑定作用域。</summary>
        public ListElement(Element element, string name, Template itemTemplate, Func<ViewScope, View> viewFactory)
            : this(element, name, null, RequireTemplate(element, itemTemplate), viewFactory)
        {
        }

        ListElement(Element element, string name, string templateUri, Template itemTemplate,
            Func<ViewScope, View> viewFactory) : base(element, name)
        {
            this.templateUri = templateUri;
            this.itemTemplate = itemTemplate;
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            applyRebuild = ApplyRebuild;
            ItemsSource = new BindableProperty<IReadOnlyObservableList<ObservableObject>>(null, (_, __) =>
            {
                OnItemsSourceChanged();
                Rebuild();
            });
        }

        static Template RequireTemplate(Element element, Template itemTemplate)
        {
            if (itemTemplate == null)
            {
                throw new ArgumentNullException(nameof(itemTemplate));
            }
            if (element == null || !ReferenceEquals(element.Document, itemTemplate.Document))
            {
                throw new ArgumentException("An inline item template must be in the list's document.", nameof(itemTemplate));
            }
            return itemTemplate;
        }

        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource
        {
            get;
        }
        /// <summary>
        /// 按数据顺序获取当前条目视图；索引在列表变化后重新解析。
        /// </summary>
        public IView GetItemView(int index)
        {
            TryGetRealizedItem(index, out var view);
            return view;
        }

        /// <summary>供渲染扩展查询已实现的行。离屏查询不会创建视图。</summary>
        public bool TryGetRealizedItem(int index, out IView view)
        {
            view = null;
            var source = ItemsSource.Value;
            if (source == null || index < 0 || index >= source.Count)
            {
                return false;
            }
            var position = index - firstItem;
            if (position >= 0 && position < views.Count && ReferenceEquals(models[position], source[index]))
            {
                view = views[position];
            }
            return view != null;
        }

        public void OnAdd(object sender, int? index, object itemData) => ItemsChanged(index, 1);
        public void OnRemove(object sender, int? index, object item) => ItemsChanged(index, -1);
        public void OnReplace(object sender, int? index, object oldItem, object newItem) => ItemsChanged(index, 0);
        public void OnUpdate(object sender) => ItemsChanged(null, 0);

        void ItemsChanged(int? index, int countDelta)
        {
            OnItemsChanged(index, countDelta);
            Rebuild();
        }

        protected virtual void OnItemsSourceChanged()
        {
        }

        protected virtual void OnItemsChanged(int? index, int countDelta)
        {
        }

        protected ObservableObject GetRealizedModel(int index)
        {
            var position = index - firstItem;
            return position >= 0 && position < models.Count ? models[position] : null;
        }

        protected void Rebuild()
        {
            if (disposed)
            {
                return;
            }
            if (rebuilding)
            {
                rebuildAgain = true;
                return;
            }
            if (!rebuildQueued)
            {
                rebuildQueued = true;
                Element.Document.RunAfterEvent(applyRebuild);
            }
        }

        void ApplyRebuild()
        {
            rebuildQueued = false;
            if (disposed || !Element.IsValid)
            {
                return;
            }
            rebuilding = true;
            try
            {
                if (!ownsContent)
                {
                    if (itemTemplate != null && Element.Contains(itemTemplate))
                    {
                        GetInlinePrototype();
                    }
                    Element.SetInnerHtml(string.Empty, Element.Document.SourceUrl);
                    ownsContent = true;
                }
                do
                {
                    using var update = Element.Document.BeginUpdate();
                    rebuildAgain = false;
                    var items = ItemsSource.Value;
                    var range = GetRange(items?.Count ?? 0);
                    firstItem = range.Start;
                    desired.Clear();
                    desiredCounts.Clear();
                    var hasDuplicateDesired = false;
                    for (var index = range.Start; index < range.End; index++)
                    {
                        var item = items[index];
                        desired.Add(item);
                        if (item != null)
                        {
                            if (desiredCounts.TryGetValue(item, out var existingCount))
                            {
                                desiredCounts[item] = existingCount + 1;
                                hasDuplicateDesired = true;
                            }
                            else
                            {
                                desiredCounts.Add(item, 1);
                            }
                        }
                    }

                    // Pass 1: Evict or park unneeded existing rows up-front so retained rows stay in-place (O(1) scroll churn).
                    for (var idx = models.Count - 1; idx >= 0 && !disposed && !rebuildAgain; idx--)
                    {
                        var model = models[idx];
                        if (model != null && desiredCounts.TryGetValue(model, out var remaining) && remaining > 0)
                        {
                            desiredCounts[model] = remaining - 1;
                            continue;
                        }
                        if (RecycleItems && instances[idx] != null && model != null)
                        {
                            recyclable.Add(new ParkedItem(scopes[idx], views[idx], instances[idx], model));
                            scopes.RemoveAt(idx);
                            views.RemoveAt(idx);
                            instances.RemoveAt(idx);
                            models.RemoveAt(idx);
                        }
                        else
                        {
                            RemoveItem(idx);
                        }
                    }

                    // Pass 2: Build O(1) lookup for remaining retained rows when models are distinct.
                    var useIndexMap = !hasDuplicateDesired && models.Count > 4;
                    if (useIndexMap)
                    {
                        modelPositions.Clear();
                        for (var idx = 0; idx < models.Count; idx++)
                        {
                            var model = models[idx];
                            if (model != null && !modelPositions.ContainsKey(model))
                            {
                                modelPositions.Add(model, idx);
                            }
                            else
                            {
                                useIndexMap = false;
                                break;
                            }
                        }
                    }

                    for (var i = 0; i < desired.Count && !disposed && !rebuildAgain; i++)
                    {
                        var targetModel = desired[i];
                        if (i < models.Count && ReferenceEquals(models[i], targetModel))
                        {
                            continue;
                        }
                        var existing = -1;
                        if (useIndexMap && targetModel != null && modelPositions.TryGetValue(targetModel, out var mapped) &&
                            mapped >= i && mapped < models.Count && ReferenceEquals(models[mapped], targetModel))
                        {
                            existing = mapped;
                        }
                        else
                        {
                            for (var scan = i; scan < models.Count; scan++)
                            {
                                if (ReferenceEquals(models[scan], targetModel))
                                {
                                    existing = scan;
                                    break;
                                }
                            }
                        }
                        if (existing >= 0)
                        {
                            Reorder(existing, i);
                            if (useIndexMap)
                            {
                                for (var k = i; k <= existing; k++)
                                {
                                    if (models[k] != null)
                                    {
                                        modelPositions[models[k]] = k;
                                    }
                                }
                            }
                            continue;
                        }

                        if (RecycleItems && TryRecycleParked(i, targetModel))
                        {
                            useIndexMap = false;
                            continue;
                        }
                        if (itemTemplate != null)
                        {
                            InsertItem(i, targetModel, GetInlinePrototype());
                        }
                        else
                        {
                            InsertItem(i, targetModel, GetExternalPrototype());
                        }
                        useIndexMap = false;
                    }
                    if (!disposed && !rebuildAgain)
                    {
                        while (scopes.Count > desired.Count)
                        {
                            RemoveItem(scopes.Count - 1);
                        }
                        DrainRecyclable();
                        for (var i = 0; i < scopes.Count && !disposed && !rebuildAgain; i++)
                        {
                            PositionItem(scopes[i].Root, firstItem + i);
                        }
                    }
                    else if (!disposed && rebuildAgain)
                    {
                        RestoreRecyclable();
                    }
                } while (!disposed && rebuildAgain);
            }
            catch (Exception failure)
            {
                try
                {
                    ReleaseItems();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(failure, cleanupFailure);
                }
                throw;
            }
            finally
            {
                desired.Clear();
                desiredCounts.Clear();
                modelPositions.Clear();
                rebuilding = false;
            }
        }

        static void Move<T>(List<T> values, int from, int to)
        {
            var value = values[from];
            values.RemoveAt(from);
            values.Insert(to, value);
        }

        protected virtual (int Start, int End) GetRange(int count) => (0, count);
        protected virtual bool RecycleItems => false;
        protected virtual void PositionItem(Element item, int index)
        {
        }

        protected Element CreateMeasurementItem()
        {
            var item = itemTemplate == null ? Stage.CreateHost(Element.Document, "flow-item-host") : GetInlinePrototype().Clone();
            try
            {
                item.Style.Set("visibility", "hidden");
                item.Style.Set("position", "absolute");
                item.Style.Set("pointer-events", "none");
                Element.Add(item);
                if (itemTemplate == null)
                {
                    item.SetInnerHtml(LoadTemplate());
                }
                return item;
            }
            catch
            {
                if (item.IsValid)
                {
                    item.Destroy();
                }
                throw;
            }
        }

        void Reorder(int from, int to)
        {
            if (from == to)
            {
                return;
            }
            Element.Insert(to, scopes[from].Root);
            Move(scopes, from, to);
            Move(views, from, to);
            Move(instances, from, to);
            Move(models, from, to);
        }

        bool TryRecycleParked(int position, ObservableObject model)
        {
            if (model == null || recyclable.Count == 0)
            {
                return false;
            }
            var targetType = model.GetType();
            for (var i = recyclable.Count - 1; i >= 0; i--)
            {
                var parked = recyclable[i];
                if (parked.Model.GetType() != targetType)
                {
                    continue;
                }
                recyclable.RemoveAt(i);
                Element.Insert(position, parked.Scope.Root);
                scopes.Insert(position, parked.Scope);
                views.Insert(position, parked.View);
                instances.Insert(position, parked.Instance);
                models.Insert(position, model);
                parked.Instance.UpdateViewModel(model);
                InitializeItem(parked.Scope, model);
                return true;
            }
            return false;
        }

        void RestoreRecyclable()
        {
            for (var i = recyclable.Count - 1; i >= 0; i--)
            {
                var parked = recyclable[i];
                scopes.Add(parked.Scope);
                views.Add(parked.View);
                instances.Add(parked.Instance);
                models.Add(parked.Model);
            }
            recyclable.Clear();
        }

        void DrainRecyclable()
        {
            Exception failure = null;
            for (var i = recyclable.Count - 1; i >= 0; i--)
            {
                var parked = recyclable[i];
                recyclable.RemoveAt(i);
                try
                {
                    DestroyEntry(parked.Instance, parked.View, parked.Scope);
                }
                catch (Exception error)
                {
                    failure ??= error;
                }
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        Element GetExternalPrototype()
        {
            if (externalPrototype != null && externalPrototype.IsValid)
            {
                return externalPrototype;
            }
            var host = Stage.CreateHost(Element.Document, "flow-item-host");
            try
            {
                host.SetInnerHtml(LoadTemplate());
                externalPrototype = host;
                return externalPrototype;
            }
            catch
            {
                if (host.IsValid)
                {
                    host.Destroy();
                }
                throw;
            }
        }

        Element GetInlinePrototype()
        {
            if (inlinePrototype != null)
            {
                return inlinePrototype;
            }

            prototypeHost = Stage.CreateHost(Element.Document, "flow-list-template");
            itemTemplate.CloneContentTo(prototypeHost);
            foreach (var child in prototypeHost.Hierarchy.Children())
            {
                var tag = child.TagName;
                if (tag == "#comment" || tag == "#text" && string.IsNullOrWhiteSpace(child.Text))
                {
                    continue;
                }
                if (inlinePrototype != null || tag == "#text")
                {
                    throw new InvalidOperationException("A list item template must have a single element root.");
                }
                inlinePrototype = child;
            }

            if (inlinePrototype == null ||
                (Element.TagName == "ul" || Element.TagName == "ol") && inlinePrototype.TagName != "li")
            {
                throw new InvalidOperationException("An inline item requires a single root. Lists require an li element.");
            }
            return inlinePrototype;
        }

        void InsertItem(int index, ObservableObject model, global::URUI.Elements.Element prototype)
        {
            var container = prototype.Clone();
            Element.Insert(index, container);
            var scope = new ViewScope(container);
            scopes.Insert(index, scope);
            models.Insert(index, model);
            views.Insert(index, null);
            instances.Insert(index, null);
            InitializeItem(scope, model);
            if (disposed)
            {
                return;
            }
            var view = viewFactory(scope) ?? throw new InvalidOperationException("列表条目工厂未返回视图：" + Name);
            if (disposed)
            {
                view.Destroy();
                return;
            }
            views[index] = view;
            view.PrepareProjection();
            if (disposed)
            {
                return;
            }
            var instance = ViewInstance.Create(view, model);
            if (disposed)
            {
                instance?.DestroyProjection();
                return;
            }
            instances[index] = instance;
            instance?.Enable();
        }

        void RemoveItem(int index)
        {
            var instance = instances[index];
            var view = views[index];
            var scope = scopes[index];
            instances.RemoveAt(index);
            views.RemoveAt(index);
            scopes.RemoveAt(index);
            models.RemoveAt(index);
            DestroyEntry(instance, view, scope);
        }

        static void DestroyEntry(ViewInstance instance, View view, ViewScope scope)
        {
            Exception failure = null;
            try
            {
                instance?.DestroyProjection();
            }
            catch (Exception error)
            {
                failure = error;
            }
            try
            {
                view?.Destroy();
            }
            catch (Exception error)
            {
                failure ??= error;
            }
            try
            {
                scope.Destroy();
            }
            catch (Exception error)
            {
                failure ??= error;
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// 应用可向已加载的作者子树绑定条目数据，不生成结构或样式。
        /// </summary>
        protected virtual void InitializeItem(ViewScope scope, ObservableObject item)
        {
        }

        global::URUI.Resources.CompiledTemplate LoadTemplate() => Element.Document.GetCompiledTemplate(templateUri);

        void ReleaseItems()
        {
            Exception failure = null;
            try
            {
                DrainRecyclable();
            }
            catch (Exception error)
            {
                failure ??= error;
            }
            while (scopes.Count > 0)
            {
                try
                {
                    RemoveItem(scopes.Count - 1);
                }
                catch (Exception error)
                {
                    failure ??= error;
                }
            }
            try
            {
                if (externalPrototype != null && externalPrototype.IsValid)
                {
                    externalPrototype.Destroy();
                }
                if (prototypeHost != null && prototypeHost.IsValid)
                {
                    prototypeHost.Destroy();
                }
                if (Element != null && Element.IsValid && Element.IsAttached)
                {
                    Element.SetInnerHtml(string.Empty, Element.Document.SourceUrl);
                }
            }
            catch (Exception error)
            {
                failure ??= error;
            }
            finally
            {
                inlinePrototype = null;
                prototypeHost = null;
                externalPrototype = null;
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            try
            {
                ReleaseItems();
            }
            finally
            {
                ItemsSource?.Dispose();
                base.Dispose();
            }
        }
    }
}
