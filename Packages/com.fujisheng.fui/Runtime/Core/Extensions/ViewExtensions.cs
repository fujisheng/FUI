namespace FUI.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// 获取一个View的某个元素
        /// </summary>
        /// <typeparam name="TElement">元素类型</typeparam>
        /// <param name="view">对应的View</param>
        /// <param name="elementPath">元素路径</param>
        /// <returns>对应的元素</returns>
        /// <exception cref="System.Exception"></exception>
        public static TElement GetElement<TElement>(this IView view, string elementPath) where TElement : class, IElement
        {
            var element = view.GetElement<TElement>(elementPath);
            if (element == null)
            {
                throw new System.Exception($"{view.Name} not found {elementPath}({typeof(TElement)})");
            }

            return element;
        }
    }
}