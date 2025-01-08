using System;

namespace FUI.Manager
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ConfigAttribute : Attribute
    {
        public readonly UIConfig config;

        /// <summary>
        /// ����һ��Ĭ�ϵĽ�������
        /// </summary>
        /// <param name="layer">�㼶</param>
        /// <param name="attributes">����</param>
        public ConfigAttribute(Layer layer = Layer.Common, Attributes attributes = Attributes.None)
        {
            this.config = new UIConfig((int)layer, attributes);
        }

        /// <summary>
        /// ����һ��Ĭ�ϵĽ�������
        /// </summary>
        /// <param name="layer">�㼶</param>
        /// <param name="attributes">����</param>
        public ConfigAttribute(int layer, Attributes attributes = Attributes.None)
        {
            this.config = new UIConfig(layer, attributes);
        }
    }
}