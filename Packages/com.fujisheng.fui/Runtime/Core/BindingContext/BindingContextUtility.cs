using FUI.Bindable;

namespace FUI
{
    public static class BindingContextUtility
    {
        /// <summary>
        /// 绑定一个可观察列表到Element
        /// </summary>
        /// <param name="element">视觉元素</param>
        /// <param name="preSource">修改前的可观察列表</param>
        /// <param name="source">修改后的可观察列表</param>
        public static void BindingList<TElement>(TElement element, INotifyCollectionChanged preSource, INotifyCollectionChanged source) where TElement : class, IListView
        {
            if (preSource != null)
            {
                UnbindingList(element, preSource);
            }

            if (source != null)
            {
                source.CollectionAdd += element.OnAdd;
                source.CollectionRemove += element.OnRemove;
                source.CollectionReplace += element.OnReplace;
                source.CollectionUpdate += element.OnUpdate;
            }
        }

        /// <summary>
        /// 解绑可观察列表
        /// </summary>
        /// <typeparam name="TElement">目标Element类型</typeparam>
        /// <param name="element">目标Element</param>
        /// <param name="listSource">数据源</param>
        public static void UnbindingList<TElement>(TElement element, INotifyCollectionChanged listSource) where TElement : class, IListView
        {
            if (listSource == null)
            {
                return;
            }

            listSource.CollectionAdd -= element.OnAdd;
            listSource.CollectionRemove -= element.OnRemove;
            listSource.CollectionReplace -= element.OnReplace;
            listSource.CollectionUpdate -= element.OnUpdate;
        }
    }
}