using System;

namespace FUI
{
    [Flags]
    public enum UIEntityState
    {
        /// <summary>
        /// ���
        /// </summary>
        Alive = 1 << 0,

        /// <summary>
        /// ����
        /// </summary>
        Enabled = 1 << 1,

        /// <summary>
        /// ����״̬
        /// </summary>
        Freezed = 1 << 2,
    }
}