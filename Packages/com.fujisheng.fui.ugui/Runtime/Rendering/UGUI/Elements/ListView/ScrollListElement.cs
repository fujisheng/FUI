using FUI.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>
    /// 基于 ScrollRect 的列表视图元素。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("FUI/Elements/Scroll List")]
    public class ScrollListElement : RecyclingListElement, IScrollPositionElement, IScrollableListView
    {
        bool isUpdatingScrollState;
        bool arranging;
        LayoutGroup authoredLayout;
        GridLayoutGroup grid;
        HorizontalOrVerticalLayoutGroup line;
        RectTransform prototype;
        Vector2 cellSize;
        Vector2 spacing;
        bool horizontal;
        FixedListLayout layout;
        float viewportExtent;
        int first;
        int end;
        int arrangedCount;
        float arrangedWidth = -1;
        IReadOnlyObservableList<ObservableObject> arrangedSource;
        ItemScrollRequest? pendingScroll;
        ListScrollAnchor? dataAnchor;
        float dataAnchorScrollOffset;
        protected ScrollRect scrollRect;

        /// <summary>ScrollRect 的归一化位置。</summary>
        public BindableProperty<Vector2> NormalizedPosition { get; private set; }

        public BindableProperty<ScrollPoint> NormalizedScroll { get; private set; }

        /// <summary>Content 的锚点位置。</summary>
        public BindableProperty<Vector2> ScrollPosition { get; private set; }

        /// <summary>当前滚动速度。</summary>
        public BindableProperty<Vector2> Velocity { get; private set; }

        /// <summary>是否允许水平滚动。</summary>
        public BindableProperty<bool> Horizontal { get; private set; }

        /// <summary>是否允许垂直滚动。</summary>
        public BindableProperty<bool> Vertical { get; private set; }

        /// <summary>真实 ScrollRect 输入产生的位置变更事件。</summary>
        public Command<Vector2> OnValueChanged { get; private set; }
        public BindableProperty<ItemScrollRequest?> ScrollRequest { get; private set; }

        protected override void OnInitialize()
        {
            scrollRect = GetComponent<ScrollRect>();
            base.OnInitialize();
            ConfigureLayout();
            ScrollRequest = CreateProperty<ItemScrollRequest?>(null, (_, next) =>
            {
                pendingScroll = next;
                RefreshWindow(false);
            });
            NormalizedPosition = CreateProperty(scrollRect.normalizedPosition, OnNormalizedPositionChanged);
            var normalized = NormalizedPosition.Value;
            NormalizedScroll = CreateProperty(new ScrollPoint(normalized.x,normalized.y), (oldValue,newValue) =>
                NormalizedPosition.Value = new Vector2(newValue.x,newValue.y));
            ValueChangedHandler<Vector2> onNormalizedChanged = (oldValue,newValue) =>
                NormalizedScroll.Value = new ScrollPoint(newValue.x,newValue.y);
            NormalizedPosition.OnValueChanged += onNormalizedChanged;
            TrackCleanup(() => NormalizedPosition.OnValueChanged -= onNormalizedChanged);
            ScrollPosition = CreateProperty(GetContentAnchoredPosition(), OnScrollPositionChanged);
            Velocity = CreateProperty(scrollRect.velocity, (oldValue, newValue) => scrollRect.velocity = newValue);
            Horizontal = CreateProperty(scrollRect.horizontal, (oldValue, newValue) => scrollRect.horizontal = newValue);
            Vertical = CreateProperty(scrollRect.vertical, (oldValue, newValue) => scrollRect.vertical = newValue);
            OnValueChanged = Track(new Command<Vector2>());
            scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
            TrackCleanup(() => scrollRect?.onValueChanged.RemoveListener(OnScrollRectValueChanged));
        }

        void ConfigureLayout()
        {
            var content = scrollRect.content;
            prototype = itemPrefab.transform as RectTransform;
            if (content == null || prototype == null || scrollRect.horizontal == scrollRect.vertical)
            {
                return;
            }

            horizontal = scrollRect.horizontal;
            grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null && !horizontal &&
                grid.startAxis == GridLayoutGroup.Axis.Horizontal &&
                grid.startCorner == GridLayoutGroup.Corner.UpperLeft &&
                grid.constraint != GridLayoutGroup.Constraint.FixedRowCount &&
                grid.cellSize.x > 0 && grid.cellSize.y > 0)
            {
                authoredLayout = grid;
            }
            else
            {
                grid = null;
                line = horizontal ? content.GetComponent<HorizontalLayoutGroup>()
                    : content.GetComponent<VerticalLayoutGroup>();
                if (line == null || line.reverseArrangement || line.childScaleWidth || line.childScaleHeight ||
                    line.childControlWidth || line.childControlHeight || line.childForceExpandWidth ||
                    line.childForceExpandHeight || prototype.rect.width <= 0 || prototype.rect.height <= 0 ||
                    line.spacing < 0)
                {
                    line = null;
                    return;
                }
                authoredLayout = line;
            }

            var group = authoredLayout;
            var groupEnabled = group.enabled;
            var fitter = content.GetComponent<ContentSizeFitter>();
            var fitterEnabled = fitter != null && fitter.enabled;
            group.enabled = false;
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            TrackCleanup(() =>
            {
                if (group != null)
                {
                    group.enabled = groupEnabled;
                }
                if (fitter != null)
                {
                    fitter.enabled = fitterEnabled;
                }
                authoredLayout = null;
                grid = null;
                line = null;
                pendingScroll = null;
                arrangedSource = null;
                arrangedCount = 0;
                dataAnchor = null;
                arrangedWidth = -1;
            });
        }

        protected override void SetParent(int index, GameObject item)
        {
            if (item.transform.parent != scrollRect.content)
            {
                item.transform.SetParent(scrollRect.content, authoredLayout == null);
            }
            item.transform.SetSiblingIndex(index);
        }

        RectTransform Viewport => scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;

        protected override void OnAdd(int index, ObservableObject item)
        {
            if (authoredLayout == null)
            {
                base.OnAdd(index, item);
                return;
            }
            RefreshWindow(true, index, 1);
        }

        protected override void OnRemove(int index, ObservableObject item)
        {
            if (authoredLayout == null)
            {
                base.OnRemove(index, item);
                return;
            }
            RefreshWindow(true, index, -1);
        }

        protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
        {
            if (authoredLayout == null)
            {
                base.OnReplace(index, oldItem, newItem);
                return;
            }
            RefreshWindow(true, index, 0);
        }

        protected override void OnUpdate()
        {
            if (authoredLayout == null)
            {
                base.OnUpdate();
                return;
            }
            RefreshWindow(true);
        }

        protected override void OnReset()
        {
            pendingScroll = null;
            if (authoredLayout == null)
            {
                base.OnReset();
                return;
            }
            RefreshWindow(true);
        }

        public override bool TryGetBoundItemViewModel(int index, out ObservableObject itemViewModel)
        {
            itemViewModel = null;
            var source = ItemsSource?.Value;
            if (source == null || index < 0 || index >= source.Count ||
                !base.TryGetBoundItemViewModel(GetRealizedSlot(index), out var model) ||
                !ReferenceEquals(model, source[index]))
            {
                return false;
            }
            itemViewModel = model;
            return true;
        }

        protected override int GetRealizedSlot(int index) => authoredLayout == null ? index : index - first;

        void OnRectTransformDimensionsChange()
        {
            if (scrollRect != null && ItemsSource != null)
            {
                RefreshWindow(false);
            }
        }

        void RefreshWindow(bool dataChanged, int? changedIndex = null, int countDelta = 0)
        {
            if (arranging || scrollRect?.content == null)
            {
                return;
            }
            var source = ItemsSource?.Value;
            var sourceChanged = !ReferenceEquals(source, arrangedSource);
            if (sourceChanged)
            {
                pendingScroll = null;
                arrangedSource = source;
            }
            if (authoredLayout == null)
            {
                RevealRealizedItem();
                return;
            }

            arranging = true;
            try
            {
                var padding = authoredLayout.padding;
                var crossExtent = Cross(scrollRect.content.rect.size);
                var height = Main(Viewport.rect.size);
                var cell = grid != null ? grid.cellSize : prototype.rect.size;
                var gap = grid != null ? grid.spacing : horizontal ? new Vector2(line.spacing, 0) : new Vector2(0, line.spacing);
                var across = grid == null ? 1 : grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                    ? grid.constraintCount
                    : Mathf.FloorToInt((crossExtent - padding.horizontal + gap.x + .001f) / Mathf.Max(1, cell.x + gap.x));
                var geometryChanged = arrangedWidth != crossExtent || viewportExtent != height || cellSize != cell || spacing != gap;
                var previousLayout = layout;
                var previousViewport = viewportExtent;
                arrangedWidth = crossExtent;
                viewportExtent = height;
                cellSize = cell;
                spacing = gap;
                layout = new FixedListLayout(Mathf.Max(1, across), Main(cell), Mathf.Max(0, Main(gap)),
                    horizontal ? padding.left : padding.top, horizontal ? padding.right : padding.bottom);
                var count = source?.Count ?? 0;
                var content = scrollRect.content;
                var previousOffset = GetOffset();
                var capacityChanged = previousLayout.ItemsPerLine != layout.ItemsPerLine;
                if (sourceChanged || count == 0 || !Mathf.Approximately(dataAnchorScrollOffset, previousOffset))
                {
                    dataAnchor = null;
                }
                if (dataChanged && !sourceChanged && count > 0 && previousOffset > 0 && !dataAnchor.HasValue)
                {
                    var anchor = previousLayout.GetAnchor(arrangedCount, previousOffset, previousViewport);
                    if (anchor.Index >= 0)
                    {
                        var slot = anchor.Index - first;
                        var model = slot >= 0 && slot < ItemInstances.Count ? ItemInstances[slot].ViewModel : null;
                        dataAnchor = new ListScrollAnchor(anchor.Index, anchor.Offset, model);
                    }
                }
                var retainData = dataChanged && dataAnchor.HasValue;
                if (retainData)
                {
                    var moved = dataAnchor.Value.Move(source, changedIndex, countDelta);
                    dataAnchor = moved;
                    dataAnchorScrollOffset = layout.GetAnchoredOffset(count, moved.Index, moved.Offset, height);
                }
                arrangedCount = count;
                var size = layout.GetExtent(count);
                var sizeChanged = !Mathf.Approximately(Main(content.rect.size), size);
                if (sizeChanged)
                {
                    content.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, size);
                }
                if (retainData || capacityChanged || sizeChanged)
                {
                    var offset = retainData ? dataAnchorScrollOffset : capacityChanged
                        ? layout.RetainAnchor(previousLayout, count, previousOffset, previousViewport, height)
                        : Mathf.Clamp(previousOffset, 0, Mathf.Max(0, size - height));
                    SetOffset(offset);
                }

                if (height > 0 && pendingScroll.HasValue)
                {
                    var index = pendingScroll.Value.Index;
                    pendingScroll = null;
                    if (index >= 0 && index < count)
                    {
                        dataAnchor = null;
                        scrollRect.StopMovement();
                        SetOffset(layout.GetScrollOffset(count, index, GetOffset(), height));
                    }
                }
                var next = layout.GetRange(count, GetOffset(), height);
                if (dataChanged || geometryChanged || next.Start != first || next.End != end)
                {
                    first = next.Start;
                    end = next.End;
                    RealizeRange(first, end, dataChanged);
                }
            }
            finally
            {
                arranging = false;
            }
        }

        float Main(Vector2 size) => horizontal ? size.x : size.y;
        float Cross(Vector2 size) => horizontal ? size.y : size.x;

        void RevealRealizedItem()
        {
            if (!pendingScroll.HasValue || Viewport.rect.width <= 0 || Viewport.rect.height <= 0)
            {
                return;
            }
            var index = pendingScroll.Value.Index;
            pendingScroll = null;
            if (!TryGetRealizedItem(index, out var realized) || realized is not View view)
            {
                return;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(Viewport, view.transform);
            var viewportRect = Viewport.rect;
            var delta = new Vector2(
                scrollRect.horizontal ? Mathf.Min(0, viewportRect.xMax - bounds.max.x) + Mathf.Max(0, viewportRect.xMin - bounds.min.x) : 0,
                scrollRect.vertical ? Mathf.Min(0, viewportRect.yMax - bounds.max.y) + Mathf.Max(0, viewportRect.yMin - bounds.min.y) : 0);
            scrollRect.StopMovement();
            MoveContent(delta);
        }

        float GetOffset()
        {
            var content = scrollRect.content;
            var origin = Viewport.InverseTransformPoint(content.TransformPoint(new Vector3(content.rect.xMin, content.rect.yMax)));
            return horizontal ? Viewport.rect.xMin - origin.x : origin.y - Viewport.rect.yMax;
        }

        void SetOffset(float offset)
        {
            var difference = offset - GetOffset();
            MoveContent(horizontal ? new Vector2(-difference, 0) : new Vector2(0, difference));
        }

        void MoveContent(Vector2 delta)
        {
            if (delta == Vector2.zero)
            {
                return;
            }
            var content = scrollRect.content;
            var difference = (Vector2)content.parent.InverseTransformVector(Viewport.TransformVector(delta));
            content.anchoredPosition += difference;
        }

        private protected override void PlaceRealizedItem(int index, int position, GameObject item)
        {
            SetParent(position, item);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = Vector2.up;
            rect.sizeDelta = cellSize;
            var padding = authoredLayout.padding;
            var cellsAcross = Mathf.Min(layout.ItemsPerLine, ItemsSource.Value.Count);
            var crossCellsExtent = cellsAcross * Cross(cellSize) + Mathf.Max(0, cellsAcross - 1) * Cross(spacing);
            var crossAlignment = horizontal ? (int)authoredLayout.childAlignment / 3 : (int)authoredLayout.childAlignment % 3;
            var slack = arrangedWidth - (horizontal ? padding.vertical : padding.horizontal) - crossCellsExtent;
            var crossOffset = (horizontal ? padding.top : padding.left) + slack * crossAlignment * .5f
                + index % layout.ItemsPerLine * (Cross(cellSize) + Cross(spacing));
            var mainOffset = layout.GetItemOffset(index);
            var x = horizontal ? mainOffset : crossOffset;
            var y = horizontal ? crossOffset : mainOffset;
            rect.anchoredPosition = new Vector2(x + cellSize.x * rect.pivot.x, -y - cellSize.y * (1 - rect.pivot.y));
        }

        protected virtual void OnScrollStateChanged()
        {
        }

        void OnNormalizedPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (isUpdatingScrollState || scrollRect == null)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                scrollRect.normalizedPosition = newValue;
                ScrollPosition.SetValue(GetContentAnchoredPosition());
                RefreshWindow(false);
                OnScrollStateChanged();
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        void OnScrollPositionChanged(Vector2 oldValue, Vector2 newValue)
        {
            if (isUpdatingScrollState || scrollRect?.content == null)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                scrollRect.content.anchoredPosition = newValue;
                NormalizedPosition.SetValue(scrollRect.normalizedPosition);
                RefreshWindow(false);
                OnScrollStateChanged();
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        void OnScrollRectValueChanged(Vector2 value)
        {
            if (isUpdatingScrollState)
            {
                return;
            }

            isUpdatingScrollState = true;
            try
            {
                NormalizedPosition.SetValue(value);
                ScrollPosition.SetValue(GetContentAnchoredPosition());
                Velocity.SetValue(scrollRect.velocity);
                RefreshWindow(false);
                OnScrollStateChanged();
                OnValueChanged.Invoke(value);
            }
            finally
            {
                isUpdatingScrollState = false;
            }
        }

        protected Vector2 GetContentAnchoredPosition()
        {
            return scrollRect?.content == null ? Vector2.zero : scrollRect.content.anchoredPosition;
        }

        protected override GameObject ResolveItemPrefab(GameObject currentItemPrefab)
        {
            if (currentItemPrefab != null)
            {
                return currentItemPrefab;
            }

            if (scrollRect?.content != null && scrollRect.content.childCount > 0)
            {
                return scrollRect.content.GetChild(0).gameObject;
            }

            return base.ResolveItemPrefab(currentItemPrefab);
        }
    }
}
