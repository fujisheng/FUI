using System;

namespace FUI.Manager
{
    public struct ViewConfig : IComparable<ViewConfig>
    {
        ///<summary>
        /// 层级
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// 标记
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
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="flag">标记</param>
        public DefaultViewConfigAttribute(Layer layer = Layer.Common, ViewFlag flag = ViewFlag.None)
        {
            this.config = new ViewConfig((int)layer, flag);
        }

        /// <summary>
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="flag">标记</param>
        public DefaultViewConfigAttribute(int layer, ViewFlag flag = ViewFlag.None)
        {
            this.config = new ViewConfig(layer, flag);
        }
    }
}