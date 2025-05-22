using FUI.Bindable;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace FUI.Manager
{
    internal static class UISettingsResolver
    {
        static readonly Dictionary<Type, UISettings> viewModelViewConfigLookup = new Dictionary<Type, UISettings>();
        static readonly Dictionary<string, List<UISettings>> viewViewConfigLookup = new Dictionary<string, List<UISettings>>();

        static UISettingsResolver()
        {
            foreach(var contextType in BindingContextTypeResolver.Types)
            {
                var settings = contextType.viewModelType.GetCustomAttribute<SettingsAttribute>(false)?.settings ?? UISettings.Default;
                viewModelViewConfigLookup.Add(contextType.viewModelType, settings);

                if(string.IsNullOrEmpty(contextType.viewName))
                {
                    continue;
                }

                if(!viewViewConfigLookup.TryGetValue(contextType.viewName, out var configList))
                {
                    configList = new List<UISettings>();
                    viewViewConfigLookup.Add(contextType.viewName, configList);
                }
                
                if(configList.Contains(settings))
                {
                    continue;
                }
                configList.Add(settings);
            }
        }

        internal static UISettings Get(ObservableObject viewModel)
        {
            var type = viewModel.GetType();
            return Get(type);
        }

        internal static UISettings Get(Type viewModelType)
        {
            if (viewModelViewConfigLookup.TryGetValue(viewModelType, out var config))
            {
                return config;
            }

            var attribute = viewModelType.GetCustomAttribute<SettingsAttribute>(false);
            config = attribute == null ? UISettings.Default : attribute.settings;
            viewModelViewConfigLookup.Add(viewModelType, config);
            return config;
        }

        /// <summary>
        /// 获取一个界面的默认配置
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <returns></returns>
        internal static UISettings? GetDefault(string viewName)
        {
            if(viewViewConfigLookup.TryGetValue(viewName, out var configList))
            {
                return configList[0];
            }
            return null;
        }
    }
}