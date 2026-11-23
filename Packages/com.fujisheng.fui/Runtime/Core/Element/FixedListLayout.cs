using System;
using System.Collections.Generic;
using FUI.Binding;

namespace FUI.Rendering
{
    /// <summary>Computes the window and positions of uniformly sized list items or grid cells.</summary>
    internal readonly struct FixedListLayout
    {
        internal int ItemsPerLine { get; }
        internal float ItemExtent { get; }
        readonly float spacing;
        readonly float leadingPadding;
        readonly float trailingPadding;
        float Stride => ItemExtent + spacing;

        internal FixedListLayout(int itemsPerLine, float itemExtent, float spacing, float leadingPadding, float trailingPadding)
        {
            if (itemsPerLine < 1 || !(itemExtent > 0) || !(spacing >= 0) || !(leadingPadding >= 0) ||
                !(trailingPadding >= 0) || float.IsInfinity(itemExtent + spacing + leadingPadding + trailingPadding))
            {
                throw new ArgumentException("A list layout requires finite nonnegative spacing and positive item sizes.");
            }

            ItemsPerLine = itemsPerLine;
            ItemExtent = itemExtent;
            this.spacing = spacing;
            this.leadingPadding = leadingPadding;
            this.trailingPadding = trailingPadding;
        }

        internal float GetExtent(int count)
        {
            var lines = LineCount(count);
            return leadingPadding + trailingPadding + lines * ItemExtent + Math.Max(0, lines - 1) * spacing;
        }

        internal float GetItemOffset(int index)
        {
            return leadingPadding + index / ItemsPerLine * Stride;
        }

        internal (int Start, int End) GetRange(int count, float scrollOffset, float viewportExtent)
        {
            if (count <= 0 || !(viewportExtent > 0) || float.IsInfinity(viewportExtent) || float.IsNaN(scrollOffset))
            {
                return default;
            }

            var lines = LineCount(count);
            var offset = ClampOffset(count, scrollOffset, viewportExtent);
            var firstVisible = Math.Floor(((double)offset - leadingPadding - ItemExtent) / Stride) + 1;
            var endVisible = Math.Ceiling(((double)offset + viewportExtent - leadingPadding) / Stride);
            var first = ClampLine(firstVisible - 1, lines);
            var end = ClampLine(endVisible + 1, lines);
            return ((int)Math.Min(count, (long)first * ItemsPerLine), (int)Math.Min(count, (long)end * ItemsPerLine));
        }

        internal float GetScrollOffset(int count, int index, float currentOffset, float viewportExtent)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var start = GetItemOffset(index);
            var current = ClampOffset(count, currentOffset, viewportExtent);
            if (start < current || ItemExtent > viewportExtent)
            {
                return ClampOffset(count, start, viewportExtent);
            }

            var end = start + ItemExtent;
            return end > current + viewportExtent ? ClampOffset(count, end - viewportExtent, viewportExtent) : current;
        }

        internal float RetainAnchor(FixedListLayout previous, int count, float offset, float previousViewport, float viewport)
        {
            var anchor = previous.GetAnchor(count, offset, previousViewport);
            if (anchor.Index < 0)
            {
                return ClampOffset(count, offset, viewport);
            }

            return GetAnchoredOffset(count, anchor.Index, anchor.Offset, viewport);
        }

        internal (int Index, float Offset) GetAnchor(int count, float offset, float viewport)
        {
            if (count <= 0 || ItemsPerLine == 0 || viewport <= 0)
            {
                return (-1, 0);
            }

            var clamped = ClampOffset(count, offset, viewport);
            var line = Math.Floor(((double)clamped - leadingPadding - ItemExtent) / Stride) + 1;
            var index = ClampLine(line, LineCount(count) - 1) * ItemsPerLine;
            return (index, clamped - GetItemOffset(index));
        }

        internal float GetAnchoredOffset(int count, int index, float offset, float viewport)
        {
            return ClampOffset(count, GetItemOffset(index) + offset, viewport);
        }

        int LineCount(int count)
        {
            return count <= 0 ? 0 : (count - 1) / ItemsPerLine + 1;
        }

        float ClampOffset(int count, float offset, float viewportExtent)
        {
            return Math.Min(Math.Max(0, offset), Math.Max(0, GetExtent(count) - viewportExtent));
        }

        static int ClampLine(double line, int count)
        {
            return (int)Math.Min(count, Math.Max(0, line));
        }
    }

    /// <summary>Remembers a visible occurrence and its local position while the source is edited.</summary>
    internal readonly struct ListScrollAnchor
    {
        internal int Index { get; }
        internal float Offset { get; }
        readonly ObservableObject item;

        internal ListScrollAnchor(int index, float offset, ObservableObject item)
        {
            Index = index;
            Offset = offset;
            this.item = item;
        }

        internal ListScrollAnchor Move(IReadOnlyList<ObservableObject> items, int? changedIndex, int countDelta)
        {
            var index = Index;
            if (changedIndex.HasValue)
            {
                if (changedIndex.Value < index || changedIndex.Value == index && countDelta > 0)
                {
                    index += countDelta;
                }
            }
            else if (item != null)
            {
                index = FindNearestOccurrence(items, item, index);
            }

            index = Math.Min(index, items.Count - 1);
            return new ListScrollAnchor(index, Offset, items[index]);
        }

        static int FindNearestOccurrence(IReadOnlyList<ObservableObject> items, ObservableObject item, int index)
        {
            var center = Math.Min(index, items.Count - 1);
            for (var distance = 0; distance <= Math.Max(center, items.Count - 1 - center); distance++)
            {
                var before = center - distance;
                var after = center + distance;
                if (before >= 0 && ReferenceEquals(items[before], item))
                {
                    return before;
                }
                if (distance != 0 && after < items.Count && ReferenceEquals(items[after], item))
                {
                    return after;
                }
            }
            return center;
        }
    }
}
