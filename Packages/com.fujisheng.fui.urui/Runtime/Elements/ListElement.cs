using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Rendering;
using UnityEngine;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// ListView 的 HTML 版本：编译模板实例拥有独立 Scope/ViewInstance；集合变化保留未变条目。
    /// </summary>
    public class ListElement : ElementAdapter, IListView
    {
        readonly List<ViewScope> scopes = new List<ViewScope>();
        readonly List<View> views = new List<View>();
        readonly List<ViewInstance> instances = new List<ViewInstance>();
        readonly List<ObservableObject> models = new List<ObservableObject>();
        bool rebuilding;
        bool rebuildAgain;
        bool disposed;
        bool ownsContent;
        readonly string templateUri;
        readonly Func<ViewScope, View> viewFactory;

        /// <summary>
        /// 克隆外置作者片段；布局由普通流式样式决定，条目绑定由工厂明确提供。
        /// </summary>
        public ListElement(global::URUI.Elements.Element element, string name,
            string templateUri, Func<ViewScope, View> viewFactory) : base(element, name)
        {
            this.templateUri = templateUri ?? throw new ArgumentNullException(nameof(templateUri));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            Items = new BindableProperty<IReadOnlyObservableList<ObservableObject>>(null, (_, __) => Rebuild());
        }

        public BindableProperty<IReadOnlyObservableList<ObservableObject>> Items
        {
            get;
        }
        /// <summary>
        /// 按数据顺序获取当前条目视图；索引在列表变化后重新解析。
        /// </summary>
        public IView GetItemView(int index) => index >= 0 && index < views.Count ? views[index] : null;

        public void OnAdd(object sender, int? index, object itemData) => Rebuild();
        public void OnRemove(object sender, int? index, object item) => Rebuild();
        public void OnReplace(object sender, int? index, object oldItem, object newItem) => Rebuild();
        public void OnUpdate(object sender) => Rebuild();

        void Rebuild()
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
            rebuilding = true;
            try
            {
                if (!ownsContent)
                {
                    Element.SetInnerHtml(string.Empty, Element.Document.SourceUrl);
                    ownsContent = true;
                }
                do
                {
                    using var update = Element.Document.BeginUpdate();
                    rebuildAgain = false;
                    var items = Items.Value;
                    var desired = new ObservableObject[items?.Count ?? 0];
                    for (var i = 0; i < desired.Length; i++)
                    {
                        desired[i] = items[i];
                    }
                    global::URUI.Elements.Element prototype = null;
                    try
                    {
                        for (var i = 0; i < desired.Length && !disposed && !rebuildAgain; i++)
                        {
                            var existing = i;
                            while (existing < models.Count && !ReferenceEquals(models[existing], desired[i]))
                            {
                                existing++;
                            }
                            if (existing < models.Count)
                            {
                                if (existing != i)
                                {
                                    Element.Insert(i, scopes[existing].Root);
                                    Move(scopes, existing, i);
                                    Move(views, existing, i);
                                    Move(instances, existing, i);
                                    Move(models, existing, i);
                                }
                                continue;
                            }
                            if (prototype == null)
                            {
                                prototype = Stage.CreateHost(Element.Document, "flow-item-host");
                                Element.Add(prototype);
                                prototype.SetAttribute("hidden", "");
                                prototype.SetInnerHtml(LoadTemplate());
                            }
                            InsertItem(i, desired[i], prototype);
                        }
                        while (!disposed && !rebuildAgain && scopes.Count > desired.Length)
                        {
                            RemoveItem(scopes.Count - 1);
                        }
                    }
                    finally
                    {
                        if (prototype != null && prototype.IsValid)
                        {
                            prototype.Destroy();
                        }
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
                rebuilding = false;
            }
        }

        static void Move<T>(List<T> values, int from, int to)
        {
            var value = values[from];
            values.RemoveAt(from);
            values.Insert(to, value);
        }

        void InsertItem(int index, ObservableObject model, global::URUI.Elements.Element prototype)
        {
            var container = prototype.Clone();
            container.RemoveAttribute("hidden");
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
                if (Element != null && Element.IsValid && Element.IsAttached)
                {
                    Element.SetInnerHtml(string.Empty, Element.Document.SourceUrl);
                }
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
                Items?.Dispose();
                base.Dispose();
            }
        }
    }
}
