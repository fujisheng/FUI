using System;

namespace FUI.Manager
{
    /// <summary>
    /// 界面配置
    /// </summary>
    public struct UIConfig : IEquatable<UIConfig>
    {
        ///<summary>
        /// 层级
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// 标记
        /// </summary>
        public readonly Attributes flag;

        public UIConfig(int layer, Attributes flag)
        {
            this.layer = layer;
            this.flag = flag;
        }

        public static UIConfig Default => new UIConfig((int)Layer.Common, Attributes.None);

        public bool Equals(UIConfig other)
        {
            return layer == other.layer && flag == other.flag;
        }

        public override bool Equals(object obj)
        {
            return obj is UIConfig other && Equals(other);
        }
    }
}