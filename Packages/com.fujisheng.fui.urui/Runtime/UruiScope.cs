using System;
using System.Collections.Generic;
using global::URUI.Documents;
using global::URUI.Elements;

namespace FUI.Rendering.URUI
{
    /// <summary>顶层 Scope 拥有独立文档；页内 Scope 仅拥有自己的节点。</summary>
    public sealed class UruiScope
    {
        Element root;
        readonly Action release;
        readonly UruiDisplayState displayState = new();
        bool destroyed, visible = true, modal;
        int layer, order;
        public Document Document { get; }
        public Element Root
        {
            get {
                if (!destroyed && OwnsDocument && Document && Document.IsLoaded) root = Document.Body;
                return root;
            }
        }
        internal bool OwnsDocument => release != null;
        public UruiScope(Element root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root)); Document = root.Document;
        }
        internal UruiScope(Document document, int layer, int order, bool modal, Action release)
        {
            Document = document; root = document.Body; this.layer = layer; this.order = order; this.modal = modal;
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }
        public void LoadFragment(string html)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(UruiScope));
            if (OwnsDocument) throw new InvalidOperationException("顶层 Scope 使用完整 HTML 文档；片段只能载入页内 Scope。");
            root.SetInnerHtml(html, Document.SourceUrl);
        }
        /// <summary>在当前页面属性作用域中实例化已编译的片段。</summary>
        public void LoadFragment(global::URUI.Resources.CompiledTemplate template)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(UruiScope));
            if (OwnsDocument) throw new InvalidOperationException("编译片段只能载入页内 Scope。");
            root.SetInnerHtml(template);
        }
        void ApplyPresentation() { if (Document) Document.SetPresentation(layer, order, visible, modal); }
        internal void SetLayer(int value) { layer = value; if (OwnsDocument) ApplyPresentation(); }
        internal void SetOrder(int value)
        {
            order = value;
            if (OwnsDocument) ApplyPresentation();
            else if (root.IsValid) root.Style.SetNumber("z-index", value);
        }
        public void SetVisible(bool value)
        {
            if (destroyed) return;
            visible = value;
            if (OwnsDocument) ApplyPresentation();
            else if (root.IsValid) {
                displayState.Apply(root, value);
                if (!value) Document.Surface.Input.CancelPointers(root);
            }
        }
        public void SetInteractable(bool value) { if (!destroyed && Root.IsValid) Root.SetEnabled(value); }
        public Element Find(string name) => Root.Q(name);
        public IReadOnlyList<Element> FindAll(string name) => Root.Query(name);
        public void Destroy()
        {
            if (destroyed) return;
            destroyed = true;
            if (OwnsDocument) release();
            else if (root.IsValid) root.Destroy();
        }
    }
}
