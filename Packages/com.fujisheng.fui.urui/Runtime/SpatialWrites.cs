using System.Collections.Generic;
using System.Runtime.CompilerServices;
using URUI.Documents;
using Element = global::URUI.Elements.Element;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 只登记尚未落实的空间写入，让子世界坐标等待受控祖先稳定。
    /// </summary>
    internal sealed class SpatialWrites
    {
        static readonly ConditionalWeakTable<Document, SpatialWrites> contexts = new ConditionalWeakTable<Document, SpatialWrites>();
        readonly Document document;
        readonly Dictionary<Element, HashSet<ElementAdapter>> pending = new Dictionary<Element, HashSet<ElementAdapter>>();
        readonly Stack<HashSet<ElementAdapter>> reusable = new Stack<HashSet<ElementAdapter>>();
        bool subscribed;

        SpatialWrites(Document document)
        {
            this.document = document;
        }
        internal static SpatialWrites For(Document document) => contexts.GetValue(document, value => new SpatialWrites(value));

        internal void Add(ElementAdapter owner)
        {
            if (!pending.TryGetValue(owner.Element, out var writers))
            {
                writers = reusable.Count == 0 ? new HashSet<ElementAdapter>() : reusable.Pop();
                pending.Add(owner.Element, writers);
            }
            writers.Add(owner);
            if (!subscribed)
            {
                document.Unloaded += Clear;
                subscribed = true;
            }
        }

        internal void Remove(ElementAdapter owner)
        {
            if (owner.Element != null && pending.TryGetValue(owner.Element, out var writers))
            {
                writers.Remove(owner);
                if (writers.Count == 0)
                {
                    pending.Remove(owner.Element);
                    reusable.Push(writers);
                }
            }
        }

        internal bool HasPendingAncestor(Element element)
        {
            for (var parent = element.Parent; parent != null; parent = parent.Parent)
            {
                if (pending.ContainsKey(parent))
                {
                    return true;
                }
            }
            return false;
        }

        void Clear(Document owner)
        {
            var writers = new HashSet<ElementAdapter>();
            foreach (var group in pending.Values)
            {
                writers.UnionWith(group);
            }
            pending.Clear();
            reusable.Clear();
            document.Unloaded -= Clear;
            subscribed = false;
            foreach (var writer in writers)
            {
                writer.CancelPendingSpatial(owner);
            }
        }
    }
}
