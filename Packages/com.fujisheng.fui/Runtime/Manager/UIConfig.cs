using System;

namespace FUI.Manager
{
    /// <summary>
    /// ��������
    /// </summary>
    public struct UIConfig : IEquatable<UIConfig>
    {
        ///<summary>
        /// �㼶
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// ���
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