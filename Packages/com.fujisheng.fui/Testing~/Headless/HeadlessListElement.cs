using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Presentation;
using FUI.Rendering;

namespace FUI.Testing
{
    /// <summary>Owns one actual generated item projection and its managed view.</summary>
    public sealed class HeadlessListRow : IDisposable
    {
        readonly ProjectionHandle<ObservableObject> projection;
        internal HeadlessListRow(ObservableObject item, HeadlessView view, bool visible)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            View = view ?? throw new ArgumentNullException(nameof(view));
            if (view.Destroyed || view.HasRowOwner)
            { throw new InvalidOperationException("Row factory must create an independent live view."); }
            view.HasRowOwner = true;
            try
            {
                projection = Projection.Create(view, item);
                if (visible) { projection.Activate(); }
            }
            catch (Exception failure)
            {
                var cleanup = HeadlessCleanup.Collect(new Action[] { () => projection?.Dispose(), view.Destroy });
                if (cleanup != null)
                { throw new AggregateException("Failed to create list row and release its view.", failure, cleanup); }
                throw;
            }
        }
        public ObservableObject Item { get; }
        public HeadlessView View { get; }
        internal void SetVisibility(bool visible)
        {
            if (visible) { projection.Activate(); }
            else { projection.Deactivate(); }
        }
        public void Dispose() => projection.Dispose();
    }

    /// <summary>Observes generated IListView delivery using real managed item projections.</summary>
    public sealed class HeadlessListElement : HeadlessElement, IListView, IVisibilityElement, IScrollPositionElement
    {
        readonly Func<ObservableObject, HeadlessView> createRow;
        readonly List<HeadlessListRow> rows = new List<HeadlessListRow>();
        bool visible = true;
        bool ownerVisible = true;
        public HeadlessListElement(string name, Func<ObservableObject, HeadlessView> createRow) : base(name)
        {
            this.createRow = createRow ?? throw new ArgumentNullException(nameof(createRow));
            Rows = rows.AsReadOnly();
            ItemsSource.OnValueChanged += OnItemsChanged;
            Visible.OnValueChanged += (oldValue,newValue) => SetVisibility(ownerVisible);
        }
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource { get; }
            = new BindableProperty<IReadOnlyObservableList<ObservableObject>>();
        public IReadOnlyList<HeadlessListRow> Rows { get; }
        public BindableProperty<bool> Visible { get; } = new BindableProperty<bool>(true);
        public BindableProperty<ScrollPoint> NormalizedScroll { get; } = new BindableProperty<ScrollPoint>(new ScrollPoint(0f,1f));
        public void InputScroll(ScrollPoint position) { if (CanInput && visible) { NormalizedScroll.Value=position; } }
        /// <summary>Reports each committed refresh, including unchanged collection delivery.</summary>
        public event Action RowsChanged;
        void OnItemsChanged(IReadOnlyObservableList<ObservableObject> oldValue, IReadOnlyObservableList<ObservableObject> newValue) => Refresh();
        void Refresh()
        {
            if (IsDisposed) { return; }
            var next = new List<HeadlessListRow>();
            var added = new List<HeadlessListRow>();
            var used = new bool[rows.Count];
            try
            {
                if (ItemsSource.Value != null)
                {
                    foreach (var item in ItemsSource.Value)
                    {
                        if (item == null) { throw new InvalidOperationException("Null list item."); }
                        HeadlessListRow match = null;
                        for (var i = 0; i < rows.Count; i++)
                        {
                            if (!used[i] && ReferenceEquals(rows[i].Item, item))
                            { used[i] = true; match = rows[i]; break; }
                        }
                        if (match == null)
                        {
                            match = new HeadlessListRow(item, createRow(item), visible);
                            match.View.Interactable = Owner?.Interactable ?? true;
                            added.Add(match);
                        }
                        next.Add(match);
                    }
                }
                foreach (var row in next) { row.SetVisibility(visible); }
            }
            catch (Exception failure)
            {
                var cleanup = DisposeRows(added);
                if (cleanup != null)
                { throw new AggregateException("Failed to refresh list and release new rows.", failure, cleanup); }
                throw;
            }
            var removed = new List<HeadlessListRow>();
            for (var i = 0; i < rows.Count; i++) { if (!used[i]) { removed.Add(rows[i]); } }
            rows.Clear();
            rows.AddRange(next);
            var removalFailure = DisposeRows(removed);
            if (removalFailure != null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(removalFailure).Throw(); }
            RowsChanged?.Invoke();
        }
        internal override void SetVisibility(bool value)
        {
            ownerVisible = value;
            visible = value && Visible.Value;
            if (visible) { Refresh(); }
            else { foreach (var row in rows) { row.SetVisibility(false); } }
        }
        internal override void SetInteractable(bool value)
        { foreach (var row in rows) { row.View.Interactable = value; } }
        public void OnAdd(object sender, int? index, object itemData) => Refresh();
        public void OnRemove(object sender, int? index, object item) => Refresh();
        public void OnReplace(object sender, int? index, object oldItem, object newItem) => Refresh();
        public void OnUpdate(object sender) => Refresh();
        static Exception DisposeRows(IEnumerable<HeadlessListRow> source)
        {
            var actions = new List<Action>();
            foreach (var row in source) { actions.Add(row.Dispose); }
            return HeadlessCleanup.Collect(actions);
        }
        protected override void OnDispose()
        {
            var owned = rows.ToArray();
            rows.Clear();
            RowsChanged = null;
            ItemsSource.Dispose();
            Visible.Dispose();
            NormalizedScroll.Dispose();
            var failure = DisposeRows(owned);
            if (failure != null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
        }
    }
}
