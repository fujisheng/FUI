using System;

namespace FUI
{
    public partial class UIEntity
    {
        /// <summary>
        /// ����UIʵ�崴��ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityCreated;

        /// <summary>
        /// ����UIʵ������ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityEnabled;

        /// <summary>
        /// ����UIʵ�����ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityDisabled;

        /// <summary>
        /// ����UIʵ���ȡ����ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityFocused;

        /// <summary>
        /// ����UIʵ��ʧȥ����ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityUnfocused;

        /// <summary>
        /// ����UIʵ������ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityDestoryed;

        /// <summary>
        /// ����UIʵ�嶳��ʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityFreezed;

        /// <summary>
        /// ����UIʵ��ⶳʱ
        /// </summary>
        public static event Action<UIEntity> OnEntityUnfreezed;
    }
}