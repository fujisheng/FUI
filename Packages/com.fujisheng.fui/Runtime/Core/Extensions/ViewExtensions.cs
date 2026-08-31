using FUI.Diagnostics;

namespace FUI.Rendering
{
    /// <summary>
    /// 提供 View Element 查询的强类型便利方法和可选查询语义。
    /// </summary>
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
                Log.Instance.LogError($"{view.Name} not found {elementPath}({typeof(TElement)})");
            }

            return element;
        }

        /// <summary>
        /// 尝试获取兼容 View 提供的 Element。未提供表示当前投影不展示该字段，不记录错误。
        /// </summary>
        public static TElement TryGetElement<TElement>(this IView view, string elementPath) where TElement : class, IElement
        {
            return view.GetElement<TElement>(elementPath);
        }
    }
}
