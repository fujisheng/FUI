using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.UGUI
{
    public class UGUIViewFactory : IViewFactory
    {
        readonly Func<IAssetLoader> assetLoaderCreator;

        public UGUIViewFactory(Func<IAssetLoader> assetLoaderCreator)
        {
            this.assetLoaderCreator = assetLoaderCreator;
        }

        /// <summary>
        /// 确保AssetLoaderCreator不为空
        /// </summary>
        /// <exception cref="Exception"></exception>
        void EnsureAssetLoaderCreator()
        {
            if(assetLoaderCreator == null)
            {
                throw new Exception("UGUIBuilder assetLoaderCreator is null");
            }
        }

        public IView Create(string viewName)
        {
            EnsureAssetLoaderCreator();
            if (string.IsNullOrEmpty(viewName))
            {
                return null;
            }
            var assetLoader = assetLoaderCreator.Invoke();
            var viewObj = assetLoader.CreateGameObject(viewName);
            viewObj.SetActive(false);
            return UGUIView.Create(assetLoader, viewObj, viewName);
        }

        public async Task<IView> CreateAsync(string viewName, CancellationToken token)
        {
            EnsureAssetLoaderCreator();
            if (string.IsNullOrEmpty(viewName))
            {
                return null;
            }
            var assetLoader = assetLoaderCreator.Invoke();
            var viewObj = await assetLoader.CreateGameObjectAsync(viewName, token);
            viewObj.SetActive(false);

            return UGUIView.Create(assetLoader, viewObj, viewName);
        }
    }
}
