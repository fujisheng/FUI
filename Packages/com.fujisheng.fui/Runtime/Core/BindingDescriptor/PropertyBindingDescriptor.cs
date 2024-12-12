namespace FUI.BindingDescriptor
{
    public sealed class PropertyBindingDescriptor
    {
        internal PropertyBindingDescriptor() { }

        /// <summary>
        /// 要使用的转换器
        /// </summary>
        /// <typeparam name="T">要使用的转换器类型</typeparam>
        /// <returns></returns>
        public PropertyBindingDescriptor WithConverter<T>() where T : IValueConverter
        {
            return this;
        }

        /// <summary>
        /// 要绑定的Element路径
        /// </summary>
        /// <param name="target">路径</param>
        /// <returns></returns>
        public PropertyBindingDescriptor ToTarget(string target)
        {
            return this;
        }

        /// <summary>
        /// 要绑定的Element名字
        /// </summary>
        /// <param name="element">目标名字 请使用nameof(XElement.XProperty)</param>
        /// <returns></returns>
        public PropertyBindingDescriptor ToElement(string element)
        {
            return this;
        }

        /// <summary>
        /// 要使用的绑定模式
        /// </summary>
        /// <param name="mode">绑定模式</param>
        /// <returns></returns>
        public PropertyBindingDescriptor WithBindingMode(BindingMode mode)
        {
            return this;
        }
    }
}