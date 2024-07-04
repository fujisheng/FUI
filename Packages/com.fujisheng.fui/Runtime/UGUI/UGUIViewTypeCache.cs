using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FUI.UGUI
{
    /// <summary>
    /// View类型缓存
    /// </summary>
    internal static class UGUIViewTypeCache
    {
        internal struct BindingContextInfo
        {
            public Type contextType;
            public Type viewModelType;

            public BindingContextInfo(Type contextType, Type viewModelType)
            {
                this.contextType = contextType;
                this.viewModelType = viewModelType;
            }
        }

        static Dictionary<string, List<BindingContextInfo>> viewBindingContextMapping;
        static Dictionary<Type, List<Type>> viewModelBehaviorMapping;

        static UGUIViewTypeCache()
        {
            viewBindingContextMapping = new Dictionary<string, List<BindingContextInfo>>();
            viewModelBehaviorMapping = new Dictionary<Type, List<Type>>();
            LoadAllBindingContext();
            LoadViewModelDefaultBehavior();
        }

        static void LoadAllBindingContext()
        {
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var contexts = asm.GetTypes().Where(item=>typeof(FUI.BindingContext).IsAssignableFrom(item) && !item.IsAbstract);
                foreach (var context in contexts)
                {
                    var view = context.GetCustomAttribute<ViewAttribute>();
                    var viewModel = context.GetCustomAttribute<ViewModelAttribute>();
                    if(view == null || viewModel == null)
                    {
                        continue;
                    }

                    if(!viewBindingContextMapping.TryGetValue(view.viewName, out var contextList))
                    {
                        contextList = new List<BindingContextInfo>();
                    }
                    contextList.Add(new BindingContextInfo(context, viewModel.type));
                    viewBindingContextMapping[view.viewName] = contextList;
                }
            }
        }

        /// <summary>
        /// 加载ViewModel默认的Behavior
        /// </summary>
        static void LoadViewModelDefaultBehavior()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!typeof(ViewBehavior).IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }

                    if (!type.BaseType.IsGenericType)
                    {
                        continue;
                    }

                    var viewModelType = type.BaseType.GetGenericArguments()[0];
                    if(!viewModelBehaviorMapping.TryGetValue(viewModelType, out var behaviorList))
                    {
                        behaviorList = new List<Type>();
                    }
                    behaviorList.Add(type);
                    viewModelBehaviorMapping[viewModelType] = behaviorList;
                }
            }
        }

        /// <summary>
        /// 尝试根据一个界面名字获取默认的绑定上下文
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="types">可以绑定的上下文类型</param>
        /// <returns>是否有对应的类型</returns>
        public static bool TryGetBindingContexts(string viewName, out IReadOnlyList<BindingContextInfo> types)
        {
            types = null;
            if(!viewBindingContextMapping.TryGetValue(viewName, out var typeList))
            {
                return false;
            }

            types = typeList;
            return true;
        }

        /// <summary>
        /// 尝试获取一个ViewModel所对应的默认ViewBehavior
        /// </summary>
        /// <param name="viewModelType">ViewModel类型</param>
        /// <param name="behaviorTypes">默认ViewBehavior类型</param>
        /// <returns></returns>
        public static bool TryGetBehaviorTypes(Type viewModelType, out IReadOnlyList<Type> behaviorTypes)
        {
            behaviorTypes = null;
            if(!viewModelBehaviorMapping.TryGetValue(viewModelType, out var types))
            {
                return false;
            }
            behaviorTypes = types;
            return true;
        }
    }
}
