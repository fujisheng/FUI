using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FUI
{
    /// <summary>
    /// 绑定上下文类型解析器
    /// </summary>
    internal static class BindingContextTypeResolver
    {
        /// <summary>
        /// 绑定上下文类型信息
        /// </summary>
        internal class BindingContextTypeInfo
        {
            /// <summary>
            /// 上下文类型
            /// </summary>
            public readonly Type contextType;

            /// <summary>
            /// viewModel类型
            /// </summary>
            public readonly Type viewModelType;

            /// <summary>
            /// view名字
            /// </summary>
            public readonly string viewName;

            /// <summary>
            /// 这个类型所对应的基类型 取决于viewModelType的基类型
            /// </summary>
            readonly List<BindingContextTypeInfo> baseTypes;

            /// <summary>
            /// 所有的基类型信息
            /// </summary>
            public IReadOnlyList<BindingContextTypeInfo> BaseTypes => baseTypes;


            internal BindingContextTypeInfo(string viewName, Type viewModelType, Type contextType)
            {
                this.viewName = viewName;
                this.viewModelType = viewModelType;
                this.contextType = contextType;
                baseTypes = new List<BindingContextTypeInfo>();
            }

            /// <summary>
            /// 添加一个基类型
            /// </summary>
            /// <param name="baseType">基类型</param>
            internal void AddBaseType(BindingContextTypeInfo baseType)
            {
                if(!baseTypes.Contains(baseType))
                {
                    baseTypes.Add(baseType);
                }
            }

            public override int GetHashCode()
            {
                var hash = contextType.GetHashCode() ^ viewModelType.GetHashCode();
                if (!string.IsNullOrEmpty(viewName))
                {
                    hash ^= viewName.GetHashCode();
                }
                return hash;
            }

            public override bool Equals(object obj)
            {
                if(obj is BindingContextTypeInfo other)
                {
                    return contextType == other.contextType && viewModelType == other.viewModelType && viewName == other.viewName;
                }
                return false;
            }
        }

        /// <summary>
        /// 展示器类型信息
        /// </summary>
        class ViewPresenterTypeInfo
        {
            public readonly bool isDefault;
            public readonly Type presenterType;

            public ViewPresenterTypeInfo(bool isDefault, Type presenterType)
            {
                this.isDefault = isDefault;
                this.presenterType = presenterType;
            }
        }


        /// <summary>
        /// 存储视图名字和绑定上下文的映射
        /// </summary>
        static Dictionary<string, List<BindingContextTypeInfo>> viewContextLookup;
        /// <summary>
        /// 存储ViewModel类型和绑定上下文的映射
        /// </summary>
        static Dictionary<Type, BindingContextTypeInfo> viewModelContextLookup;
        /// <summary>
        /// 存储ViewModel类型和展示器的映射
        /// </summary>
        static Dictionary<Type, List<ViewPresenterTypeInfo>> viewModelPresenterLookup;
        /// <summary>
        /// 存储所有的绑定上下文类型
        /// </summary>
        static List<BindingContextTypeInfo> types;

        /// <summary>
        /// 所有绑定上下文类型信息
        /// </summary>
        internal static IReadOnlyList<BindingContextTypeInfo> Types => types;

        static BindingContextTypeResolver()
        {
            ResolveTypes();
        }

        /// <summary>
        /// 解析所有类型
        /// </summary>
        public static void ResolveTypes()
        {
            viewContextLookup = new Dictionary<string, List<BindingContextTypeInfo>>();
            viewModelContextLookup = new Dictionary<Type,BindingContextTypeInfo>();
            viewModelPresenterLookup = new Dictionary<Type, List<ViewPresenterTypeInfo>>();
            types = new List<BindingContextTypeInfo>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    ResolveAllBindingContext(type);
                    ResolveViewModelDefaultPresenter(type);
                }
            }
            ResolveBaseType();
        }

        /// <summary>
        /// 解析绑定上下文类型信息
        /// </summary>
        static void ResolveAllBindingContext(Type type)
        {
            if (!typeof(IBindingContext).IsAssignableFrom(type) || type.IsAbstract)
            {
                return;
            }

            var viewAttribute = type.GetCustomAttribute<ViewAttribute>();
            var viewModelAttribute = type.GetCustomAttribute<ViewModelAttribute>();
            if (viewAttribute == null || viewModelAttribute == null)
            {
                return;
            }

            var typeInfo = new BindingContextTypeInfo(viewAttribute.viewName, viewModelAttribute.type, type);
            types.Add(typeInfo);

            //构建view名字和绑定上下文的映射
            if (!string.IsNullOrEmpty(viewAttribute.viewName))
            {
                if (!viewContextLookup.TryGetValue(viewAttribute.viewName, out var contextList))
                {
                    contextList = new List<BindingContextTypeInfo>();
                    viewContextLookup.Add(viewAttribute.viewName, contextList);
                }
                contextList.Add(typeInfo);
            }
            viewModelContextLookup[viewModelAttribute.type] = typeInfo;
        }

        /// <summary>
        /// 解析ViewModel默认的Presenter
        /// </summary>
        static void ResolveViewModelDefaultPresenter(Type type)
        {
            if (!typeof(IPresenter).IsAssignableFrom(type) || type.IsAbstract || type == typeof(EmptyPresenter))
            {
                return;
            }

            void TryAddPresenter(Type viewModelType, bool isDefault)
            {
                if (!viewModelPresenterLookup.TryGetValue(viewModelType, out var presenterList))
                {
                    presenterList = new List<ViewPresenterTypeInfo>();
                    viewModelPresenterLookup.Add(viewModelType, presenterList);
                }
                if(presenterList.Exists(item=>item.presenterType == type))
                {
                    return;
                }

                if(isDefault)
                {
                    //如果是默认的展示器类型 则将其插入到第一个位置 方便后续访问
                    presenterList.Insert(0, new ViewPresenterTypeInfo(true, type));
                }
                else
                {
                    presenterList.Add(new ViewPresenterTypeInfo(false, type));
                }
            }

            var presenterViewModelAttr = type.GetCustomAttribute<PresenterViewModelAttribute>();
            if (presenterViewModelAttr != null && presenterViewModelAttr.type != null)
            {
                Logger.Instance.Log($"add default :{presenterViewModelAttr.type}  type:{type}");
                TryAddPresenter(presenterViewModelAttr.type, true);
            }

            foreach(var @interface in type.GetInterfaces())
            {
                if(@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IPresenter<>))
                {
                    var viewModelType = @interface.GenericTypeArguments[0];
                    TryAddPresenter(viewModelType, false);
                    break;
                }
            }
        }

        /// <summary>
        /// 解析所有绑定上下文信息的基类型
        /// </summary>
        static void ResolveBaseType()
        {
            foreach (var contextType in types)
            {
                if (contextType.viewModelType.BaseType == typeof(ObservableObject) ||
                    contextType.viewModelType.BaseType == typeof(ViewModel))
                {
                    continue;
                }

                var baseType = types.FirstOrDefault(t => t.viewModelType == contextType.viewModelType.BaseType);
                if(baseType == null)
                {
                    continue;
                }
                contextType.AddBaseType(baseType);
            }
        }

        /// <summary>
        /// 通过view名字获取绑定上下文类型  如何存在多个则提示需要指定viewModel
        /// </summary>
        public static bool TryGetContextType(string viewName, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;
            if (string.IsNullOrEmpty(viewName))
            {
                return false;
            }

            if(!viewContextLookup.TryGetValue(viewName, out var contexts) || contexts == null || contexts.Count == 0)
            {
                return false;
            }

            if (contexts.Count == 1)
            {
                contextType = contexts[0].contextType;
                viewModelType = contexts[0].viewModelType;
                return true;
            }

            Logger.Instance.LogError($"create view [{viewName}] failed, there are multiple contexts, please assign viewModel");
            return false;
        }

        /// <summary>
        /// 尝试通过ViewModel类型获取绑定上下文类型 (若该类型无专属上下文則向上查找兼容的基类上下文)
        /// </summary>
        public static bool TryGetContextType(Type assignViewModelType, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;
            if (assignViewModelType == null)
            {
                return false;
            }

            if (viewModelContextLookup.TryGetValue(assignViewModelType, out var directContext))
            {
                contextType = directContext.contextType;
                viewModelType = directContext.viewModelType;
                return true;
            }

            var baseType = assignViewModelType.BaseType;
            while (baseType != null && baseType != typeof(ViewModel) && baseType != typeof(ObservableObject) && baseType != typeof(object))
            {
                if(TryGetContextType(baseType, out contextType, out viewModelType))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// 通过指定的视图模型类型和视图名字获取上下文类型
        /// </summary>
        public static bool TryGetContextType(Type assignViewModelType, string viewName, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;

            if(assignViewModelType != null && TryGetContextType(assignViewModelType, out contextType, out viewModelType))
            {
                return true;
            }

            if(TryGetContextType(viewName, out contextType, out viewModelType))
            {
                return true;
            }

            Logger.Instance.LogError($"create view [{viewName}] failed, cannot find binding context");
            return false;
        }

        /// <summary>
        /// 尝试获取展示器类型
        /// </summary>
        /// <param name="assignPresenterType">指定的展示器类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="presenterType">展示器类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool TryGetPresenterType(Type assignPresenterType, Type viewModelType, out Type presenterType)
        {
            presenterType = null;
            if(assignPresenterType != null)
            {
                presenterType = assignPresenterType;
                return true;
            }

            if (!viewModelPresenterLookup.TryGetValue(viewModelType, out var presenterInfos) || presenterInfos.Count <= 0)
            {
                return false;
            }

            if(presenterInfos.Count == 1)
            {
                presenterType = presenterInfos[0].presenterType;
                return true;
            }

            //存在多个的时候 寻找默认的展示器类型
            var first = presenterInfos[0];
            var second = presenterInfos[1];
            if (first.isDefault && !second.isDefault)
            {
                presenterType = first.presenterType;
                return true;
            }

            Logger.Instance.LogError($"create presenter [{viewModelType}] failed, there are multiple presenters, please assign presenter");
            return false;
        }
    }
}
