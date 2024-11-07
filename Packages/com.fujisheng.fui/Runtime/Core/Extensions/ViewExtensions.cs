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
            if (!(view is FUI.IElement e))
            {
                throw new System.Exception($"{view.Name} not FUI.IElement");
            }

            var element = e.GetChild<TElement>(elementPath);
            if (element == null)
            {
                throw new System.Exception($"{view.Name} not found {elementPath}:{typeof(TElement)}");
            }

            return element;
        }
    }
}