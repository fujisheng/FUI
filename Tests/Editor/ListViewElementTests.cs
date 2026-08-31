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
            element.Items.Value = list;
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
            element.Items.Value = list;
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
            var listObject = new GameObject("Items", typeof(RectTransform));
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
            element.Items.Value = list;
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
            var listObject = new GameObject("Items", typeof(RectTransform));
            listObject.transform.SetParent(root.transform, false);
            var element = listObject.AddComponent<RecyclingListElement>();
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(listObject.transform, false);
            var runtimeView = (IView)view;
            runtimeView.Initialize();

            var first = new ItemViewModel("first");
            var replacement = new AlternateItemViewModel("replacement");
            var list = new ObservableList<ObservableObject>(new ObservableObject[] { first });
            element.Items.Value = list;
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
            element.Items.Value = list;
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
            Assert.That(element.Items.Value, Is.Null);
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
            element.Items.Value = list;
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
