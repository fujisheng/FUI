using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FUI.UGUI
{
    public class UGUIViewCreator : IViewCreator
    {
        readonly List<Type> viewTypes;
        readonly Dictionary<string, Type> viewNameTypeMapping;
        readonly Dictionary<Type, Type> viewModelDefaultBehaviorMapping;
        readonly Func<IAssetLoader> assetLoaderCreator;

        public UGUIViewCreator(Func<IAssetLoader> assetLoaderCreator)
        {
            viewTypes = new List<Type>();
            viewNameTypeMapping = new Dictionary<string, Type>();
            viewModelDefaultBehaviorMapping = new Dictionary<Type, Type>();

            this.assetLoaderCreator = assetLoaderCreator;
        }

        public void Initialize()
        {
            LoadAllViewTypes();
            LoadViewModelDefaultBehavior();
        }

        public View CreateView(ViewCreateParam param, out ObservableObject viewModel, out ViewBehavior behavior)
        {
            viewModel = null;
            behavior = null;
            var hasView = TryGetViewType(param.viewName, out var viewType);
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
            if(behaviorType == null && viewModelDefaultBehaviorMapping.TryGetValue(viewModelType, out var bt))
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

        /// <summary>
        /// 加载所有View的类型
        /// </summary>
        void LoadAllViewTypes()
        {
            viewTypes.Clear();
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                viewTypes.AddRange(asm.GetTypes().Where(item => typeof(View).IsAssignableFrom(item) && !item.IsAbstract));
            }
        }

        /// <summary>
        /// 尝试根据一个界面名字获取对应的Type
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="type">界面对应的类型</param>
        /// <returns>是否有对应的类型</returns>
        bool TryGetViewType(string viewName, out Type type)
        {
            if (viewNameTypeMapping.TryGetValue(viewName, out type))
            {
                return true;
            }

            var viewTypeName = Utility.UI.GetViewTypeFullName(viewName);
            type = viewTypes.Find(item=>item.FullName == viewTypeName);
            return type != null;
        }

        /// <summary>
        /// 加载ViewModel默认的Behavior
        /// </summary>
        void LoadViewModelDefaultBehavior()
        {
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in asm.GetTypes())
                {
                    if(!typeof(ViewBehavior).IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }

                    var baseType = type.BaseType;
                    var viewModelType = baseType.GetGenericArguments()[0];
                    viewModelDefaultBehaviorMapping[viewModelType] = type;
                }
            }
        }
    }
}
