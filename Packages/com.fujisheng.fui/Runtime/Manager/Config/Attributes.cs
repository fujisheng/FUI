using System;

namespace FUI.Manager
{
    /// <summary>
    /// ��������
    /// </summary>
    [Flags]
    public enum Attributes
    {
        /// <summary>
        /// ���κ�����
        /// </summary>
        None = 0,

        /// <summary>
        /// �Ƿ�ȫ��
        /// </summary>
        FullScreen = 1,

        /// <summary>
        /// �Ƿ�����ͬʱ���ڶ��ʵ��
        /// </summary>
        AllowMultiple = 1 << 1,
    }
}