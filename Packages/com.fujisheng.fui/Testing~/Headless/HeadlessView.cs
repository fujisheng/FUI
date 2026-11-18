using System;
using System.Collections.Generic;
using FUI.Rendering;

namespace FUI.Testing
{
    /// <summary>Owns managed testing elements with strict exact-name lookup.</summary>
    public sealed class HeadlessView : IView
    {
        readonly Dictionary<string, IElement> byName = new Dictionary<string, IElement>(StringComparer.Ordinal);
        public HeadlessView(string name, params IElement[] elements)
        {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("View name is required.", nameof(name)); }
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            Name = name;
            foreach (var element in elements)
            {
                if (element == null) { throw new ArgumentException("Null element.", nameof(elements)); }
                if (byName.ContainsKey(element.Name)) { throw new ArgumentException("Duplicate element: " + element.Name, nameof(elements)); }
                byName.Add(element.Name, element);
            }
            foreach (var element in elements)
            {
                if (element is HeadlessElement managed && managed.Owner != null)
                { throw new ArgumentException("Element already belongs to a view: " + element.Name, nameof(elements)); }
            }
            Elements = Array.AsReadOnly((IElement[])elements.Clone());
            foreach (var element in Elements)
            {
                if (element is HeadlessElement managed) { managed.Owner = this; managed.SetVisibility(false); }
            }
        }
        public string Name { get; }
        public int Layer { get; set; }
        public int Order { get; set; }
        bool visible;
        public bool Visible
        {
            get => visible;
            set
            {
                if (Destroyed && value) { throw new ObjectDisposedException(Name); }
                if (visible == value) { return; }
                visible = value;
                try
                {
                    foreach (var element in Elements)
                    { if (element is HeadlessElement managed) { managed.SetVisibility(value); } }
                }
                catch
                {
                    visible = false;
                    foreach (var element in Elements)
                    { if (element is HeadlessElement managed) { managed.SetVisibility(false); } }
                    throw;
                }
            }
        }
        bool interactable = true;
        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                foreach (var element in Elements)
                { if (element is HeadlessElement managed) { managed.SetInteractable(value); } }
            }
        }
        public bool Destroyed { get; private set; }
        internal bool HasRowOwner { get; set; }
        internal bool Initialized { get; private set; }
        public IReadOnlyList<IElement> Elements { get; }
        public T GetElement<T>(string path) where T : IElement => (T)GetElement(path, typeof(T));
        public IElement GetElement(string path, Type elementType)
        {
            if (elementType == null) { throw new ArgumentNullException(nameof(elementType)); }
            if (path == null || !byName.TryGetValue(path, out var element))
            { throw new InvalidOperationException("Missing required element: " + path); }
            if (!elementType.IsInstanceOfType(element))
            { throw new InvalidOperationException("Element type mismatch: " + path); }
            return element;
        }
        public void Initialize()
        {
            if (Destroyed) { throw new ObjectDisposedException(Name); }
            Initialized = true;
        }
        public void Destroy()
        {
            if (Destroyed) { return; }
            Destroyed = true;
            var actions = new List<Action> { () => Visible = false };
            foreach (var element in Elements)
            { if (element is IDisposable owned) { actions.Add(owned.Dispose); } }
            var failure = HeadlessCleanup.Collect(actions);
            if (failure != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
        }
    }
}
