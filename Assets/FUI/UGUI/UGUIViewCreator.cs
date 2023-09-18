using FUI.Bindable;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.UGUI
{
    public class UGUIViewCreator : IViewCreator
    {
        readonly Func<IAssetLoader> assetLoaderCreator;

        public UGUIViewCreator(Func<IAssetLoader> assetLoaderCreator)
        {
            this.assetLoaderCreator = assetLoaderCreator;
        }

        public View CreateView(ViewCreateParam param, out ObservableObject viewModel, out ViewBehavior behavior)
        {
            viewModel = null;
            behavior = null;
            var hasView = UGUIViewTypeCache.TryGetViewType(param.viewName, out var viewType);
            UnityEngine.Debug.Log($"createView:{param.viewName}  viewType:{viewType}");
            if (!hasView)
            {
                return null;
            }

            if(assetLoaderCreator == null)
            {
                return null;
            }

            var viewModelType = param.viewModelType ?? viewType.GetCustomAttribute<DefaultViewModelAttribute>()?.type;
            UnityEngine.Debug.Log($"viewModelType:{viewModelType}");
            var behaviorType = param.viewBehaviorType;
            if(behaviorType == null && UGUIViewTypeCache.TryGetDefaultBehaviorType(viewModelType, out var bt))
            {
                behaviorType = bt;
            }
            UnityEngine.Debug.Log($"behaviorType:{behaviorType}");

            if(behaviorType == null)
            {
                return null;
            }

            var assetLoader = assetLoaderCreator?.Invoke();
            var viewObj = assetLoader.CreateGameObject(param.viewName);
            viewModel = Activator.CreateInstance(viewModelType) as ObservableObject;
            var view = Activator.CreateInstance(viewType, viewModel, assetLoader, viewObj, param.viewName) as View;
            behavior = Activator.CreateInstance(behaviorType) as ViewBehavior;
            return view;
        }

        public Task<View> CreateViewAsync(ViewCreateParam param, CancellationToken cancellationToken, out ObservableObject viewModel, out ViewBehavior behavior)
        {
            throw new NotImplementedException();
        }
    }
}
