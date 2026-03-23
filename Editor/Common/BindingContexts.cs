using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FUI.Editor
{
    /// <summary>
    /// 所有的UI绑定上下文信息
    /// </summary>
    public class BindingContexts
    {
        struct ContextKey : IEquatable<ContextKey>
        {
            public string viewModelName;
            public string viewName;

            public bool Equals(ContextKey other)
            {
                return viewModelName == other.viewModelName && viewName == other.viewName;
            }
        }

        static BindingContexts instance;
        Dictionary<ContextKey, ContextBindingInfo> contexts;
        public static BindingContexts Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BindingContexts();
                }
                return instance;
            }
        }

        BindingContexts()
        {
            LoadAllBindingContextInfo();
        }

        [AssemblyCompilationFinished]
        static void OnCompilationComplete(string file, List<object> messages)
        {
            if(instance == null)
            {
                return;
            }

            instance.LoadAllBindingContextInfo();
        }

        void LoadAllBindingContextInfo()
        {
            contexts = new Dictionary<ContextKey, ContextBindingInfo>();

            // 获取所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                // 获取FUI.ContextInfo命名空间下的所有静态类
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "FUI.Generated.ContextInfo");

                foreach (var type in types)
                {
                    // 查找名为Info的静态属性
                    var infoProp = type.GetProperty("Info", BindingFlags.Public | BindingFlags.Static);
                    if (infoProp == null)
                    {
                        continue;
                    }

                    var info = infoProp.GetValue(null) as ContextBindingInfo;
                    var key = new ContextKey { viewModelName = info.viewModelType, viewName = info.viewName };
                    contexts[key] = info;
                }
            }
        }

        /// <summary>
        /// 根据实体获取上下文信息
        /// </summary>
        /// <param name="entity">ui实体</param>
        /// <returns></returns>
        public ContextBindingInfo GetContextInfo(UIEntity entity)
        {
            if(entity == null)
            {
                return null;
            }

            if(contexts == null)
            {
                return null;
            }

            var bindingContextType = entity.BindingContext.GetType();
            var viewModelTypeName = bindingContextType.GetCustomAttribute<ViewModelAttribute>()?.type.FullName;
            var viewName = bindingContextType.GetCustomAttribute<ViewAttribute>()?.viewName;
            var key = new ContextKey { viewModelName = viewModelTypeName, viewName = viewName };
            if (contexts.TryGetValue(key, out var info))
            {
                return info;
            }
            return null;
        }
    }
}