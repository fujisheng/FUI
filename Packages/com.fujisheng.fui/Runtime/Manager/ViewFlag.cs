using System;

namespace FUI.Manager
{
    /// <summary>
    /// �����ǩ
    /// </summary>
    [Flags]
    public enum ViewFlag
    {
        /// <summary>
        /// ���κα�ǩ
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