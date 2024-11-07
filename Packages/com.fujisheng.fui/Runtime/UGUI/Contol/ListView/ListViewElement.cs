using FUI.Bindable;

using System;

using UnityEngine;

namespace FUI.UGUI.Control
{
    /// <summary>
    /// �б���ͼԪ�ػ���
    /// </summary>
    public abstract class ListViewElement : UGUIView, IContainerElement, IListView
    {
        public BindableProperty<IReadOnlyObservableList<ObservableObject>> Data { get; private set; }

        protected override void Initialize()
        {
            Data = new BindableProperty<IReadOnlyObservableList<ObservableObject>>();
            Data.OnValueChanged += OnSetData;
        }

        void OnSetData(IReadOnlyObservableList<ObservableObject> oldValue, IReadOnlyObservableList<ObservableObject> newValue)
        {
            if(newValue == null)
            {
                return;
            }

            OnUpdate();
        }

        void IListView.OnAdd(object sender, int? index, object item)
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

        void IListView.OnRemove(object sender, int? index, object item)
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

        void IListView.OnReplace(object sender, int? index, object oldItem, object newItem)
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

        void IListView.OnUpdate(object sender)
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
            var itemView = UGUIView.Create(this.AssetLoader, itemObject, string.Empty);
            return UIEntity.Create(itemView, itemViewModel);
        }

        protected override void Destroy()
        {
            Data.ClearValueChangedEvent();
        }
    }
}