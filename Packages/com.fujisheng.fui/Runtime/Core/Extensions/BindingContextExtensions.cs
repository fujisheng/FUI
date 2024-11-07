using FUI.Bindable;

namespace FUI.Extensions
{
    public static class BindingContextExtensions
    {
        public static void BindingList(IElement element, INotifyCollectionChanged preValue, INotifyCollectionChanged @value)
        {
            if (element is FUI.IListView listView)
            {
                if (preValue != null)
                {
                    preValue.CollectionAdd -= listView.OnAdd;
                    preValue.CollectionRemove -= listView.OnRemove;
                    preValue.CollectionReplace -= listView.OnReplace;
                    preValue.CollectionUpdate -= listView.OnUpdate;
                }

                if (@value != null)
                {
                    @value.CollectionAdd += listView.OnAdd;
                    @value.CollectionRemove += listView.OnRemove;
                    @value.CollectionReplace += listView.OnReplace;
                    @value.CollectionUpdate += listView.OnUpdate;
                }
            }
        }

        public static void UnbindingList<TElement>(this BindingContext context, INotifyCollectionChanged list, string elementPath) where TElement : class, IElement
        {
            if (list == null)
            {
                return;
            }

            var element = context.View.GetElement<TElement>(elementPath);
            if (element is FUI.IListView listView)
            {
                list.CollectionAdd -= listView.OnAdd;
                list.CollectionRemove -= listView.OnRemove;
                list.CollectionReplace -= listView.OnReplace;
                list.CollectionUpdate -= listView.OnUpdate;
            }
        }
    }
}