namespace FUI.BindingDescriptor
{
    public sealed class PropertyBindingDescriptor
    {
        internal PropertyBindingDescriptor() { }

        /// <summary>
        /// Ҫʹ�õ�ת����
        /// </summary>
        /// <typeparam name="T">Ҫʹ�õ�ת��������</typeparam>
        /// <returns></returns>
        public PropertyBindingDescriptor WithConverter<T>() where T : IValueConverter
        {
            return this;
        }

        /// <summary>
        /// Ҫ�󶨵�Element·��
        /// </summary>
        /// <param name="target">·��</param>
        /// <returns></returns>
        public PropertyBindingDescriptor ToTarget(string target)
        {
            return this;
        }

        /// <summary>
        /// Ҫ�󶨵�Element����
        /// </summary>
        /// <param name="element">Ŀ������ ��ʹ��nameof(XElement.XProperty)</param>
        /// <returns></returns>
        public PropertyBindingDescriptor ToElement(string element)
        {
            return this;
        }

        /// <summary>
        /// Ҫʹ�õİ�ģʽ
        /// </summary>
        /// <param name="mode">��ģʽ</param>
        /// <returns></returns>
        public PropertyBindingDescriptor WithBindingMode(BindingMode mode)
        {
            return this;
        }
    }
}