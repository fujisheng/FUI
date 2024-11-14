using System;

namespace FUI
{
    /// <summary>
    /// 绑定模式
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// ViewModel->View
        /// </summary>
        OneWay = 1 << 0,

        /// <summary>
        /// View->ViewModel
        /// </summary>
        OneWayToSource = 1 << 1,

        /// <summary>
        /// ViewModel<->View
        /// </summary>
        TwoWay = OneWay | OneWayToSource,
    }

    /// <summary>
    /// 标记一条绑定关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class BindingAttribute : Attribute
    {
        public readonly string target;
        public readonly string propertyName;
        public readonly Type valueConverterType;   
        public readonly BindingMode bindingMode;

        /// <summary>
        /// 绑定某个ViewModel到某个View
        /// </summary>
        /// <param name="target">View名字</param>
        public BindingAttribute(string target)
        {
            this.target = target;
            this.propertyName = null;
            this.valueConverterType = null;
            this.bindingMode = BindingMode.OneWay;
        }

        /// <summary>
        /// 绑定一个属性到View的某个属性
        /// </summary>
        /// <param name="target">目标View名</param>
        /// <param name="propertyName">目标属性名</param>
        /// <param name="converterType">值转换器类型</param>
        /// <param name="bindingMode">绑定类型</param>
        public BindingAttribute(string target, string propertyName, Type converterType = null, BindingMode bindingMode = BindingMode.OneWay)
        {
            this.target = target;
            this.propertyName = propertyName;

            this.valueConverterType= null;
            this.bindingMode = BindingMode.OneWay;
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
    /// 标记某个方法或委托为命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public readonly string target;
        public readonly string commandName;

        public CommandAttribute(string target, string commandName)
        {
            this.target = target;
            this.commandName = commandName;
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
