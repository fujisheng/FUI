using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;

namespace FUI.Editor.Inspection
{
    /// <summary>
    /// 收集生成器输出的绑定清单，并为编辑器检查工具提供按 ViewModel 与 View 查询的目录。
    /// </summary>
    internal sealed class BindingCatalog
    {
        /// <summary>
        /// 唯一标识一组 ViewModel 与 View 的绑定上下文。
        /// </summary>
        struct ContextKey : IEquatable<ContextKey>
        {
            public string viewModelName;
            public string viewName;

            public bool Equals(ContextKey other)
            {
                return viewModelName == other.viewModelName && viewName == other.viewName;
            }
        }

        static BindingCatalog instance;
        Dictionary<ContextKey, BindingManifest> contexts;
        public static BindingCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BindingCatalog();
                }
                return instance;
            }
        }

        BindingCatalog()
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
            contexts = new Dictionary<ContextKey, BindingManifest>();

            // 获取所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (assemblyName.EndsWith(".Generators") || assemblyName.Contains("SourceGenerator") || assemblyName.Contains("Analyzer"))
                {
                    continue;
                }

                Type[] assemblyTypes;
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    // Analyzer/SourceGenerator 的编译器依赖不会在 Editor 运行时加载。
                    assemblyTypes = Array.FindAll(exception.Types, type => type != null);
                }

                // 绑定清单是生成器供 Editor 使用的隐藏静态类型，不依赖固定项目命名空间。
                var types = assemblyTypes
                    .Where(t => t.IsClass && t.IsAbstract && t.IsSealed);

                foreach (var type in types)
                {
                    // 查找名为Info的静态属性
                    var infoProp = type.GetProperty("Info", BindingFlags.Public | BindingFlags.Static);
                    if (infoProp == null || infoProp.PropertyType != typeof(BindingManifest))
                    {
                        continue;
                    }

                    BindingManifest info;
                    try
                    {
                        info = infoProp.GetValue(null) as BindingManifest;
                    }
                    catch (Exception exception)
                    {
                        UnityEngine.Debug.LogWarning($"FUI could not read BindingManifest {type.FullName}: {exception.Message}");
                        continue;
                    }
                    if (info == null)
                    {
                        continue;
                    }

                    var key = new ContextKey { viewModelName = info.viewModelType, viewName = info.viewName };
                    contexts[key] = info;
                }
            }
        }

        /// <summary>
        /// 根据实例获取上下文信息
        /// </summary>
        /// <param name="instance">ui实例</param>
        /// <returns></returns>
        public BindingManifest GetContextInfo(ViewInstance instance)
        {
            if(instance == null)
            {
                return null;
            }

            if(contexts == null)
            {
                return null;
            }

            var bindingContextType = instance.BindingContext.GetType();
            var viewModelTypeName = bindingContextType.GetCustomAttribute<ViewModelAttribute>()?.Type.FullName;
            var viewName = bindingContextType.GetCustomAttribute<ViewAttribute>()?.ViewName;
            var key = new ContextKey { viewModelName = viewModelTypeName, viewName = viewName };
            if (contexts.TryGetValue(key, out var info))
            {
                return info;
            }
            return null;
        }

        public BindingManifest GetManifest(Type viewModelType, string viewName)
        {
            if (viewModelType == null || contexts == null)
            {
                return null;
            }

            var key = new ContextKey
            {
                viewModelName = viewModelType.FullName,
                viewName = viewName ?? string.Empty,
            };
            if (contexts.TryGetValue(key, out var manifest))
            {
                return manifest;
            }

            // Route.CreateViewVariant 会复用原绑定契约，但可以指向不同 AssetKey。
            var candidates = contexts
                .Where(pair => pair.Key.viewModelName == viewModelType.FullName)
                .Select(pair => pair.Value)
                .ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        public IReadOnlyList<BindingManifest> GetManifests(string viewName)
        {
            if (contexts == null)
            {
                return Array.Empty<BindingManifest>();
            }

            return contexts
                .Where(pair => pair.Key.viewName == (viewName ?? string.Empty))
                .Select(pair => pair.Value)
                .ToArray();
        }
    }
}
