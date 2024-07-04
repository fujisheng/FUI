using FUI.Bindable;

using System;
using System.Collections.Generic;

namespace FUI.Manager
{
    internal static class ViewConfigCache
    {
        static readonly Dictionary<Type, DefaultViewConfigAttribute> cache = new Dictionary<Type, DefaultViewConfigAttribute>();

        internal static DefaultViewConfigAttribute Get(ObservableObject viewModel)
        {
            var type = viewModel.GetType();
            if (cache.TryGetValue(type, out var config))
            {
                return config;
            }

            var attrs = type.GetCustomAttributes(typeof(DefaultViewConfigAttribute), false);
            if (attrs.Length > 0)
            {
                config = attrs[0] as DefaultViewConfigAttribute;
                cache.Add(type, config);
                return config;
            }

            return null;
        }
    }
}