using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FUI.Manager
{
    internal static class UIConfigResolver
    {
        static readonly Dictionary<Type, UIConfig> viewModelViewConfigLookup = new Dictionary<Type, UIConfig>();
        static readonly Dictionary<string, List<UIConfig>> viewViewConfigLookup = new Dictionary<string, List<UIConfig>>();

        static UIConfigResolver()
        {
            foreach(var contextType in BindingContextTypeResolver.Types)
            {
                var config = contextType.viewModelType.GetCustomAttribute<ConfigAttribute>(false)?.config ?? UIConfig.Default;
                viewModelViewConfigLookup.Add(contextType.viewModelType, config);

                if(string.IsNullOrEmpty(contextType.viewName))
                {
                    continue;
                }

                if(!viewViewConfigLookup.TryGetValue(contextType.viewName, out var configList))
                {
                    configList = new List<UIConfig>();
                    viewViewConfigLookup.Add(contextType.viewName, configList);
                }
                
                if(configList.Contains(config))
                {
                    continue;
                }
                configList.Add(config);
            }
        }

        internal static UIConfig Get(ObservableObject viewModel)
        {
            var type = viewModel.GetType();
            return Get(type);
        }

        internal static UIConfig Get(Type viewModelType)
        {
            if (viewModelViewConfigLookup.TryGetValue(viewModelType, out var config))
            {
                return config;
            }

            var attribute = viewModelType.GetCustomAttribute<ConfigAttribute>(false);
            config = attribute == null ? UIConfig.Default : attribute.config;
            viewModelViewConfigLookup.Add(viewModelType, config);
            return config;
        }

        /// <summary>
        /// 获取一个界面的默认配置
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <returns></returns>
        internal static UIConfig? GetDefault(string viewName)
        {
            if(viewViewConfigLookup.TryGetValue(viewName, out var configList))
            {
                return configList[0];
            }
            return null;
        }
    }
}