using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FUI
{
    /// <summary>
    /// View类型缓存
    /// </summary>
    internal static class ViewTypeCache
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
        static Dictionary<Type, List<Type>> viewModelBindingContextMapping;
        static Dictionary<Type, List<Type>> viewModelBehaviorMapping;

        static ViewTypeCache()
        {
            viewBindingContextMapping = new Dictionary<string, List<BindingContextInfo>>();
            viewModelBindingContextMapping = new Dictionary<Type, List<Type>>();
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

                    if (!string.IsNullOrEmpty(view.viewName))
                    {
                        if (!viewBindingContextMapping.TryGetValue(view.viewName, out var contextList))
                        {
                            contextList = new List<BindingContextInfo>();
                        }
                        contextList.Add(new BindingContextInfo(context, viewModel.type));
                        viewBindingContextMapping[view.viewName] = contextList;
                    }
                    
                    if(!viewModelBindingContextMapping.TryGetValue(viewModel.type, out var viewModelContexts))
                    {
                        viewModelContexts = new List<Type>();
                    }
                    viewModelContexts.Add(context);
                    viewModelBindingContextMapping[viewModel.type] = viewModelContexts;
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
        /// 尝试获取绑定上下文类型
        /// </summary>
        /// <param name="inputViewModelType">指定的上下文类型</param>
        /// <param name="viewName">界面名字</param>
        /// <param name="contextType">上下文类型</param>
        /// <param name="viewModelType">视图模型类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static bool TryGetContextType(Type inputViewModelType, string viewName, out Type contextType, out Type viewModelType)
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
                    contextType = viewModelContexts[0];
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
        internal static bool TryGetBehaviorType(Type inputBehaviorType, Type viewModelType, out Type behaviorType)
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
