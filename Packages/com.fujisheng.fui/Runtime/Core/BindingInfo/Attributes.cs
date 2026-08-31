using FUI.Binding;

using System;

namespace FUI.Binding
{
    /// <summary>
    /// 指定可观察属性由纯 Source Generator 或 Source Generator 与 ILPP 混合生成。
    /// </summary>
    public enum PropertyGenerationMode
    {
        Pure,
        Mixed,
    }

    /// <summary>
    /// 为当前程序集指定生成 Route Catalog 的命名空间。
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ViewModuleAttribute : Attribute
    {
        public string Namespace { get; }

        public PropertyGenerationMode PropertyMode { get; set; } = PropertyGenerationMode.Pure;

        public ViewModuleAttribute(string moduleNamespace)
        {
            if (string.IsNullOrWhiteSpace(moduleNamespace))
            {
                throw new ArgumentException("The module namespace cannot be empty.", nameof(moduleNamespace));
            }

            Namespace = moduleNamespace;
        }
    }

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
    /// 声明 ViewModel 的默认 View/资源提示，用于生成顶层 Route 和对应 BindingContext。
    /// 该特性不是一对一绑定限制；同一个 ViewModel 实例仍可投影到其他兼容 View 或嵌套子 View。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ViewContractAttribute : Attribute
    {
        /// <summary>
        /// 视图名字
        /// </summary>
        public string ViewName { get; }

        /// <summary>
        /// 资源路径
        /// </summary>
        public string AssetKey { get; }

        /// <summary>
        /// 创建一个视图绑定属性
        /// </summary>
        /// <param name="viewName">View名字 资源路径和View名字同名</param>
        public ViewContractAttribute(string viewName)
        {
            ViewName = viewName;
            AssetKey = viewName;
        }

        /// <summary>
        /// 创建一个视图绑定属性
        /// </summary>
        /// <param name="viewName">View名字</param>
        /// <param name="sourcePath">资源路径</param>
        public ViewContractAttribute(string viewName, string sourcePath)
        {
            ViewName = viewName;
            AssetKey = sourcePath;
        }
    }

    /// <summary>
    /// 标记一条绑定关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class BindAttribute : Attribute
    {
        /// <summary>目标 Element 路径。</summary>
        public string Target { get; }
        /// <summary>目标 Element 的可绑定属性名。</summary>
        public string PropertyName { get; }
        /// <summary>可选的双向值转换器类型。</summary>
        public Type ConverterType { get; }
        /// <summary>该绑定的数据流方向。</summary>
        public BindingMode Mode { get; }

        /// <summary>
        /// 绑定一个属性到View的某个属性
        /// </summary>
        /// <param name="target">目标View名</param>
        /// <param name="propertyName">目标属性名</param>
        /// <param name="converterType">值转换器类型</param>
        /// <param name="bindingMode">绑定类型</param>
        public BindAttribute(string target, string propertyName, Type converterType = null, BindingMode bindingMode = BindingMode.OneWay)
        {
            Target = target;
            PropertyName = propertyName;
            ConverterType = converterType;
            Mode = bindingMode;
        }
    }

    /// <summary>
    /// 可观察对象
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ObservableObjectAttribute : Attribute { }

    /// <summary>
    /// 可观察属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ObservablePropertyAttribute : Attribute { }

    /// <summary>
    /// 忽略某个属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class IgnoreObservableAttribute : Attribute { }

    /// <summary>
    /// 标记某个方法或委托为命令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class CommandAttribute : Attribute
    {
        public string Target { get; }
        public string CommandName { get; }

        public CommandAttribute(string target, string commandName)
        {
            Target = target;
            CommandName = commandName;
        }
    }

    /// <summary>
    /// 标记某个方法为属性变更通知方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class OnChangedAttribute : Attribute
    {
        public string PropertyName { get; }
        public OnChangedAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// 标记一个展示器默认的视图模型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PresenterForAttribute : Attribute
    {
        public Type ViewModelType { get; }
        public PresenterForAttribute(Type type)
        {
            if (!typeof(ObservableObject).IsAssignableFrom(type))
            {
                throw new Exception($"PresenterForAttribute type {type} is not ObservableObject");
            }
            ViewModelType = type;
        }
    }
}
