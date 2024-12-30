using FUI.Bindable;

namespace FUI
{
    public static class BindingContextUtility
    {
        /// <summary>
        /// ��һ���ɹ۲��б�Element
        /// </summary>
        /// <param name="element">�Ӿ�Ԫ��</param>
        /// <param name="preSource">�޸�ǰ�Ŀɹ۲��б�</param>
        /// <param name="source">�޸ĺ�Ŀɹ۲��б�</param>
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
        /// ���ɹ۲��б�
        /// </summary>
        /// <typeparam name="TElement">Ŀ��Element����</typeparam>
        /// <param name="element">Ŀ��Element</param>
        /// <param name="listSource">����Դ</param>
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