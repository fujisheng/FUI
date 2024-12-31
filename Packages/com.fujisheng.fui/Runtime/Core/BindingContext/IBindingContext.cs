using FUI.Bindable;

using System;

namespace FUI
{
    /// <summary>
    /// ��������
    /// </summary>
    internal interface IBindingContext
    {
        /// <summary>
        /// ���������������Ӧ����ͼ
        /// </summary>
        internal IView View { get; }

        /// <summary>
        /// ���������������Ӧ����ͼģ������
        /// </summary>
        internal Type ViewModelType { get; }

        /// <summary>
        /// ���������������Ӧ����ͼģ��
        /// </summary>
        internal ObservableObject ViewModel { get; }

        /// <summary>
        /// ����View
        /// </summary>
        /// <param name="view">���º��view</param>
        /// <returns></returns>
        internal bool UpdateView(IView view);

        /// <summary>
        /// ����ViewModel ���Ը���ΪԭʼViewModel������
        /// </summary>
        /// <param name="viewModel">���º��ViewModel</param>
        /// <returns></returns>
        internal bool UpdateViewModel(ObservableObject viewModel);

        /// <summary>
        /// ��
        /// </summary>
        internal void Binding();

        /// <summary>
        /// ���
        /// </summary>
        internal void Unbinding();
    }
}