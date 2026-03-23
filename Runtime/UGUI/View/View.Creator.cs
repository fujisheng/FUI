using UnityEngine;

namespace FUI.UGUI
{
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
        /// <param name="go">游戏实体</param>
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
    }
}