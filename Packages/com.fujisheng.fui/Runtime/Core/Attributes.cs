using System;

namespace FUI
{
    public enum BindingType
    {
        OneWay,
        TwoWay,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class BindingAttribute : Attribute
    {
        public readonly string target;
        public readonly string propertyName;
        public readonly Type valueConverterType;
        public readonly Type visualElementType;
        public readonly BindingType bindingType;

        /// <summary>
        /// 绑定一个属性到某个视觉元素 适用于可观察对象的属性
        /// </summary>
        /// <param name="target">目标 视图名或者视图某个字段名</param>
        /// <param name="converterType">值转换器类型</param>
        /// <param name="elementType">视觉元素类型</param>
        public BindingAttribute(string target, Type elementType = null, Type converterType = null, string propertyName = null, BindingType bindingType = BindingType.OneWay)
        {
            this.target = target;
            this.visualElementType = elementType;
            this.valueConverterType = converterType;
            this.propertyName = propertyName;
            this.bindingType = bindingType;
        }
    }

    /// <summary>
    /// 可观察对象
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ObservableObjectAttribute : Attribute { }

    /// <summary>
    /// 可观察属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ObservablePropertyAttribute : Attribute { }

    /// <summary>
    /// 忽略某个属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ObservablePropertyIgnoreAttribute : Attribute { }

    /// <summary>
    /// 标记某个方法为命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public readonly string target;
        public readonly Type valueConverterType;
        public readonly Type visualElementType;

        public CommandAttribute()
        {

        }

        public CommandAttribute(string target, Type valueConverterType, Type visualElementType)
        {
            this.target = target;
        }
    }

    /// <summary>
    /// 标记某个绑定上下文的视图模型类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ViewModelAttribute : Attribute
    {
        public readonly Type type;
        public ViewModelAttribute(Type type)
        {
            this.type = type;
        }
    }

    /// <summary>
    /// 标记某个绑定上下文的视图名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ViewAttribute : Attribute
    {
        public readonly string viewName;
        public ViewAttribute(string viewName)
        {
            this.viewName = viewName;
        }
    }
}
