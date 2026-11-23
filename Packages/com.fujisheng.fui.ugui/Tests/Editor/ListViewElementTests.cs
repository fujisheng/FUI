using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class ListElementTests
    {
        sealed class ItemViewModel : ObservableObject
        {
            public string Id { get; }

            public ItemViewModel(string id)
            {
                Id = id;
            }
        }

        sealed class ItemBindingContext : BindingContext<ItemViewModel>
        {
            public ItemBindingContext(IView view, ItemViewModel viewModel) : base(view, viewModel)
            {
            }
        }

        sealed class AlternateItemViewModel : ObservableObject
        {
            public string Id { get; }

            public AlternateItemViewModel(string id)
            {
                Id = id;
            }
        }

        sealed class AlternateItemBindingContext : BindingContext<AlternateItemViewModel>
        {
            public AlternateItemBindingContext(IView view, AlternateItemViewModel viewModel) : base(view, viewModel)
            {
            }
        }

        sealed class ListProbe : ListElement
        {
            public int AddCount { get; private set; }
            public int RemoveCount { get; private set; }
            public int ReplaceCount { get; private set; }
            public int UpdateCount { get; private set; }

            protected override void OnAdd(int index, ObservableObject item)
            {
                AddCount++;
            }

            protected override void OnRemove(int index, ObservableObject item)
            {
                RemoveCount++;
            }

            protected override void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem)
            {
                ReplaceCount++;
            }

            protected override void OnUpdate()
            {
                UpdateCount++;
            }

            protected override void OnReset()
            {
            }
        }

        sealed class ScrollListProbe : ScrollListElement
        {
            public int StateChangeCount { get; private set; }

            protected override void OnScrollStateChanged()
            {
                StateChangeCount++;
            }
        }

        GameObject root;

        [SetUp]
        public void SetUp()
        {
            GeneratedBindingRegistry.Register<ItemViewModel>(
                (view, viewModel) => new ItemBindingContext(view, viewModel),
                () => new EmptyPresenter());
            GeneratedBindingRegistry.Register<AlternateItemViewModel>(
                (view, viewModel) => new AlternateItemBindingContext(view, viewModel),
                () => new EmptyPresenter());
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AuthoredTemplateCollectionPreservesRowOrderAndReleasesClones()
        {
            root = new GameObject("Overlay", typeof(RectTransform));
            var view = (IView)root.AddComponent<View>();
            var template = new GameObject("Marker", typeof(RectTransform));
            template.transform.SetParent(root.transform, false);
            var prototype = template.AddComponent<ViewPrototypeElement>();
            view.Initialize();

            var items = new ObservableList<ItemViewModel> { new("first"), new("second") };
            BindingContextUtility.BindingList(prototype, null, items);
            prototype.ItemsSource.Value = items;
            var first = root.transform.GetChild(1);
            var second = root.transform.GetChild(2);
            Assert.IsFalse(template.activeSelf);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.IsTrue(second.gameObject.activeSelf);

            items.RemoveAt(0);
            Assert.IsFalse(first.gameObject.activeSelf);
            items.Add(new ItemViewModel("replacement"));
            Assert.AreSame(first, root.transform.GetChild(2));
            Assert.IsTrue(first.gameObject.activeSelf);

            items.Reverse();
            Assert.AreSame(first, root.transform.GetChild(1));
            Assert.AreSame(second, root.transform.GetChild(2));
            BindingContextUtility.UnbindingList(prototype, items);
            prototype.ItemsSource.Value = null;
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            view.Destroy();
        }

        [Test]
        public void ListElementKeepsIndexAndItemSelectionConsistentAcrossMutations()
        {
            root = new GameObject("ListProbeView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("ListProbe", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<ListProbe>();
            var first = new ItemViewModel("first");
            var second = new ItemViewModel("second");
            var replacement = new ItemViewModel("replacement");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first, second });
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            Assert.That(element.AnchorMin, Is.Not.Null);
            Assert.That(element.SizeDelta, Is.Not.Null);
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            element.SelectedIndex.Value = 1;
            Assert.That(element.SelectedItem.Value, Is.SameAs(second));

            element.SelectedItem.Value = first;
            Assert.That(element.SelectedIndex.Value, Is.Zero);

            list[0] = replacement;
            Assert.That(element.SelectedIndex.Value, Is.Zero);
            Assert.That(element.SelectedItem.Value, Is.SameAs(replacement));

            element.SelectedItem.Value = second;
            list.Remove(second);
            Assert.That(element.SelectedIndex.Value, Is.EqualTo(-1));
            Assert.That(element.SelectedItem.Value, Is.Null);
            Assert.That(element.ReplaceCount, Is.EqualTo(1));
            Assert.That(element.RemoveCount, Is.EqualTo(1));

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
        }

        [Test]
        public void ListElementDispatchesPreciseAndFullCollectionChanges()
        {
            root = new GameObject("ListProbeView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("ListProbe", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<ListProbe>();
            var list = new ObservableList<ObservableObject>();
            var runtimeView = (IView)view;
            runtimeView.Initialize();
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            list.Add(new ItemViewModel("one"));
            list[0] = new ItemViewModel("two");
            list.RemoveAt(0);
            list.Clear();

            Assert.That(element.AddCount, Is.EqualTo(1));
            Assert.That(element.ReplaceCount, Is.EqualTo(1));
            Assert.That(element.RemoveCount, Is.EqualTo(1));
            Assert.That(element.UpdateCount, Is.EqualTo(2));

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
        }

        [Test]
        public void RecyclingListReusesRemovedItemsAndPreservesDataOrder()
        {
            root = new GameObject("InventoryView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("ItemsSource", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<RecyclingListElement>();
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(listObject.transform, false);
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            var first = new ItemViewModel("first");
            var second = new ItemViewModel("second");
            var inserted = new ItemViewModel("inserted");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first, second });
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            AssertBoundItem(element, 0, first);
            AssertBoundItem(element, 1, second);
            Assert.That(CountActiveClones(listObject.transform, template), Is.EqualTo(2));

            list.RemoveAt(0);
            AssertBoundItem(element, 0, second);
            Assert.That(CountActiveClones(listObject.transform, template), Is.EqualTo(1));

            list.Insert(0, inserted);
            AssertBoundItem(element, 0, inserted);
            AssertBoundItem(element, 1, second);
            Assert.That(listObject.transform.childCount, Is.EqualTo(3));
            Assert.That(CountActiveClones(listObject.transform, template), Is.EqualTo(2));

            list.Clear();
            Assert.That(element.TryGetBoundItemViewModel(0, out _), Is.False);
            Assert.That(CountActiveClones(listObject.transform, template), Is.Zero);

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
        }

        [Test]
        public void RecyclingListRebuildsProjectionWhenItemViewModelTypeChanges()
        {
            root = new GameObject("TypedInventoryView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("ItemsSource", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<RecyclingListElement>();
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(listObject.transform, false);
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            var first = new ItemViewModel("first");
            var replacement = new AlternateItemViewModel("replacement");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first });
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            list[0] = replacement;

            AssertBoundItem(element, 0, replacement);
            Assert.That(CountActiveClones(listObject.transform, template), Is.EqualTo(1));

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
        }

        [Test]
        public void SlotListRemapsSlotsAfterAddAndRemoveAndReleasesProperties()
        {
            root = new GameObject("PartyView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("Slots", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<SlotListElement>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(listObject.transform, false);
            var firstSlot = new GameObject("SlotOne", typeof(RectTransform));
            firstSlot.transform.SetParent(content.transform, false);
            var secondSlot = new GameObject("SlotTwo", typeof(RectTransform));
            secondSlot.transform.SetParent(content.transform, false);
            var serializedElement = new SerializedObject(element);
            serializedElement.FindProperty("content").objectReferenceValue = content.transform;
            serializedElement.ApplyModifiedPropertiesWithoutUndo();
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            var first = new ItemViewModel("first");
            var second = new ItemViewModel("second");
            var inserted = new ItemViewModel("inserted");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first, second });
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            list.RemoveAt(0);
            AssertBoundItem(element, 0, second);
            Assert.That(firstSlot.activeSelf, Is.True);
            Assert.That(secondSlot.activeSelf, Is.False);

            list.Insert(0, inserted);
            AssertBoundItem(element, 0, inserted);
            AssertBoundItem(element, 1, second);
            Assert.That(firstSlot.activeSelf, Is.True);
            Assert.That(secondSlot.activeSelf, Is.True);

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
            Assert.That(element.ItemsSource.Value, Is.Null);
        }

        [Test]
        public void AnchoredListLimitsVisibleItemsToConfiguredCapacity()
        {
            root = new GameObject("AnchoredInventoryView", typeof(RectTransform));
            var view = root.AddComponent<View>();
            var listObject = new GameObject("AnchoredItems", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<AnchoredListElement>();
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(listObject.transform, false);
            var firstAnchor = new GameObject("FirstAnchor", typeof(RectTransform)).transform;
            firstAnchor.SetParent(listObject.transform, false);
            var secondAnchor = new GameObject("SecondAnchor", typeof(RectTransform)).transform;
            secondAnchor.SetParent(listObject.transform, false);

            var serializedElement = new SerializedObject(element);
            serializedElement.FindProperty("itemPrefab").objectReferenceValue = template;
            var anchors = serializedElement.FindProperty("anchors");
            anchors.arraySize = 2;
            anchors.GetArrayElementAtIndex(0).objectReferenceValue = firstAnchor;
            anchors.GetArrayElementAtIndex(1).objectReferenceValue = secondAnchor;
            serializedElement.ApplyModifiedPropertiesWithoutUndo();

            var runtimeView = (IView)view;
            runtimeView.Initialize();
            var first = new ItemViewModel("first");
            var second = new ItemViewModel("second");
            var hidden = new ItemViewModel("hidden");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first, second, hidden });
            element.ItemsSource.Value = list;
            BindingContextUtility.BindingList(element, null, list);

            Assert.That(element.Capacity, Is.EqualTo(2));
            AssertBoundItem(element, 0, first);
            AssertBoundItem(element, 1, second);
            Assert.That(element.TryGetBoundItemViewModel(2, out _), Is.False);
            Assert.That(firstAnchor.childCount, Is.EqualTo(1));
            Assert.That(secondAnchor.childCount, Is.EqualTo(1));

            BindingContextUtility.UnbindingList(element, list);
            runtimeView.Destroy();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScrollWindowKeepsItsCellsBoundedAndRevealsTheRequestedItem(bool horizontal)
        {
            root = new GameObject("ScrollGrid", typeof(RectTransform), typeof(ScrollRect));
            var viewport = (RectTransform)root.transform;
            viewport.sizeDelta = new Vector2(300, 100);
            var content = new GameObject("Content", typeof(RectTransform),
                horizontal ? typeof(HorizontalLayoutGroup) : typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = content.anchorMax = Vector2.up;
            content.pivot = Vector2.up;
            content.sizeDelta = new Vector2(300, 100);
            var prototype = new GameObject("Item", typeof(RectTransform));
            prototype.transform.SetParent(content, false);
            ((RectTransform)prototype.transform).sizeDelta = new Vector2(100, 20);
            if (horizontal)
            {
                var layout = content.GetComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = 5;
            }
            else
            {
                var layout = content.GetComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(100, 20);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 3;
            }
            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;

            var view = root.AddComponent<View>();
            var list = root.AddComponent<ScrollListElement>();
            ((IView)view).Initialize();
            var data = new ObservableList<ObservableObject>();
            for (var index = 0; index < 1000; index++)
            {
                data.Add(new ItemViewModel(index.ToString()));
            }
            list.ItemsSource.Value = data;
            Assert.That(content.childCount, Is.LessThanOrEqualTo(22));
            Assert.That(list.TryGetBoundItemViewModel(999, out _), Is.False);

            ((IScrollableListView)list).ScrollRequest.Value = new ItemScrollRequest(999, 1);

            Assert.That(list.TryGetBoundItemViewModel(999, out var item), Is.True);
            Assert.That(item, Is.SameAs(data[999]));
            Assert.That(content.childCount, Is.LessThanOrEqualTo(22));
            Assert.That(list.TryGetBoundItemViewModel(0, out _), Is.False);
            Assert.That(list.TryGetRealizedItem(0, out _), Is.False);

            list.ScrollRequest.Value = new ItemScrollRequest(0, 2);
            Assert.That(list.TryGetRealizedItem(0, out _), Is.True);
            Assert.That(list.TryGetRealizedItem(999, out _), Is.False);

            list.ScrollRequest.Value = new ItemScrollRequest(500, 3);
            var before = horizontal ? -content.anchoredPosition.x : content.anchoredPosition.y;
            var unit = horizontal ? 105 : 20;
            var across = horizontal ? 1 : 3;
            var firstVisible = Mathf.FloorToInt(before / unit) * across;
            Assert.That(list.TryGetRealizedItem(firstVisible, out var row), Is.True);
            for (var index = 0; index < 6; index++)
            {
                var newItem = new ItemViewModel("Inserted");
                data.Insert(0, newItem);
                ((IListView)list).OnAdd(data, 0, newItem);
            }
            var after = horizontal ? -content.anchoredPosition.x : content.anchoredPosition.y;
            Assert.That(after, Is.EqualTo(before + 6 / across * unit).Within(.1f));
            Assert.That(list.TryGetRealizedItem(firstVisible + 6, out var shifted), Is.True);
            Assert.That(shifted, Is.SameAs(row));
            Assert.That(content.childCount, Is.LessThanOrEqualTo(22));
        }

        [Test]
        public void ScrollListSynchronizesBindableAndExternalPositionsWithoutRecursion()
        {
            root = new GameObject("ScrollableView", typeof(RectTransform), typeof(ScrollRect));
            var view = root.AddComponent<View>();
            var element = root.AddComponent<ScrollListProbe>();
            var scrollRect = root.GetComponent<ScrollRect>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root.transform, false);
            scrollRect.content = content;
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(content, false);
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            element.ScrollPosition.Value = new Vector2(20f, 30f);
            Assert.That(content.anchoredPosition, Is.EqualTo(new Vector2(20f, 30f)));
            Assert.That(element.StateChangeCount, Is.EqualTo(1));

            content.anchoredPosition = new Vector2(40f, 80f);
            scrollRect.onValueChanged.Invoke(new Vector2(0.5f, 0.25f));
            Assert.That(element.NormalizedPosition.Value, Is.EqualTo(new Vector2(0.5f, 0.25f)));
            Assert.That(element.ScrollPosition.Value, Is.EqualTo(new Vector2(40f, 80f)));
            Assert.That(element.StateChangeCount, Is.EqualTo(2));

            runtimeView.Destroy();
        }

        static void AssertBoundItem(ListElement element, int index, ObservableObject expected)
        {
            Assert.That(element.TryGetBoundItemViewModel(index, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }

        static int CountActiveClones(Transform parent, GameObject template)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index).gameObject;
                if (child != template && child.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
