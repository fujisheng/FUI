using FUI.Binding;
using FUI.Rendering;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class FixedListLayoutTests
    {
        sealed class Row : ObservableObject
        {
        }

        [Test]
        public void VisibleGridRangeUsesOnlyIntersectingRowsAndTheirNeighbors()
        {
            var layout = new FixedListLayout(3, 20, 5, 10, 8);
            Assert.That(layout.GetRange(1000, 60, 50), Is.EqualTo((3, 15)));
            Assert.That(layout.GetRange(1000, 60, 0), Is.EqualTo((0, 0)));
            Assert.That(layout.GetRange(0, 60, 50), Is.EqualTo((0, 0)));
        }

        [Test]
        public void ChangingColumnCountRetainsTheFirstItemAndItsLocalOffset()
        {
            var before = new FixedListLayout(3, 20, 5, 10, 8);
            var after = new FixedListLayout(2, 20, 5, 10, 8);
            Assert.That(after.RetainAnchor(before, 1000, 65, 50, 50), Is.EqualTo(90));
            Assert.That(after.RetainAnchor(before, 0, 65, 50, 50), Is.EqualTo(0));
        }

        [Test]
        public void ScrollToLastItemPreservesViewportAndPadding()
        {
            var layout = new FixedListLayout(3, 20, 5, 10, 8);
            Assert.That(layout.GetExtent(1000), Is.EqualTo(8363));
            Assert.That(layout.GetScrollOffset(1000, 999, 0, 50), Is.EqualTo(8305));
            Assert.That(layout.GetScrollOffset(1000, 999, 8305, 50), Is.EqualTo(8305));
            Assert.That(layout.GetScrollOffset(1000, 0, 8305, 50), Is.EqualTo(10));
        }

        [Test]
        public void IndexedMutationKeepsTheSameOccurrenceOfADuplicateItem()
        {
            var repeated = new Row();
            var anchor = new ListScrollAnchor(2, 5, repeated);
            var source = new ObservableList<ObservableObject> { new Row(), repeated, new Row(), repeated };

            var shifted = anchor.Move(source, 0, 1);

            Assert.That(shifted.Index, Is.EqualTo(3));
            Assert.That(shifted.Offset, Is.EqualTo(5));
        }

        [Test]
        public void BulkReorderFindsThePreviouslyVisibleModel()
        {
            var item = new Row();
            var source = new ObservableList<ObservableObject> { new Row(), new Row(), item, new Row() };

            var moved = new ListScrollAnchor(0, 4, item).Move(source, null, 0);

            Assert.That(moved.Index, Is.EqualTo(2));
            Assert.That(moved.Offset, Is.EqualTo(4));
        }
    }
}
