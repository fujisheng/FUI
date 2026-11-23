using System;
using System.Globalization;
using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using global::URUI.Documents;
using global::URUI.Events;
using Element = global::URUI.Elements.Element;
using Template = global::URUI.Controls.Template;

namespace FUI.Rendering.URUI
{
    /// <summary>Realizes a bounded window of fixed-size rows from an authored scrolling list.</summary>
    public sealed class ScrollListElement : ListElement, IScrollableListView
    {
        readonly Element viewport;
        readonly Document document;
        readonly IDisposable scrollSubscription;
        readonly Action refreshAfterEvent;
        readonly System.Collections.Generic.Dictionary<Element, Vector4> positionedItems = new();
        Element measurement;
        Vector2 itemSize;
        Vector2 gap;
        Vector4 padding;
        bool wrap;
        bool horizontal;
        bool borderBox;
        bool disposing;
        bool refreshing;
        bool refreshQueued;
        int count;
        FixedListLayout layout;
        (int Start, int End) range;
        ItemScrollRequest? pending;
        ListScrollAnchor? dataAnchor;
        bool dataAnchorChanged;
        float dataAnchorScrollOffset;
        float? retainedOffset;
        float extent = -1;
        float crossExtent = -1;
        float viewportExtent = -1;

        public ScrollListElement(Element element, string name, string itemTemplateUri,
            Func<ViewScope, View> createItemView, Element viewport)
            : base(RequireViewport(element, viewport), name, itemTemplateUri, createItemView)
        {
            this.viewport = viewport;
            document = viewport.Document;
            refreshAfterEvent = RefreshAfterEvent;
            ScrollRequest = new BindableProperty<ItemScrollRequest?>(null, (_, request) => Request(request));
            scrollSubscription = Subscribe();
        }

        public ScrollListElement(Element element, string name, Template itemTemplate,
            Func<ViewScope, View> createItemView, Element viewport)
            : base(RequireViewport(element, viewport), name, itemTemplate, createItemView)
        {
            this.viewport = viewport;
            document = viewport.Document;
            refreshAfterEvent = RefreshAfterEvent;
            ScrollRequest = new BindableProperty<ItemScrollRequest?>(null, (_, request) => Request(request));
            scrollSubscription = Subscribe();
        }

        public BindableProperty<ItemScrollRequest?> ScrollRequest { get; }

        static Element RequireViewport(Element element, Element viewport)
        {
            if (element == null || viewport == null || !ReferenceEquals(element.Document, viewport.Document))
            {
                throw new ArgumentException("A scrolling list and its viewport must belong to the same document.");
            }
            return element;
        }

        IDisposable Subscribe()
        {
            var subscription = viewport.RegisterCallback<ScrollEvent>(OnScroll);
            document.LayoutUpdated += OnLayout;
            return subscription;
        }

        void OnScroll(ScrollEvent evt)
        {
            if (ReferenceEquals(evt.Target, viewport))
            {
                QueueRefresh();
            }
        }

        void QueueRefresh()
        {
            if (disposing || refreshing || refreshQueued)
            {
                return;
            }
            refreshQueued = true;
            document.RunAfterEvent(refreshAfterEvent);
        }

        void RefreshAfterEvent()
        {
            refreshQueued = false;
            Refresh();
        }

        void OnLayout(Document _)
        {
            if (measurement != null || pending.HasValue || retainedOffset.HasValue ||
                itemSize != Vector2.zero && Element.IsValid && viewport.IsValid &&
                (crossExtent != Cross(Element.ContentRect.size) || viewportExtent != Main(viewport.ContentRect.size)))
            {
                QueueRefresh();
            }
        }

        void Request(ItemScrollRequest? request)
        {
            pending = request;
            dataAnchor = null;
            dataAnchorChanged = false;
            QueueRefresh();
        }

        protected override bool RecycleItems => true;

        protected override void OnItemsSourceChanged()
        {
            pending = null;
            dataAnchor = null;
            dataAnchorChanged = false;
            retainedOffset = null;
            positionedItems.Clear();
        }

