using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Presentation;
using FUI.Rendering;
using UnityEngine;
using Element = global::URUI.Elements.Element;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 保留隐藏作者模板，为独立绑定实例提供受控创建和所有权释放。
    /// </summary>
    public sealed class ProjectionTemplateElement : ElementAdapter, IProjectionTemplateElement, IListView, IProjectionLayoutElement, IExternalValueSource
    {
        readonly Dictionary<ViewInstance, View> instances = new Dictionary<ViewInstance, View>(ViewInstance.IdentityComparer.Instance);
        readonly Func<ViewScope, View> factory;
        ProjectionCollection collection;
        BindableProperty<ProjectionLayout> layoutValue;
        bool disposed;

        public ProjectionTemplateElement(Element element, string name, Func<ViewScope, View> factory) : base(element, name)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            element.AddToClassList("projection-prototype");
            Template = new BindableProperty<ProjectionTemplate>(new ProjectionTemplate(CreateProjection, ReadLayout, TryScreenToLocal));
        }

        public BindableProperty<ProjectionTemplate> Template
        {
            get;
        }

        ProjectionCollection Collection
        {
            get
            {
                RequireOwner();
                var document = Element.Document;
                return collection ??= new ProjectionCollection(CreateProjection, PlaceBefore, document.RunAfterEvent, document.BeginUpdate);
            }
        }

        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource => Collection.ItemsSource;

        public BindableProperty<ProjectionLayout> LayoutValue =>
            layoutValue ??= new BindableProperty<ProjectionLayout>(ReadLayout());

        void IListView.OnAdd(object sender, int? index, object itemData) => collection?.Notify(sender, index, 1);
        void IListView.OnRemove(object sender, int? index, object item) => collection?.Notify(sender, index, -1);
        void IListView.OnReplace(object sender, int? index, object oldItem, object newItem) => collection?.Notify(sender, index);
        void IListView.OnUpdate(object sender) => collection?.Notify(sender);

        void IExternalValueSource.SynchronizeExternalValues()
        {
            if (layoutValue != null && !disposed && Element.IsValid && Element.IsAttached)
            {
                layoutValue.Value = ReadLayout();
            }
        }

        void PlaceBefore(ViewInstance instance, ViewInstance next)
        {
            var node = instances[instance].Root;
            var parent = Element.Parent;
            if (next == null)
            {
                parent.Add(node);
                return;
            }

            var target = instances[next].Root;
            if (!ReferenceEquals(node, target))
            {
                parent.InsertBefore(node, target);
            }
        }

        ProjectionCreation CreateProjection(ObservableObject model)
        {
            RequireOwner();
            var clone = Element.Clone();
            clone.Name = string.Empty;
            clone.RemoveFromClassList("projection-prototype");
            var scope = new ViewScope(clone);
            View view = null;
            ViewInstance instance = null;
            try
            {
                Element.Parent.Add(clone);
                view = factory(scope) ?? throw new InvalidOperationException("Projection factory returned no view.");
                RequireOwner();
                view.Visible = false;
                view.Initialize();
                RequireOwner();
                instance = ViewInstance.Create(view, model);
                if (instance == null)
                {
                    throw new InvalidOperationException("Projection model has no registered binding context.");
                }
                RequireOwner();
                instances.Add(instance, view);
                return new ProjectionCreation(instance, Release);
            }
            catch (Exception failure)
            {
                try
                {
                    if (view != null)
                    {
                        DestroyProjection(instance, view);
                    }
                    else
                    {
                        scope.Destroy();
                    }
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(failure, cleanup);
                }
                throw;
            }
        }

        void Release(ViewInstance instance)
        {
            if (!instances.TryGetValue(instance, out var view))
            {
                return;
            }
            instances.Remove(instance);
            DestroyProjection(instance, view);
        }

        static void DestroyProjection(ViewInstance instance, View view)
        {
            Exception failure = null;
            try
            {
                instance?.DestroyProjection();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                view.Destroy();
            }
            catch (Exception exception)
            {
                failure = failure == null ? exception : new AggregateException(failure, exception);
            }
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        ProjectionLayout ReadLayout()
        {
            RequireOwner();
            var parent = Element.Parent;
            return new ProjectionLayout(Dimension(Element, true), Dimension(Element, false),
                Dimension(parent, true), Dimension(parent, false), Pivot(parent, "x"), Pivot(parent, "y"));
        }

        static float Dimension(Element element, bool horizontal)
        {
            if (element == null || element.Parent == null)
            {
                return horizontal ? Screen.width : Screen.height;
            }
            if (element.HasLayout)
            {
                var value = horizontal ? element.Layout.width : element.Layout.height;
                if (value > 0)
                {
                    return value;
                }
            }
            if (element.ResolvedStyle.TryGetLength(horizontal ? "width" : "height", out var length))
            {
                if (length.Unit == global::URUI.Elements.LengthUnit.Pixel)
                {
                    return length.Value;
                }
                if (length.Unit == global::URUI.Elements.LengthUnit.Percent)
                {
                    return Dimension(element.Parent, horizontal) * length.Value / 100;
                }
            }
            throw new InvalidOperationException("Projection layout is not ready; declare authored dimensions or wait for layout.");
        }

        static float Pivot(Element element, string axis) => ElementAdapter.ReadPivot(element, axis);

        bool TryScreenToLocal(float x, float y, out float localX, out float localY)
        {
            RequireOwner();
            var parent = Element.Parent;
            localX = localY = 0;
            if (!parent.HasLayout)
            {
                return false;
            }
            var point = parent.WorldToLocal(new Vector2(x, Screen.height - y)) + parent.ScrollOffset;
            localX = point.x - parent.Layout.width * Pivot(parent, "x");
            localY = parent.Layout.height * (1 - Pivot(parent, "y")) - point.y;
            return true;
        }

        void RequireOwner()
        {
            if (disposed || Element == null || !Element.IsValid || !Element.IsAttached || Element.Parent == null)
            {
                throw new ObjectDisposedException(nameof(ProjectionTemplateElement));
            }
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Exception failure = null;
            try
            {
                collection?.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            layoutValue?.Dispose();
            foreach (var instance in new List<ViewInstance>(instances.Keys))
            {
                try
                {
                    Release(instance);
                }
                catch (Exception exception)
                {
                    failure = failure == null ? exception : new AggregateException(failure, exception);
                }
            }
            Template.Dispose();
            base.Dispose();
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
