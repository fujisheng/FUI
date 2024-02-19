using FUI.Bindable;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.UGUI
{
    public class UGUIBuilder : IUIBuilder
    {
        readonly Func<IAssetLoader> assetLoaderCreator;

        public UGUIBuilder(Func<IAssetLoader> assetLoaderCreator)
        {
            this.assetLoaderCreator = assetLoaderCreator;
        }

        public IView BuildView(UIBuildParam param, out ObservableObject viewModel, out ViewBehavior behavior)
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
            var view = Activator.CreateInstance(viewType, viewModel, assetLoader, viewObj, param.viewName) as IView;
            behavior = Activator.CreateInstance(behaviorType) as ViewBehavior;
            return view;
        }

        public IView BuildView(UIBuildParam param, ObservableObject viewModel)
        {
            if(string.IsNullOrEmpty(param.viewName) || param.viewType == null || viewModel == null)
            {
                return null;
            }
            var assetLoader = assetLoaderCreator?.Invoke();
            var viewObj = assetLoader.CreateGameObject(param.viewName);
            var view = Activator.CreateInstance(param.viewType, viewModel, assetLoader, viewObj, param.viewName) as IView;
            return view;
        }

        public async Task<(IView, ObservableObject, ViewBehavior)> BuildViewAsync(UIBuildParam param, CancellationToken cancellationToken)
        {
            ObservableObject viewModel = null;
            ViewBehavior behavior = null;
            var hasView = UGUIViewTypeCache.TryGetViewType(param.viewName, out var viewType);
            UnityEngine.Debug.Log($"createView:{param.viewName}  viewType:{viewType}");
            if (!hasView)
            {
                return default;
            }

            if (assetLoaderCreator == null)
            {
                return default;
            }

            var viewModelType = param.viewModelType ?? viewType.GetCustomAttribute<DefaultViewModelAttribute>()?.type;
            UnityEngine.Debug.Log($"viewModelType:{viewModelType}");
            var behaviorType = param.viewBehaviorType;
            if (behaviorType == null && UGUIViewTypeCache.TryGetDefaultBehaviorType(viewModelType, out var bt))
            {
                behaviorType = bt;
            }
            UnityEngine.Debug.Log($"behaviorType:{behaviorType}");

            if (behaviorType == null)
            {
                return default;
            }

            var assetLoader = assetLoaderCreator?.Invoke();
            var viewObj = await assetLoader.CreateGameObjectAsync(param.viewName);
            viewModel = Activator.CreateInstance(viewModelType) as ObservableObject;
            var view = Activator.CreateInstance(viewType, viewModel, assetLoader, viewObj, param.viewName) as IView;
            behavior = Activator.CreateInstance(behaviorType) as ViewBehavior;
            return (view, viewModel, behavior);
        }
    }
}
