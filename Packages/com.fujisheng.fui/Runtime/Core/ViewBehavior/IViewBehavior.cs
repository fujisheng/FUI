using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// ��ͼ��Ϊ
    /// </summary>
    internal interface IViewBehavior
    {
        /// <summary>
        /// ���Դ������ͼģ������
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// ������ͼģ��
        /// </summary>
        /// <param name="vm">��ͼģ��</param>
        internal bool UpdateViewModel(ObservableObject vm);

        /// <summary>
        /// �����������ͼ��Ϊ��ʱ��
        /// </summary>
        /// <param name="vm">����ʱ����ͼģ��</param>
        internal void OnCreate(ObservableObject vm);

        /// <summary>
        /// ���������View��ʱ��
        /// </summary>
        /// <param name="param">��ʱ�Ĳ���</param>
        internal void OnEnable(object param);

        /// <summary>
        /// ����������ǵ�ǰ�۽���View��ʱ��
        /// </summary>
        internal void OnFocus();

        /// <summary>
        /// ���������ʧ����ʱ��
        /// </summary>
        internal void OnUnfocus();

        /// <summary>
        /// �����������View��ʱ��
        /// </summary>
        internal void OnDisable();

        /// <summary>
        /// ������������ٵ�ʱ��
        /// </summary>
        internal void OnDestroy();
    }
}