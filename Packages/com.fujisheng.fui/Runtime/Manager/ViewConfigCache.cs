using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FUI.Manager
{
    internal static class ViewConfigCache
    {
        static readonly Dictionary<Type, ViewConfig> viewModelViewConfigLookup = new Dictionary<Type, ViewConfig>();
        static readonly Dictionary<string, List<ViewConfig>> viewViewConfigLookup = new Dictionary<string, List<ViewConfig>>();

        static ViewConfigCache()
        {
            foreach(var contextType in BindingContextTypeCache.BindingContexts)
            {
                var config = contextType.viewModelType.GetCustomAttribute<DefaultViewConfigAttribute>(false)?.config ?? ViewConfig.Default;
                viewModelViewConfigLookup.Add(contextType.viewModelType, config);

                if(string.IsNullOrEmpty(contextType.viewName))
                {
                    continue;
                }

                if(!viewViewConfigLookup.TryGetValue(contextType.viewName, out var configList))
                {
                    configList = new List<ViewConfig>();
                    viewViewConfigLookup.Add(contextType.viewName, configList);
                }
                
                if(configList.Contains(config))
                {
                    continue;
                }
                configList.Add(config);
            }
        }

        internal static ViewConfig Get(ObservableObject viewModel)
        {
            var type = viewModel.GetType();
            return Get(type);
        }

        internal static ViewConfig Get(Type viewModelType)
        {
            if (viewModelViewConfigLookup.TryGetValue(viewModelType, out var config))
            {
                return config;
            }

            var attribute = viewModelType.GetCustomAttribute<DefaultViewConfigAttribute>(false);
            config = attribute == null ? ViewConfig.Default : attribute.config;
            viewModelViewConfigLookup.Add(viewModelType, config);
            return config;
        }

        /// <summary>
        /// 获取一个界面的默认配置
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <returns></returns>
        internal static ViewConfig? GetDefault(string viewName)
        {
            if(viewViewConfigLookup.TryGetValue(viewName, out var configList))
            {
                return configList[0];
            }
            return null;
        }
    }
}