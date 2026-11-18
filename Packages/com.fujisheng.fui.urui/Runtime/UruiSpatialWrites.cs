using System.Collections.Generic;
using System.Runtime.CompilerServices;
using URUI.Documents;
using Element = global::URUI.Elements.Element;

namespace FUI.Rendering.URUI
{
    /// <summary>只登记尚未落实的空间写入，让子世界坐标等待受控祖先稳定。</summary>
    internal sealed class UruiSpatialWrites
    {
        static readonly ConditionalWeakTable<Document, UruiSpatialWrites> contexts = new ConditionalWeakTable<Document, UruiSpatialWrites>();
        readonly Document document;
        readonly Dictionary<Element, HashSet<UruiElement>> pending = new Dictionary<Element, HashSet<UruiElement>>();
        readonly Stack<HashSet<UruiElement>> reusable = new Stack<HashSet<UruiElement>>();
        bool subscribed;

        UruiSpatialWrites(Document document) { this.document = document; }
        internal static UruiSpatialWrites For(Document document) => contexts.GetValue(document, value => new UruiSpatialWrites(value));

        internal void Add(UruiElement owner)
        {
            if (!pending.TryGetValue(owner.Element, out var writers))
            {
                writers = reusable.Count == 0 ? new HashSet<UruiElement>() : reusable.Pop();
                pending.Add(owner.Element, writers);
            }
            writers.Add(owner);
            if (!subscribed)
            {
                document.Unloaded += Clear;
                subscribed = true;
            }
        }

        internal void Remove(UruiElement owner)
        {
            if (owner.Element != null && pending.TryGetValue(owner.Element, out var writers))
            {
                writers.Remove(owner);
                if (writers.Count == 0) { pending.Remove(owner.Element); reusable.Push(writers); }
            }
        }

        internal bool HasPendingAncestor(Element element)
        {
            for (var parent = element.Parent; parent != null; parent = parent.Parent)
            {
                if (pending.ContainsKey(parent)) { return true; }
            }
            return false;
        }

        void Clear(Document owner)
        {
            var writers = new HashSet<UruiElement>();
            foreach (var group in pending.Values) { writers.UnionWith(group); }
            pending.Clear();
            reusable.Clear();
            document.Unloaded -= Clear;
            subscribed = false;
            foreach (var writer in writers) { writer.CancelPendingSpatial(owner); }
        }
    }
}
