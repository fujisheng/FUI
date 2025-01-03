using UnityEngine;

namespace FUI.UGUI
{
    public partial class View
    {
        /// <summary>
        /// ����һ��UGUIView
        /// </summary>
        /// <param name="assetLoader">��Դ������</param>
        /// <param name="assetPath">��Դ·��</param>
        /// <param name="viewName">view����</param>
        /// <param name="initialized">�ѳ�ʼ����</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, string assetPath, string viewName)
        {
            var go = assetLoader.CreateGameObject(assetPath);
            return Create(assetLoader, go, viewName);
        }

        /// <summary>
        /// ����һ��UGUIView
        /// </summary>
        /// <param name="assetLoader">��Դ������</param>
        /// <param name="go">��Ϸʵ��</param>
        /// <param name="viewName">view����</param>
        /// <param name="initialized">�ѳ�ʼ����</param>
        /// <returns></returns>
        public static View Create(IAssetLoader assetLoader, GameObject go, string viewName)
        {
            if (go == null)
            {
                return null;
            }

            if (!go.TryGetComponent<View>(out var view))
            {
                view = go.AddComponent<View>();
            }

            view.viewName = viewName;
            view.OnCreate(assetLoader);
            return view;
        }
    }
}