        protected override void OnItemsChanged(int? index, int countDelta)
        {
            var source = ItemsSource.Value;
            if (source == null || source.Count == 0 || !viewport.IsValid)
            {
                dataAnchor = null;
                dataAnchorChanged = false;
                return;
            }
            var offset = retainedOffset ?? Main(viewport.ScrollOffset);
            if (!dataAnchorChanged && !Mathf.Approximately(dataAnchorScrollOffset, offset))
            {
                dataAnchor = null;
            }
            if (!dataAnchor.HasValue)
            {
                if (offset <= 0)
                {
                    return;
                }
                var anchor = layout.GetAnchor(count, offset, viewportExtent);
                if (anchor.Index < 0)
                {
                    return;
                }
                dataAnchor = new ListScrollAnchor(anchor.Index, anchor.Offset, GetRealizedModel(anchor.Index));
            }
            dataAnchor = dataAnchor.Value.Move(source, index, countDelta);
            dataAnchorChanged = true;
        }

        protected override (int Start, int End) GetRange(int count)
        {
            this.count = count;
            if (count == 0)
            {
                pending = null;
                retainedOffset = null;
                range = default;
                UpdateExtent();
                return range;
            }

            if (itemSize == Vector2.zero)
            {
                if (measurement == null || !measurement.IsValid)
                {
                    measurement = CreateMeasurementItem();
                }
                if (measurement.IsAttached && !measurement.HasLayout)
                {
                    document.RequestGeometry(measurement);
                    document.FlushPendingGeometry();
                }
                if (!ReadMeasurement())
                {
                    return default;
                }
            }

            ReadViewport();
            UpdateExtent();
            if (dataAnchorChanged)
            {
                var anchor = dataAnchor.Value;
                dataAnchorChanged = false;
                dataAnchorScrollOffset = layout.GetAnchoredOffset(count, anchor.Index, anchor.Offset, viewportExtent);
                retainedOffset = dataAnchorScrollOffset;
            }
            range = layout.GetRange(count, retainedOffset ?? Main(viewport.ScrollOffset), viewportExtent);
            return range;
        }

        float Main(Vector2 size) => horizontal ? size.x : size.y;
        float Cross(Vector2 size) => horizontal ? size.y : size.x;
        float StartPadding => horizontal ? padding.x : padding.y;
        float EndPadding => horizontal ? padding.z : padding.w;

        void ReadViewport()
        {
            if (!viewport.HasLayout || !Element.HasLayout)
            {
                return;
            }

            var previous = layout;
            var previousViewport = viewportExtent;
            crossExtent = Cross(Element.ContentRect.size);
            viewportExtent = Main(viewport.ContentRect.size);
            var crossGap = Cross(gap);
            var across = wrap
                ? Mathf.Max(1, Mathf.FloorToInt((crossExtent + crossGap + .001f) / (Cross(itemSize) + crossGap)))
                : 1;
            layout = new FixedListLayout(across, Main(itemSize), Main(gap), StartPadding, EndPadding);
            if (previous.ItemsPerLine > 0 && previous.ItemsPerLine != across && count > 0)
            {
                retainedOffset = layout.RetainAnchor(previous, count, Main(viewport.ScrollOffset), previousViewport, viewportExtent);
            }
        }

        void UpdateExtent()
        {
            if (itemSize == Vector2.zero)
            {
                return;
            }
            var next = layout.GetExtent(count);
            if (next != extent)
            {
                extent = next;
                var size = Mathf.Max(0, borderBox ? extent : extent - StartPadding - EndPadding);
                if (horizontal)
                {
                    Element.Style.SetWidth(size);
                }
                else
                {
                    Element.Style.SetHeight(size);
                }
            }
        }

