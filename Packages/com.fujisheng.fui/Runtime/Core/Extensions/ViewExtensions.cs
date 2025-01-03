namespace FUI.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// ��ȡһ��View��ĳ��Ԫ��
        /// </summary>
        /// <typeparam name="TElement">Ԫ������</typeparam>
        /// <param name="view">��Ӧ��View</param>
        /// <param name="elementPath">Ԫ��·��</param>
        /// <returns>��Ӧ��Ԫ��</returns>
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