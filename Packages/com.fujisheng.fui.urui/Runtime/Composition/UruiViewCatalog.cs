using System;
using System.Collections.Generic;
using FUI.Navigation;
namespace FUI.Rendering.URUI
{
    /// <summary>每种路由 VM 唯一对应一个定义；不使用资源键或继承关系猜测页面。</summary>
    public sealed class UruiViewCatalog
    {
        readonly IReadOnlyDictionary<Type, UruiViewDefinition> definitions;
        public UruiViewCatalog(IEnumerable<KeyValuePair<Type, UruiViewDefinition>> registrations)
        {
            if (registrations == null) { throw new ArgumentNullException(nameof(registrations)); }
            var entries = new Dictionary<Type, UruiViewDefinition>();
            foreach (var entry in registrations)
            {
                if (entry.Key == null || entry.Value == null) { throw new ArgumentException("页面注册必须包含 VM 类型和定义。", nameof(registrations)); }
                if (entries.ContainsKey(entry.Key)) { throw new InvalidOperationException("重复的 URUI 页面定义：" + entry.Key.FullName); }
                entries.Add(entry.Key, entry.Value);
            }
            definitions = entries;
        }
        public IEnumerable<UruiViewDefinition> Definitions => definitions.Values;
        public bool Contains(Type viewModelType) => viewModelType != null && definitions.ContainsKey(viewModelType);
        public UruiViewDefinition Get(RouteDescriptor route)
        {
            if (route == null) { throw new ArgumentNullException(nameof(route)); }
            if (!definitions.TryGetValue(route.ViewModelType, out var definition))
            { throw new InvalidOperationException("缺少 URUI 页面定义：" + route.ViewModelType.FullName); }
            return definition;
        }
    }
}
