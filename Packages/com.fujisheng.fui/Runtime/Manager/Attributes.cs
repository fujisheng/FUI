using System;

namespace FUI.Manager
{
    public struct ViewConfig : IComparable<ViewConfig>
    {
        ///<summary>
        /// �㼶
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// ���
        /// </summary>
        public readonly ViewFlag flag;

        public ViewConfig(int layer, ViewFlag flag)
        {
            this.layer = layer;
            this.flag = flag;
        }

        public static ViewConfig Default => new ViewConfig((int)Layer.Common, ViewFlag.None);

        public int CompareTo(ViewConfig other)
        {
            if (layer != other.layer)
            {
                return layer.CompareTo(other.layer);
            }

            return flag.CompareTo(other.flag);
        }
    }

    public class DefaultViewConfigAttribute : Attribute
    {
        public readonly ViewConfig config;

        /// <summary>
        /// ����һ��Ĭ�ϵĽ�������
        /// </summary>
        /// <param name="layer">�㼶</param>
        /// <param name="flag">���</param>
        public DefaultViewConfigAttribute(Layer layer = Layer.Common, ViewFlag flag = ViewFlag.None)
        {
            this.config = new ViewConfig((int)layer, flag);
        }

        /// <summary>
        /// ����һ��Ĭ�ϵĽ�������
        /// </summary>
        /// <param name="layer">�㼶</param>
        /// <param name="flag">���</param>
        public DefaultViewConfigAttribute(int layer, ViewFlag flag = ViewFlag.None)
        {
            this.config = new ViewConfig(layer, flag);
        }
    }
}