using System;

namespace FUI.Manager
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ConfigAttribute : Attribute
    {
        public readonly UIConfig config;

        /// <summary>
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="attributes">特性</param>
        public ConfigAttribute(Layer layer = Layer.Common, Attributes attributes = Attributes.None)
        {
            this.config = new UIConfig((int)layer, attributes);
        }

        /// <summary>
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="attributes">特性</param>
        public ConfigAttribute(int layer, Attributes attributes = Attributes.None)
        {
            this.config = new UIConfig(layer, attributes);
        }
    }
}