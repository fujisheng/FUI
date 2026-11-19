using System;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class ProjectionTemplateElement : ElementAdapter, IProjectionTemplateElement
    {
        readonly Dictionary<ViewInstance, View> instances = new Dictionary<ViewInstance, View>();
        readonly Func<ViewScope, View> factory;
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
            var raw = element.ResolvedStyle.Get(horizontal ? "width" : "height");
            if (raw != null && raw.EndsWith("px", StringComparison.Ordinal) && float.TryParse(raw.Substring(0, raw.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels))
            {
                return pixels;
            }
            if (raw != null && raw.EndsWith("%", StringComparison.Ordinal) && float.TryParse(raw.Substring(0, raw.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return Dimension(element.Parent, horizontal) * percent / 100;
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