        void Refresh()
        {
            if (disposing || refreshing || !Element.IsValid || !viewport.IsValid || !viewport.HasLayout)
            {
                return;
            }

            refreshing = true;
            try
            {
                var geometryChanged = ReadMeasurement();
                if (itemSize == Vector2.zero)
                {
                    return;
                }
                if (geometryChanged || crossExtent != Cross(Element.ContentRect.size) ||
                    viewportExtent != Main(viewport.ContentRect.size))
                {
                    ReadViewport();
                    UpdateExtent();
                    geometryChanged = true;
                }

                if ((pending.HasValue || retainedOffset.HasValue) && viewportExtent > 0 &&
                    Main(viewport.ScrollSize) + .1f >= Mathf.Max(0, extent))
                {
                    var index = pending?.Index ?? -1;
                    var offset = viewport.ScrollOffset;
                    var value = index >= 0 && index < count
                        ? layout.GetScrollOffset(count, index, Main(offset), viewportExtent)
                        : retainedOffset ?? Main(offset);
                    pending = null;
                    retainedOffset = null;
                    if (horizontal)
                    {
                        offset.x = value;
                    }
                    else
                    {
                        offset.y = value;
                    }
                    viewport.ScrollOffset = offset;
                }
                var nextRange = layout.GetRange(count, Main(viewport.ScrollOffset), viewportExtent);
                if (geometryChanged || nextRange != range)
                {
                    Rebuild();
                }
            }
            finally
            {
                refreshing = false;
            }
        }

        bool ReadMeasurement()
        {
            if (measurement == null || !measurement.IsValid || !measurement.HasLayout)
            {
                return false;
            }
            var measured = measurement.Layout.size;
            if (measured.x <= 0 || measured.y <= 0)
            {
                return false;
            }
            itemSize = measured;
            horizontal = viewport.ResolvedStyle.Get("overflow-x") is "auto" or "scroll" &&
                viewport.ResolvedStyle.Get("overflow-y") is not ("auto" or "scroll");
            gap = new Vector2(Pixels("column-gap"), Pixels("row-gap"));
            padding = new Vector4(Pixels("padding-left"), Pixels("padding-top"), Pixels("padding-right"), Pixels("padding-bottom"));
            wrap = Element.ResolvedStyle.Get("flex-wrap") == "wrap";
            borderBox = Element.ResolvedStyle.Get("box-sizing") == "border-box";
            if (Element.ResolvedStyle.Get("position") == "static")
            {
                Element.Style.Set("position", "relative");
            }
            measurement.Destroy();
            measurement = null;
            return true;
        }

        float Pixels(string property)
        {
            if (Element.ResolvedStyle.TryGetLength(property, out var length) && length.Unit == global::URUI.Elements.LengthUnit.Pixel)
            {
                return Mathf.Max(0, length.Value);
            }
            var value = Element.ResolvedStyle.Get(property);
            if (!string.IsNullOrEmpty(value) && value.EndsWith("px", StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - 2);
            }
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) ? Mathf.Max(0, size) : 0;
        }

        protected override void PositionItem(Element item, int index)
        {
            var main = layout.GetItemOffset(index);
            var cross = index % layout.ItemsPerLine * (Cross(itemSize) + Cross(gap));
            var left = horizontal ? main : padding.x + cross;
            var top = horizontal ? padding.y + cross : main;
            var rect = new Vector4(left, top, itemSize.x, itemSize.y);
            if (positionedItems.TryGetValue(item, out var previous))
            {
                if (previous == rect)
                {
                    return;
                }
            }
            else
            {
                item.Style.Position = global::URUI.Elements.Position.Absolute;
            }
            item.Style.SetLeft(left);
            item.Style.SetTop(top);
            if (previous.z != itemSize.x || previous.w != itemSize.y)
            {
                item.Style.SetWidth(itemSize.x);
                item.Style.SetHeight(itemSize.y);
            }
            positionedItems[item] = rect;
        }

        public override void Dispose()
        {
            if (disposing)
            {
                return;
            }
            disposing = true;
            positionedItems.Clear();
            document.LayoutUpdated -= OnLayout;
            scrollSubscription.Dispose();
            ScrollRequest.Dispose();
            if (measurement != null && measurement.IsValid)
            {
                measurement.Destroy();
            }
            base.Dispose();
        }
    }
}
