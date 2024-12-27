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
    public static class BindingContextTypeResolver
    {
        /// <summary>
        /// 绑定上下文类型信息
        /// </summary>
        public class BindingContextTypeInfo
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
        /// 存储视图名字和绑定上下文的映射
        /// </summary>
        static Dictionary<string, List<BindingContextTypeInfo>> viewContextLookup;

        /// <summary>
        /// 存储ViewModel类型和绑定上下文的映射
        /// </summary>
        static Dictionary<Type, List<BindingContextTypeInfo>> viewModelContextLookup;

        /// <summary>
        /// 存储ViewModel类型和视图行为的映射
        /// </summary>
        static Dictionary<Type, List<Type>> viewModelBehaviorLookup;

        /// <summary>
        /// 存储所有的绑定上下文类型
        /// </summary>
        static List<BindingContextTypeInfo> types;

        /// <summary>
        /// 所有绑定上下文类型信息
        /// </summary>
        public static IReadOnlyList<BindingContextTypeInfo> Types => types;

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
            viewModelContextLookup = new Dictionary<Type, List<BindingContextTypeInfo>>();
            viewModelBehaviorLookup = new Dictionary<Type, List<Type>>();
            types = new List<BindingContextTypeInfo>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    ResolveAllBindingContext(type);
                    ResolveViewModelDefaultBehavior(type);
                }
            }

            ResolveBaseType();
        }

        /// <summary>
        /// 解析绑定上下文类型信息
        /// </summary>
        static void ResolveAllBindingContext(Type type)
        {
            if(!typeof(BindingContext).IsAssignableFrom(type) || type.IsAbstract)
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
                }
                contextList.Add(typeInfo);
                viewContextLookup[viewAttribute.viewName] = contextList;
            }

            //构建viewModel类型和绑定上下文的映射
            if (!viewModelContextLookup.TryGetValue(viewModelAttribute.type, out var viewModelContexts))
            {
                viewModelContexts = new List<BindingContextTypeInfo>();
            }
            viewModelContexts.Add(typeInfo);
            viewModelContextLookup[viewModelAttribute.type] = viewModelContexts;
        }

        /// <summary>
        /// 解析ViewModel默认的Behavior
        /// </summary>
        static void ResolveViewModelDefaultBehavior(Type type)
        {
            if (!typeof(ViewBehavior).IsAssignableFrom(type) || type.IsAbstract)
            {
                return;
            }

            if (!type.BaseType.IsGenericType)
            {
                return;
            }

            var viewModelType = type.BaseType.GetGenericArguments()[0];
            if (!viewModelBehaviorLookup.TryGetValue(viewModelType, out var behaviorList))
            {
                behaviorList = new List<Type>();
            }
            behaviorList.Add(type);
            viewModelBehaviorLookup[viewModelType] = behaviorList;
        }

        /// <summary>
        /// 解析所有绑定上下文信息的基类型
        /// </summary>
        static void ResolveBaseType()
        {
            //设置基类型
            foreach (var contextType in types)
            {
                if (contextType.viewModelType.BaseType == typeof(ObservableObject) 
                    || contextType.viewModelType.BaseType == typeof(ViewModel))
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
        /// 仅根据视图名字获取上下文类型和视图模型类型
        /// 如果名字为空则返回false
        /// 如果这个ViewName有多个上下文, 则抛出异常需要指定视图模型
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <param name="contextType">绑定上下文类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <returns></returns>
        public static bool TryGetContextType(string viewName, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;

            if (string.IsNullOrEmpty(viewName))
            {
                return false;
            }

            //如果没有找到这个视图名所对应的绑定上下文, 则返回false
            if(!viewContextLookup.TryGetValue(viewName, out var contexts))
            {
                return false;
            }

            //如果只有一个上下文, 则直接使用这个上下文
            if(contexts.Count == 1)
            {
                contextType = contexts[0].contextType;
                viewModelType = contexts[0].viewModelType;
                return true;
            }

            //如果有多个上下文, 则抛出异常需要指定视图模型
            throw new Exception($"create view:{viewName} failed, there are multiple contexts, please assign viewModel");
        }

        /// <summary>
        /// 仅指定视图模型类型获取上下文类型和视图模型类型
        /// </summary>
        /// <param name="assignViewModelType">请求的ViewModel类型</param>
        /// <param name="contextType">返回的上下文类型</param>
        /// <returns></returns>
        public static bool TryGetContextType(Type assignViewModelType, out Type contextType, out string viewName)
        {
            contextType = null;
            viewName = null;

            if(assignViewModelType == null)
            {
                return false;
            }

            //如果找到对应视图模型的上下文, 则失败
            if (!viewModelContextLookup.TryGetValue(assignViewModelType, out var contexts))
            {
                return false;
            }

            //如果只有一个上下文, 则直接使用这个上下文
            if (contexts.Count == 1)
            {
                contextType = contexts[0].contextType;
                viewName = contexts[0].viewName;
                return true;
            }

            throw new Exception($"create view failed, there are multiple contexts, please select viewName");
        }

        /// <summary>
        /// 通过指定的视图模型类型和视图名字获取上下文类型 这里的视图名字是可以为空的
        /// </summary>
        /// <param name="assignViewModelType">请求的视图模型类型</param>
        /// <param name="viewName">viewName</param>
        /// <param name="contextType">游戏上下文类型</param>
        /// <returns></returns>
        public static bool TryGetContextType(Type assignViewModelType, string viewName, out Type contextType)
        {
            contextType = null;
            if(!viewModelContextLookup.TryGetValue(assignViewModelType, out var contexts))
            {
                return false;
            }

            contextType = contexts.FirstOrDefault(c => c.viewName == viewName)?.contextType;
            return contextType != null;
        }

        /// <summary>
        /// 尝试获取兼容的绑定上下文类型
        /// </summary>
        /// <param name="assignViewModelType">请求的上下文类型</param>
        /// <param name="viewName">界面名字</param>
        /// <param name="contextType">上下文类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool TryGetContextType(Type assignViewModelType, string viewName, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;

            //没有指定视图模型, 但是没有这个视图名字的上下文, 则抛出异常
            if (!viewContextLookup.TryGetValue(viewName, out var contexts) && assignViewModelType == null)
            {
                throw new Exception($"create view:{viewName} failed, cannot find any binding context");
            }

            //没有对应视图名所对应的上下文, 但是有对应视图模型的上下文
            if(contexts == null && viewModelContextLookup.TryGetValue(assignViewModelType, out var viewModelContexts))
            {
                if(viewModelContexts.Count == 1)
                {
                    contextType = viewModelContexts[0].contextType;
                    viewModelType = assignViewModelType;
                    return true;
                }

                throw new Exception($"create view:{viewName} failed, there are multiple contexts, please select viewModel");
            }

            //没有指定视图模型, 但是有多个这个viewName的上下文, 则抛出异常需要指定视图模型
            if (assignViewModelType == null && contexts.Count > 1)
            {
                throw new Exception($"create view:{viewName} failed, there are multiple contexts, please select viewModel");
            }

            //指定了视图模型, 但是没有找到对应的上下文, 则抛出异常
            if (assignViewModelType != null && contexts.Count == 1 && !contexts[0].viewModelType.IsAssignableFrom(assignViewModelType))
            {
                throw new Exception($"create view:{viewName} failed, cannot find {assignViewModelType} binding context");
            }

            //没有指定视图模型且只有一个上下文, 则直接使用这个上下文
            if (assignViewModelType == null && contexts.Count == 1)
            {
                contextType = contexts[0].contextType;
                viewModelType = contexts[0].viewModelType;
                return true;
            }

            //查找对应的上下文
            if(assignViewModelType != null)
            {
                foreach (var contextInfo in contexts)
                {
                    if (contextInfo.viewModelType.IsAssignableFrom(assignViewModelType))
                    {
                        contextType = contextInfo.contextType;
                        viewModelType = contextInfo.viewModelType;
                        return true;
                    }
                }
            }
            

            throw new Exception($"create view:{viewName} failed, cannot find binding context");
        }

        /// <summary>
        /// 尝试获取视图行为类型
        /// </summary>
        /// <param name="inputBehaviorType">指定的视图行为类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <param name="behaviorType">视图行为类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool TryGetBehaviorType(Type inputBehaviorType, Type viewModelType, out Type behaviorType)
        {
            behaviorType = null;
            if (!viewModelBehaviorLookup.TryGetValue(viewModelType, out var behaviors))
            {
                return false;
            }

            //没有指定视图行为, 但是有多个这个viewModelType的行为, 则抛出异常需要指定视图行为
            if (inputBehaviorType == null && behaviors.Count > 1)
            {
                throw new Exception($"create behavior:{viewModelType} failed, there are multiple behaviors, please select behavior");
            }

            //指定了视图行为, 但是没有找到对应的行为, 则抛出异常
            if (inputBehaviorType != null && behaviors.Count == 1 && !behaviors[0].IsAssignableFrom(inputBehaviorType))
            {
                throw new Exception($"create behavior:{viewModelType} failed, cannot find {inputBehaviorType} behavior");
            }

            //没有指定视图行为且只有一个行为, 则直接使用这个行为
            if (inputBehaviorType == null && behaviors.Count == 1)
            {
                behaviorType = behaviors[0];
                return true;
            }

            foreach (var behavior in behaviors)
            {
                if (inputBehaviorType != null && behavior.IsAssignableFrom(inputBehaviorType))
                {
                    behaviorType = behavior;
                    return true;
                }
            }
            return false;
        }
    }
}
