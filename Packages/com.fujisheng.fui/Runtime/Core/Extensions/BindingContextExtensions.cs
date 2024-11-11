using FUI.Bindable;

namespace FUI.Extensions
{
    public static class BindingContextExtensions
    {
        /// <summary>
        /// 绑定一个可观察列表到Element
        /// </summary>
        /// <param name="element">视觉元素</param>
        /// <param name="preValue">修改前的可观察列表</param>
        /// <param name="value">修改后的可观察列表</param>
        public static void BindingList<TElement>(TElement element, INotifyCollectionChanged preValue, INotifyCollectionChanged @value) where TElement : IListView
        {
            if (preValue != null)
            {
                preValue.CollectionAdd -= element.OnAdd;
                preValue.CollectionRemove -= element.OnRemove;
                preValue.CollectionReplace -= element.OnReplace;
                preValue.CollectionUpdate -= element.OnUpdate;
            }

            if (@value != null)
            {
                @value.CollectionAdd += element.OnAdd;
                @value.CollectionRemove += element.OnRemove;
                @value.CollectionReplace += element.OnReplace;
                @value.CollectionUpdate += element.OnUpdate;
            }
        }

        /// <summary>
        /// 解绑可观察列表和视觉元素
        /// </summary>
        /// <typeparam name="TElement">视觉元素类型</typeparam>
        /// <param name="context"></param>
        /// <param name="list"></param>
        /// <param name="elementPath"></param>
        public static void UnbindingList<TElement>(this BindingContext context, INotifyCollectionChanged list, string elementPath) where TElement : class, IListView
        {
            if (list == null)
            {
                return;
            }

            var element = context.View.GetElement<TElement>(elementPath);
            list.CollectionAdd -= element.OnAdd;
            list.CollectionRemove -= element.OnRemove;
            list.CollectionReplace -= element.OnReplace;
            list.CollectionUpdate -= element.OnUpdate;
        }
    }
}