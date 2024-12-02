using System;

namespace FUI
{
    public partial class UIEntity
    {
        /// <summary>
        /// 当有UI实体创建时
        /// </summary>
        public static event Action<UIEntity> OnEntityCreated;

        /// <summary>
        /// 当有UI实体启用时
        /// </summary>
        public static event Action<UIEntity> OnEntityEnabled;

        /// <summary>
        /// 当有UI实体禁用时
        /// </summary>
        public static event Action<UIEntity> OnEntityDisabled;

        /// <summary>
        /// 当有UI实体获取焦点时
        /// </summary>
        public static event Action<UIEntity> OnEntityFocused;

        /// <summary>
        /// 当有UI实体失去焦点时
        /// </summary>
        public static event Action<UIEntity> OnEntityUnfocused;

        /// <summary>
        /// 当有UI实体销毁时
        /// </summary>
        public static event Action<UIEntity> OnEntityDestoryed;

        /// <summary>
        /// 当有UI实体冻结时
        /// </summary>
        public static event Action<UIEntity> OnEntityFreezed;

        /// <summary>
        /// 当有UI实体解冻时
        /// </summary>
        public static event Action<UIEntity> OnEntityUnfreezed;
    }
}