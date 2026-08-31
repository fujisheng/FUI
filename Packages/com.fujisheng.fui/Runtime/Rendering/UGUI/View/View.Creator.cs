using UnityEngine;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 提供从资源路径或现有 GameObject 创建并初始化 View 的入口。
    /// </summary>
    public partial class View
    {
        /// <summary>
        /// 创建一个UGUIView
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="assetPath">资源路径</param>
        /// <param name="viewName">view名字</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, string assetPath, string viewName)
        {
            var go = assetLoader.CreateGameObject(assetPath);
            return Create(assetLoader, go, viewName);
        }

        /// <summary>
        /// 创建一个UGUIView
        /// </summary>
        /// <param name="assetLoader">资源加载器</param>
        /// <param name="go">对象实例</param>
        /// <param name="viewName">view名字</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, GameObject go, string viewName = null)
        {
            if (go == null)
            {
                return null;
            }

            if (!go.TryGetComponent<View>(out var view))
            {
                view = go.AddComponent<View>();
            }

            view.viewName = viewName == null ? Utility.RemoveCloneSuffix(go.name) : viewName;
            view.SetAssetLoader(assetLoader);
            return view;
        }

        /// <summary>
        /// 在现有宿主对象上创建子投影 View，并从子 View 的扫描中排除宿主 Element。
        /// </summary>
        internal static View CreateProjection(IAssetLoader assetLoader, GameObject gameObject, IElement ownerElement)
        {
            var view = Create(assetLoader, gameObject);
            if (view != null)
            {
                view.excludedElement = ownerElement;
            }
            return view;
        }
    }
}
