using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI.VisualElement
{
    /// <summary>
    /// �б���ͼԪ�ػ���
    /// </summary>
    public abstract class ListViewElement : UGUIVisualElement<IReadOnlyObservableList<ObservableObject>>, IContainerElement
    {
        protected IReadOnlyObservableList<ObservableObject> List;
        public sealed override void UpdateValue(IReadOnlyObservableList<ObservableObject> value)
        {
            if (value == List || value == null)
            {
                return;
            }

            if (List != null)
            {
                List.CollectionAdd -= OnCollectionAdd;
                List.CollectionRemove -= OnCollectionRemove;
                List.CollectionReplace -= OnCollectionReplace;
                List.CollectionReset -= OnCollectionReset;
                List.CollectionMove -= OnCollectionMove;
            }

            value.CollectionAdd += OnCollectionAdd;
            value.CollectionRemove += OnCollectionRemove;
            value.CollectionReplace += OnCollectionReplace;
            value.CollectionReset += OnCollectionReset;
            value.CollectionMove += OnCollectionMove;
            this.List = value;
            OnUpdate();
        }

        void OnCollectionAdd(object sender, int? index, object item)
        {
            // ���indexΪnull����itemΪnull����ʾ�����б�������
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnAdd(index.Value, item as ObservableObject);
            }
        }

        void OnCollectionRemove(object sender, int? index, object item)
        {
            if (index == null || item == null)
            {
                OnUpdate();
            }
            else
            {
                OnRemove(index.Value, item as ObservableObject);
            }
        }

        void OnCollectionReplace(object sender, int? index, object oldItem, object newItem)
        {
            if (index == null || oldItem == null || newItem == null)
            {
                OnUpdate();
            }
            else
            {
                OnReplace(index.Value, oldItem as ObservableObject, newItem as ObservableObject);
            }
        }

        void OnCollectionReset(object sender)
        {
            OnReset();
        }

        void OnCollectionMove(object sender)
        {
            OnUpdate();
        }

        /// <summary>
        /// �����һ��Ԫ�ص�ʱ��
        /// </summary>
        /// <param name="index">��ӵ�index</param>
        /// <param name="item">��ӵ�item</param>
        protected virtual void OnAdd(int index, ObservableObject item) { }

        /// <summary>
        /// ���Ƴ�һ��Ԫ�ص�ʱ��
        /// </summary>
        /// <param name="index">�Ƴ���index</param>
        /// <param name="item">�Ƴ�����</param>
        protected virtual void OnRemove(int index, ObservableObject item) { }

        /// <summary>
        /// ���滻��ʱ��
        /// </summary>
        /// <param name="index">�滻��index</param>
        /// <param name="oldItem">�滻ǰ��item</param>
        /// <param name="newItem">�滻���item</param>
        protected virtual void OnReplace(int index, ObservableObject oldItem, ObservableObject newItem) { }

        /// <summary>
        /// �����õ�ʱ��
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// ��ȫ�����µ�ʱ��
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// ����һ��ItemView
        /// </summary>
        /// <param name="itemViewModel">���Item��Ӧ����ͼģ��</param>
        /// <param name="itemObject">���Item��Ӧ��GameObject</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected UIEntity CreateItemEntity(ObservableObject itemViewModel, GameObject itemObject)
        {
            var itemView = new UGUIView(AssetLoader, itemObject, string.Empty);
            return UIEntity.Create(itemView, itemViewModel);
        }
    }
}