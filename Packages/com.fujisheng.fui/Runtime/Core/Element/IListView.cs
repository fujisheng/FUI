using FUI.Bindable;

namespace FUI
{
    /// <summary>
    /// �������б����ͼԪ��
    /// </summary>
    public interface IListView : IView, IElement
    {
        /// <summary>
        /// ����
        /// </summary>
        BindableProperty<IReadOnlyObservableList<ObservableObject>> List { get; }

        /// <summary>
        /// ����Ԫ�ر����ʱ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">����ӵ�Ԫ�ص�index</param>
        /// <param name="itemData">����ӵ�Ԫ��</param>
        void OnAdd(object sender, int? index, object itemData);

        /// <summary>
        /// ����Ԫ�ر��Ƴ�ʱ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">���Ƴ���Ԫ��index</param>
        /// <param name="item">���Ƴ���Ԫ��</param>
        void OnRemove(object sender, int? index, object item);

        /// <summary>
        /// ����Ԫ�ر��滻ʱ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index">���滻��Ԫ��index</param>
        /// <param name="oldItem">���滻��Ԫ��</param>
        /// <param name="newItem">�滻���Ԫ��</param>
        void OnReplace(object sender, int? index, object oldItem, object newItem);

        /// <summary>
        /// �������б�����ʱ
        /// </summary>
        void OnUpdate(object sender);
    }
}