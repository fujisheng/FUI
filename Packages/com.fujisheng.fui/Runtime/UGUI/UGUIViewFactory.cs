using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.UGUI
{
    /// <summary>
    /// uguiView构造工厂
    /// </summary>
    public class UGUIViewFactory : IViewFactory
    {
        readonly IAssetLoaderFactory assetLoaderFactory;

        /// <summary>
        /// 构造一个UGUIView工厂
        /// </summary>
        /// <param name="assetLoaderFactory">AssetLoader工厂</param>
        public UGUIViewFactory(IAssetLoaderFactory assetLoaderFactory)
        {
            this.assetLoaderFactory = assetLoaderFactory;
        }

        /// <summary>
        /// 确保AssetLoaderFactory不为空
        /// </summary>
        /// <exception cref="Exception"></exception>
        void EnsureAssetLoaderFactory()
        {
            if(assetLoaderFactory == null)
            {
                throw new Exception("UGUIViewFactory assetLoaderFactory is null");
            }
        }

        public IView Create(string viewName)
        {
            EnsureAssetLoaderFactory();
            if (string.IsNullOrEmpty(viewName))
            {
                return null;
            }
            var assetLoader = assetLoaderFactory.Create(viewName);
            var viewObj = assetLoader.CreateGameObject(viewName);
            viewObj.SetActive(false);
            return View.Create(assetLoader, viewObj, viewName);
        }

        public async ValueTask<IView> CreateAsync(string viewName, CancellationToken token)
        {
            EnsureAssetLoaderFactory();
            if (string.IsNullOrEmpty(viewName))
            {
                return null;
            }
            var assetLoader = assetLoaderFactory.Create(viewName);
            var viewObj = await assetLoader.CreateGameObjectAsync(viewName, token);
            viewObj.SetActive(false);

            return View.Create(assetLoader, viewObj, viewName);
        }
    }
}
