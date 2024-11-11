using FUI.Bindable;

namespace FUI.Extensions
{
    public static class BindingContextExtensions
    {
        /// <summary>
        /// ��һ���ɹ۲��б�Element
        /// </summary>
        /// <param name="element">�Ӿ�Ԫ��</param>
        /// <param name="preValue">�޸�ǰ�Ŀɹ۲��б�</param>
        /// <param name="value">�޸ĺ�Ŀɹ۲��б�</param>
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
        /// ���ɹ۲��б���Ӿ�Ԫ��
        /// </summary>
        /// <typeparam name="TElement">�Ӿ�Ԫ������</typeparam>
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