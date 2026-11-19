using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using URUI.Documents;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 把中立屏幕端点绘制为原生线段，复用空间适配器处理父变换与释放。
    /// </summary>
    public sealed class ConnectionLinesElement : ElementAdapter, IConnectionLinesElement
    {
        readonly Action<IReadOnlyList<LineSegment>> render;
        bool subscribed;
        bool dirty;
        bool disposed;
        Vector2 topLeft;
        Vector2 topRight;
        Vector2 bottomLeft;
        Vector2 bottomRight;

        public ConnectionLinesElement(global::URUI.Elements.Element element, string name,
            Action<IReadOnlyList<LineSegment>> render) : base(element, name)
        {
            this.render = render ?? throw new ArgumentNullException(nameof(render));
            LineSegments = new BindableProperty<IReadOnlyList<LineSegment>>(null, (_, value) => RequestUpdate(), ValidateSegments);
        }

        public BindableProperty<IReadOnlyList<LineSegment>> LineSegments
        {
            get;
        }

        static IReadOnlyList<LineSegment> ValidateSegments(IReadOnlyList<LineSegment> values)
        {
            if (values == null)
            {
                return null;
            }
            foreach (var value in values)
            {
                if (!float.IsFinite(value.from.x) || !float.IsFinite(value.from.y) || !float.IsFinite(value.from.z) ||
                    !float.IsFinite(value.to.x) || !float.IsFinite(value.to.y) || !float.IsFinite(value.to.z) ||
                    !float.IsFinite(value.color.r) || !float.IsFinite(value.color.g) || !float.IsFinite(value.color.b) || !float.IsFinite(value.color.a))
                {
                    throw new ArgumentOutOfRangeException(nameof(values), "Line endpoints and colors must be finite.");
                }
            }
            return values;
        }

        void RequestUpdate()
        {
            var count = LineSegments.Value?.Count ?? 0;
            if (count == 0)
            {
                try
                {
                    render(Array.Empty<LineSegment>());
                }
                finally
                {
                    Unsubscribe();
                    dirty = false;
                }
                return;
            }
            dirty = true;
            if (!subscribed)
            {
                Element.Document.LayoutUpdated += OnLayout;
                subscribed = true;
            }
        }

        void OnLayout(Document document)
        {
            if (disposed || Element == null || !Element.IsValid || !Element.HasLayout)
            {
                return;
            }
            var size = Element.Layout.size;
            if (size.x <= 0 || size.y <= 0)
            {
                return;
            }
            var a = Element.LocalToWorld(Vector2.zero);
            var b = Element.LocalToWorld(new Vector2(size.x, 0));
            var c = Element.LocalToWorld(new Vector2(0, size.y));
            var d = Element.LocalToWorld(size);
            if (Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < .00001f)
            {
                return;
            }
            var changed = a != topLeft || b != topRight || c != bottomLeft || d != bottomRight;
            if (dirty || changed)
            {
                UpdateStrokes();
                dirty = false;
                topLeft = a;
                topRight = b;
                bottomLeft = c;
                bottomRight = d;
            }
        }

        void UpdateStrokes()
        {
            var values = LineSegments.Value;
            var local = new LineSegment[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                var segment = values[i];
                var from = Element.WorldToLocal(new Vector2(segment.from.x, Screen.height - segment.from.y));
                var to = Element.WorldToLocal(new Vector2(segment.to.x, Screen.height - segment.to.y));
                local[i] = new LineSegment(new SpatialValue(from.x, from.y, 0),
                    new SpatialValue(to.x, to.y, 0), segment.color);
            }
            render(local);
        }

        void Unsubscribe()
        {
            if (subscribed)
            {
                Element.Document.LayoutUpdated -= OnLayout;
                subscribed = false;
            }
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Unsubscribe();
            try
            {
                render(Array.Empty<LineSegment>());
            }
            finally
            {
                LineSegments.Dispose();
                base.Dispose();
            }
        }
    }
}
