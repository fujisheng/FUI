using FUI.Binding;
using FUI.Infrastructure.Binding;
using FUI.Rendering;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FUI.Tests
{
    public sealed class BindingPrimitivesTests
    {
        sealed class Item : ObservableObject, IComparable<Item>
        {
            public int Value { get; }

            public Item(int value)
            {
                Value = value;
            }

            public int CompareTo(Item other)
            {
                return other == null ? 1 : Value.CompareTo(other.Value);
            }
        }

        sealed class ListProbe : IListView
        {
            public string Name => "Probe";
            public IElement Parent => null;
            public BindableProperty<IReadOnlyObservableList<ObservableObject>> ItemsSource { get; }
                = new BindableProperty<IReadOnlyObservableList<ObservableObject>>();
            public int AddCount { get; private set; }
            public int RemoveCount { get; private set; }
            public int ReplaceCount { get; private set; }
            public int UpdateCount { get; private set; }

            public void OnAdd(object sender, int? index, object itemData) => AddCount++;
            public void OnRemove(object sender, int? index, object item) => RemoveCount++;
            public void OnReplace(object sender, int? index, object oldItem, object newItem) => ReplaceCount++;
            public void OnUpdate(object sender) => UpdateCount++;
        }

        [Test]
        public void BindablePropertyNotifiesOnlyWhenValueChanges()
        {
            var changes = new List<(int OldValue, int NewValue)>();
            var property = new BindableProperty<int>(3, (oldValue, newValue) => changes.Add((oldValue, newValue)));

            property.Value = 3;
            property.Value = 7;
            property.SetValue(9);

            Assert.That(property.GetValue(), Is.EqualTo(9));
            Assert.That(changes, Is.EqualTo(new[] { (3, 7), (7, 9) }));
            Assert.That(property.ToString(), Is.EqualTo("Value:9"));
        }

        [Test]
        public void NormalizedPropertyNotifiesCanonicalValueOnceAcrossTypedAndObjectWriters()
        {
            var changes = new List<float>();
            var property = new BindableProperty<float>(0, (_, value) => changes.Add(value), value => (float)Math.Round(value));
            property.Value = 2.6f;
            ((IWriteOnlyBindableProperty<float>)property).SetValue((object)3.4f);
            ((IWriteOnlyBindableProperty<float>)property).SetValue((object)4.6f);
            Assert.That(changes, Is.EqualTo(new[] { 3f, 5f }));
            Assert.That(property.Value, Is.EqualTo(5f));
        }

        [Test]
        public void NormalizerFailureLeavesStoredValueAndNotificationsUnchanged()
        {
            var notifications = 0;
            var property = new BindableProperty<int>(2, (_, value) => notifications++, value =>
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                return value;
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => property.Value = -1);
            Assert.That(property.Value, Is.EqualTo(2));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void BindablePropertyObjectSetterValidatesTypeAndSupportsNullReset()
        {
            var property = new BindableProperty<int>(5);
            var writer = (IWriteOnlyBindableProperty<int>)property;

            writer.SetValue((object)8);
            Assert.That(property.Value, Is.EqualTo(8));

            writer.SetValue((object)null);
            Assert.That(property.Value, Is.Zero);

            Assert.Throws<Exception>(() => writer.SetValue("invalid"));
        }

        [Test]
        public void DisposingBindablePropertyClearsValueAndListeners()
        {
            var notifications = 0;
            var property = new BindableProperty<string>("before", (oldValue, newValue) => notifications++);

            property.Dispose();
            property.Value = "after";

            Assert.That(property.Value, Is.EqualTo("after"));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void CommandRejectsDuplicateListenersAndSupportsRemovalDuringInvocation()
        {
            var command = new Command();
            var calls = new List<string>();
            Action removable = null;
            removable = () =>
            {
                calls.Add("removable");
                command.RemoveListener(removable);
            };
            Action persistent = () => calls.Add("persistent");

            command.AddListener(removable);
            command.AddListener(removable);
            command.AddListener(persistent);
            command.Invoke();
            command.Invoke();

            Assert.That(calls, Is.EqualTo(new[] { "persistent", "removable", "persistent" }));

            command.ClearListeners();
            command.Invoke();
            Assert.That(calls.Count, Is.EqualTo(3));
        }

        [Test]
        public void GenericCommandsForwardEveryArgument()
        {
            var total = 0;
            var one = new Command<int>();
            var two = new Command<int, int>();
            var three = new Command<int, int, int>();
            var four = new Command<int, int, int, int>();
            var five = new Command<int, int, int, int, int>();
            var six = new Command<int, int, int, int, int, int>();
            var seven = new Command<int, int, int, int, int, int, int>();
            one.AddListener(a => total += a);
            two.AddListener((a, b) => total += a + b);
            three.AddListener((a, b, c) => total += a + b + c);
            four.AddListener((a, b, c, d) => total += a + b + c + d);
            five.AddListener((a, b, c, d, e) => total += a + b + c + d + e);
            six.AddListener((a, b, c, d, e, f) => total += a + b + c + d + e + f);
            seven.AddListener((a, b, c, d, e, f, g) => total += a + b + c + d + e + f + g);

            one.Invoke(1);
            two.Invoke(1, 2);
            three.Invoke(1, 2, 3);
            four.Invoke(1, 2, 3, 4);
            five.Invoke(1, 2, 3, 4, 5);
            six.Invoke(1, 2, 3, 4, 5, 6);
            seven.Invoke(1, 2, 3, 4, 5, 6, 7);

            Assert.That(total, Is.EqualTo(84));
        }

        [Test]
        public void ObservableListReportsPreciseSingleItemMutations()
        {
            var list = new ObservableList<Item>();
            var events = new List<string>();
            list.CollectionAdd += (sender, index, item) => events.Add($"add:{index}:{((Item)item).Value}");
            list.CollectionRemove += (sender, index, item) => events.Add($"remove:{index}:{((Item)item).Value}");
            var first = new Item(1);
            var second = new Item(2);

            list.Add(first);
            list.Insert(0, second);
            Assert.That(list.Remove(new Item(99)), Is.False);
            Assert.That(list.Remove(first), Is.True);
            list.RemoveAt(0);

            Assert.That(events, Is.EqualTo(new[]
            {
                "add:0:1",
                "add:0:2",
                "remove:1:1",
                "remove:0:2",
            }));
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void ObservableListReportsBatchAndOrderChanges()
        {
            var list = new ObservableList<Item>();
            var addCount = 0;
            var removeCount = 0;
            var updateCount = 0;
            list.CollectionAdd += (sender, index, item) => addCount++;
            list.CollectionRemove += (sender, index, item) => removeCount++;
            list.CollectionUpdate += sender => updateCount++;

            list.AddRange(new[] { new Item(3), new Item(1) });
            list.InsertRange(1, new[] { new Item(2) });
            list.Sort();
            list.Reverse();
            list.RemoveRange(0, 1);
            list.RemoveAll(item => item.Value == 1);
            list.Clear();

            Assert.That(addCount, Is.EqualTo(2));
            Assert.That(removeCount, Is.EqualTo(2));
            Assert.That(updateCount, Is.EqualTo(3));
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void ObservableListNonGenericMutationsNotifyAndClearAfterStateChanges()
        {
            var list = new ObservableList<Item>();
            var untyped = (IList)list;
            var events = new List<string>();
            list.CollectionAdd += (sender, index, item) => events.Add($"add:{index}");
            list.CollectionReplace += (sender, index, oldItem, newItem) => events.Add($"replace:{index}");
            list.CollectionRemove += (sender, index, item) => events.Add($"remove:{index}");
            list.CollectionUpdate += sender => events.Add($"clear:{list.Count}");

            untyped.Add(new Item(1));
            untyped.Insert(0, new Item(2));
            untyped[1] = new Item(3);
            untyped.Remove(untyped[0]);
            list.ConvertAll(item => item.Value);
            untyped.Clear();

            Assert.That(events, Is.EqualTo(new[]
            {
                "add:0",
                "add:0",
                "replace:1",
                "remove:0",
                "clear:0",
            }));
        }

        [Test]
        public void ObservableListQueryOperationsMatchListSemantics()
        {
            var one = new Item(1);
            var two = new Item(2);
            var three = new Item(3);
            var list = new ObservableList<Item>(new[] { one, two, three, two });
            var copied = new Item[4];
            var visited = 0;

            list.CopyTo(copied);
            list.ForEach(item => visited += item.Value);

            Assert.That(list.Contains(two), Is.True);
            Assert.That(list.Exists(item => item.Value == 3), Is.True);
            Assert.That(list.Find(item => item.Value == 2), Is.SameAs(two));
            Assert.That(list.FindAll(item => item.Value == 2), Has.Count.EqualTo(2));
            Assert.That(list.FindIndex(item => item.Value == 2), Is.EqualTo(1));
            Assert.That(list.FindIndex(2, item => item.Value == 2), Is.EqualTo(3));
            Assert.That(list.FindIndex(1, 2, item => item.Value == 3), Is.EqualTo(2));
            Assert.That(list.FindLast(item => item.Value == 2), Is.SameAs(two));
            Assert.That(list.FindLastIndex(item => item.Value == 2), Is.EqualTo(3));
            Assert.That(list.GetRange(1, 2), Is.EqualTo(new[] { two, three }));
            Assert.That(list.IndexOf(two), Is.EqualTo(1));
            Assert.That(list.LastIndexOf(two), Is.EqualTo(3));
            Assert.That(list.TrueForAll(item => item.Value > 0), Is.True);
            Assert.That(list.ToArray(), Is.EqualTo(copied));
            Assert.That(visited, Is.EqualTo(8));
        }

        [Test]
        public void BindingListConnectsAndDisconnectsCollectionNotifications()
        {
            var probe = new ListProbe();
            var list = new ObservableList<Item>();

            BindingContextUtility.BindingList(probe, null, list);
            list.Add(new Item(1));
            list.RemoveAt(0);
            list.Clear();
            BindingContextUtility.UnbindingList(probe, list);
            list.Add(new Item(2));

            Assert.That(probe.AddCount, Is.EqualTo(1));
            Assert.That(probe.RemoveCount, Is.EqualTo(1));
            Assert.That(probe.UpdateCount, Is.EqualTo(1));
            Assert.That(probe.ReplaceCount, Is.Zero);
        }

        [Test]
        public void PrimitiveConvertersRoundTripSupportedValues()
        {
            Assert.That(new InvertBoolConverter().Convert(true), Is.False);
            Assert.That(new InvertBoolConverter().ConvertBack(false), Is.True);
            Assert.That(new IntToBoolConverter().Convert(2), Is.True);
            Assert.That(new IntToBoolConverter().ConvertBack(false), Is.Zero);
            Assert.That(new FloatToBoolConverter().Convert(0f), Is.False);
            Assert.That(new FloatToBoolConverter().ConvertBack(true), Is.EqualTo(1f));
            Assert.That(new DoubleToBoolConverter().Convert(0.5d), Is.True);
            Assert.That(new DoubleToBoolConverter().ConvertBack(false), Is.Zero);
            Assert.That(new StringToBoolConverter().Convert(string.Empty), Is.False);
            Assert.That(new StringToBoolConverter().ConvertBack(true), Is.EqualTo("true"));
            Assert.That(new IntToStringConverter().ConvertBack("12"), Is.EqualTo(12));
            Assert.That(new LongToStringConverter().ConvertBack("12"), Is.EqualTo(12L));
            Assert.That(new ShortToStringConverter().ConvertBack("12"), Is.EqualTo((short)12));
            Assert.That(new ByteToStringConverter().ConvertBack("12"), Is.EqualTo((byte)12));
            Assert.That(new FloatToStringConverter().ConvertBack("12"), Is.EqualTo(12f));
            Assert.That(new DoubleToStringConverter().ConvertBack("12"), Is.EqualTo(12d));
            Assert.That(new DecimalToStringConverter().ConvertBack("12"), Is.EqualTo(12m));
            Assert.That(new BoolToStringConverter().ConvertBack("true"), Is.True);
        }

        [Test]
        public void UntypedConverterValidatesRequestedTypes()
        {
            IValueConverter converter = new IntToStringConverter();

            Assert.That(converter.Convert(42, typeof(string)), Is.EqualTo("42"));
            Assert.That(converter.ConvertBack("42", typeof(int)), Is.EqualTo(42));
            Assert.Throws<Exception>(() => converter.Convert(42, typeof(int)));
            Assert.Throws<Exception>(() => converter.ConvertBack("42", typeof(string)));
            Assert.Throws<Exception>(() => new IntToStringConverter().ConvertBack("invalid"));
            Assert.Throws<Exception>(() => new ObjectToStringConverter().ConvertBack("value"));
        }
    }
}
