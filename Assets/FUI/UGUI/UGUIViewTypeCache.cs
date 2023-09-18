using System;
using System.Collections.Generic;
using System.Linq;

namespace FUI.UGUI
{
    /// <summary>
    /// View类型缓存
    /// </summary>
    internal static class UGUIViewTypeCache
    {
        const string viewTypeNameFormat = "__DataBindingGenerated.__{0}_Binding_Generated";
        static List<Type> viewTypes;
        static Dictionary<string, Type> viewNameTypeMapping;
        static Dictionary<Type, Type> viewModelDefaultBehaviorMapping;

        static UGUIViewTypeCache()
        {
            viewTypes = new List<Type>();
            viewNameTypeMapping= new Dictionary<string, Type>();
            viewModelDefaultBehaviorMapping = new Dictionary<Type, Type>();
            LoadAllViewTypes();
            LoadViewModelDefaultBehavior();
        }

        /// <summary>
        /// 加载所有View的类型
        /// </summary>
        static void LoadAllViewTypes()
        {
            viewTypes.Clear();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                viewTypes.AddRange(asm.GetTypes().Where(item => typeof(View).IsAssignableFrom(item) && !item.IsAbstract));
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

                    var baseType = type.BaseType;
                    var viewModelType = baseType.GetGenericArguments()[0];
                    viewModelDefaultBehaviorMapping[viewModelType] = type;
                }
            }
        }

        /// <summary>
        /// 尝试根据一个界面名字获取对应的Type
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="type">界面对应的类型</param>
        /// <returns>是否有对应的类型</returns>
        public static bool TryGetViewType(string viewName, out Type type)
        {
            if (viewNameTypeMapping.TryGetValue(viewName, out type))
            {
                return true;
            }

            var viewTypeName = string.Format(viewTypeNameFormat, viewName);
            type = viewTypes.Find(item => item.FullName == viewTypeName);
            return type != null;
        }

        /// <summary>
        /// 尝试获取一个ViewModel所对应的默认ViewBehavior
        /// </summary>
        /// <param name="viewModelType">ViewModel类型</param>
        /// <param name="defaultBehaviorType">默认ViewBehavior类型</param>
        /// <returns></returns>
        public static bool TryGetDefaultBehaviorType(Type viewModelType, out Type defaultBehaviorType)
        {
            return viewModelDefaultBehaviorMapping.TryGetValue(viewModelType, out defaultBehaviorType);
        }
    }
}
