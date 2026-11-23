using System;
using FUI.Binding;

namespace FUI.Rendering
{
    /// <summary>Identifies one request to reveal an item in the current list data source.</summary>
    public readonly struct ItemScrollRequest : IEquatable<ItemScrollRequest>
    {
        public int Index { get; }
        public long Sequence { get; }

        public ItemScrollRequest(int index, long sequence)
        {
            Index = index;
            Sequence = sequence;
        }

        public bool Equals(ItemScrollRequest other)
        {
            return Index == other.Index && Sequence == other.Sequence;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemScrollRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Sequence.GetHashCode();
            }
        }
    }

    /// <summary>Receives ViewModel requests to reveal an item without exposing row views.</summary>
    public interface IScrollableListView : IListView
    {
        /// <summary>The next request to reveal an item. Null clears a pending request.</summary>
        BindableProperty<ItemScrollRequest?> ScrollRequest { get; }
    }
}
