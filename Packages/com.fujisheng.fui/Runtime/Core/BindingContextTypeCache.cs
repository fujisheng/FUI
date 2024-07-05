using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FUI
{
    /// <summary>
    /// View类型缓存
    /// </summary>
    public static class BindingContextTypeCache
    {
        /// <summary>
        /// 绑定上下文类型
        /// </summary>
        public class BindingContextType
        {
            public readonly Type contextType;
            public readonly Type viewModelType;
            public readonly string viewName;
            readonly List<BindingContextType> baseTypes;
            readonly List<BindingContextType> subTypes;

            /// <summary>
            /// 所有的基类型
            /// </summary>
            public IReadOnlyList<BindingContextType> BaseTypes => baseTypes;

            /// <summary>
            /// 所有的子类型
            /// </summary>
            public IReadOnlyList<BindingContextType> SubTypes => subTypes;

            public BindingContextType(string viewName, Type viewModelType, Type contextType)
            {
                this.viewName = viewName;
                this.viewModelType = viewModelType;
                this.contextType = contextType;
                baseTypes = new List<BindingContextType>();
                subTypes = new List<BindingContextType>();
            }

            /// <summary>
            /// 添加一个基类型
            /// </summary>
            /// <param name="baseType">基类型</param>
            public void AddBaseType(BindingContextType baseType)
            {
                if(!baseTypes.Contains(baseType))
                {
                    baseTypes.Add(baseType);
                }

                if(!baseType.subTypes.Contains(this))
                {
                    baseType.subTypes.Add(this);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="bindingContextType"></param>
            /// <returns></returns>
            public bool IsAssignableFrom(BindingContextType bindingContextType)
            {
                return this.viewModelType.IsAssignableFrom(bindingContextType.viewModelType);
            }
        }

        /// <summary>
        /// 存储视图名字和绑定上下文的映射
        /// </summary>
        static Dictionary<string, List<BindingContextType>> viewBindingContextMapping;

        /// <summary>
        /// 存储ViewModel类型和绑定上下文的映射
        /// </summary>
        static Dictionary<Type, List<BindingContextType>> viewModelBindingContextMapping;

        /// <summary>
        /// 存储ViewModel类型和视图行为的映射
        /// </summary>
        static Dictionary<Type, List<Type>> viewModelBehaviorMapping;

        /// <summary>
        /// 存储所有的绑定上下文类型
        /// </summary>
        static List<BindingContextType> bindingContexts;
        public static IReadOnlyList<BindingContextType> BindingContexts => bindingContexts;

        static BindingContextTypeCache()
        {
            viewBindingContextMapping = new Dictionary<string, List<BindingContextType>>();
            viewModelBindingContextMapping = new Dictionary<Type, List<BindingContextType>>();
            viewModelBehaviorMapping = new Dictionary<Type, List<Type>>();
            bindingContexts = new List<BindingContextType>();
            LoadAllBindingContext();
            LoadViewModelDefaultBehavior();
        }

        /// <summary>
        /// 加载所有绑定上下文
        /// </summary>
        static void LoadAllBindingContext()
        {
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var bindingContextTypes = asm.GetTypes().Where(item=>typeof(FUI.BindingContext).IsAssignableFrom(item) && !item.IsAbstract);
                foreach (var bindingContextType in bindingContextTypes)
                {
                    var viewAttribute = bindingContextType.GetCustomAttribute<ViewAttribute>();
                    var viewModelAttribute = bindingContextType.GetCustomAttribute<ViewModelAttribute>();
                    if(viewAttribute == null || viewModelAttribute == null)
                    {
                        continue;
                    }

                    var typeInfo = new BindingContextType(viewAttribute.viewName, viewModelAttribute.type, bindingContextType);

                    if (!string.IsNullOrEmpty(viewAttribute.viewName))
                    {
                        if (!viewBindingContextMapping.TryGetValue(viewAttribute.viewName, out var contextList))
                        {
                            contextList = new List<BindingContextType>();
                        }
                        contextList.Add(typeInfo);
                        viewBindingContextMapping[viewAttribute.viewName] = contextList;
                    }
                    
                    if(!viewModelBindingContextMapping.TryGetValue(viewModelAttribute.type, out var viewModelContexts))
                    {
                        viewModelContexts = new List<BindingContextType>();
                    }
                    viewModelContexts.Add(typeInfo);
                    viewModelBindingContextMapping[viewModelAttribute.type] = viewModelContexts;
                }
            }

            //设置基类型
            foreach(var contextType in bindingContexts)
            {
                if (contextType.viewModelType.BaseType == typeof(ObservableObject) || contextType.viewModelType.BaseType == typeof(ViewModel))
                {
                    continue;
                }

                foreach (var baseType in bindingContexts)
                {
                    if(contextType.viewModelType.BaseType == baseType.viewModelType)
                    {
                        contextType.AddBaseType(baseType);
                    }
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
        /// 仅根据视图名字获取上下文类型和视图模型类型
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
            if(!viewBindingContextMapping.TryGetValue(viewName, out var contexts))
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
        /// <param name="assignViewModelType"></param>
        /// <param name="contextType"></param>
        /// <param name="viewModelType"></param>
        /// <returns></returns>
        public static bool TryGetContextType(Type assignViewModelType, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;

            //如果找到对应视图模型的上下文, 则直接使用这个上下文
            if (!viewModelBindingContextMapping.TryGetValue(assignViewModelType, out var contexts))
            {
                return false;
            }

            contextType = contexts[0].contextType;
            viewModelType = assignViewModelType;
            return true;
        }

        /// <summary>
        /// 尝试获取绑定上下文类型
        /// </summary>
        /// <param name="inputViewModelType">指定的上下文类型</param>
        /// <param name="viewName">界面名字</param>
        /// <param name="contextType">上下文类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool TryGetContextType(Type inputViewModelType, string viewName, out Type contextType, out Type viewModelType)
        {
            contextType = null;
            viewModelType = null;

            //没有指定视图模型, 但是没有这个视图名字的上下文, 则抛出异常
            if (!viewBindingContextMapping.TryGetValue(viewName, out var contexts) && inputViewModelType == null)
            {
                throw new Exception($"create view:{viewName} failed, cannot find any binding context");
            }

            //没有对应视图名所对应的上下文, 但是有对应视图模型的上下文
            if(contexts == null && viewModelBindingContextMapping.TryGetValue(inputViewModelType, out var viewModelContexts))
            {
                if(viewModelContexts.Count == 1)
                {
                    contextType = viewModelContexts[0].contextType;
                    viewModelType = inputViewModelType;
                    return true;
                }

                throw new Exception($"create view:{viewName} failed, there are multiple contexts, please select viewModel");
            }

            //没有指定视图模型, 但是有多个这个viewName的上下文, 则抛出异常需要指定视图模型
            if (inputViewModelType == null && contexts.Count > 1)
            {
                throw new Exception($"create view:{viewName} failed, there are multiple contexts, please select viewModel");
            }

            //指定了视图模型, 但是没有找到对应的上下文, 则抛出异常
            if (inputViewModelType != null && contexts.Count == 1 && !contexts[0].viewModelType.IsAssignableFrom(inputViewModelType))
            {
                throw new Exception($"create view:{viewName} failed, cannot find {inputViewModelType} binding context");
            }

            //没有指定视图模型且只有一个上下文, 则直接使用这个上下文
            if (inputViewModelType == null && contexts.Count == 1)
            {
                contextType = contexts[0].contextType;
                viewModelType = contexts[0].viewModelType;
                return true;
            }

            //查找对应的上下文
            foreach (var contextInfo in contexts)
            {
                if (inputViewModelType != null && contextInfo.viewModelType.IsAssignableFrom(inputViewModelType))
                {
                    contextType = contextInfo.contextType;
                    viewModelType = contextInfo.viewModelType;
                    return true;
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
            if (!viewModelBehaviorMapping.TryGetValue(viewModelType, out var behaviors))
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
