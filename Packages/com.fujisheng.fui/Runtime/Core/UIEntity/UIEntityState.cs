using System;

namespace FUI
{
    [Flags]
    public enum UIEntityState
    {
        /// <summary>
        /// ´æ»î
        /// </summary>
        Alive = 1 << 0,

        /// <summary>
        /// ¼¤»î
        /// </summary>
        Enabled = 1 << 1,

        /// <summary>
        /// ¶³½á×´Ì¬
        /// </summary>
        Freezed = 1 << 2,
    }
}