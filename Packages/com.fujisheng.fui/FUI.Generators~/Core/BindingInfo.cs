using System;
using System.Collections.Generic;

namespace FUI.Generators
{
    /// <summary>
    /// 绑定模式
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// 单向绑定：从ViewModel到View
        /// </summary>
        OneWay = 1,

        /// <summary>
        /// 从View到ViewModel
        /// </summary>
        OneWayToSource = 2,

        /// <summary>
        /// 双向绑定
        /// </summary>
        TwoWay = OneWay | OneWayToSource
    }

    /// <summary>
    /// 位置信息
    /// </summary>
    public class SourceLocation
    {
        public string path;
        public int line;
        public int column;
    }

    /// <summary>
    /// 属性信息
    /// </summary>
    public class SourcePropertyDescriptor
    {
        public string name;
        public string type;
        public bool isList;
        public SourceLocation? location;
    }

    /// <summary>
    /// 转换器信息
    /// </summary>
    public class ConverterDescriptor
    {
        public string type;
        public string sourceType;
        public string targetType;
        public SourceLocation? location;
    }

    /// <summary>
    /// 目标信息
    /// </summary>
    public class ElementTargetDescriptor
    {
        public string path;
        public string type;
        public string propertyType;
        public string propertyName;
        public string propertyValueType;
    }

    /// <summary>
    /// 属性绑定信息
    /// </summary>
    public class PropertyBindingDescriptor
    {
        public SourcePropertyDescriptor propertyInfo;
        public ConverterDescriptor? converterInfo;
        public ElementTargetDescriptor targetInfo;
        public BindingMode bindingMode;
    }

    /// <summary>
    /// 命令信息
    /// </summary>
    public class CommandDescriptor
    {
        public string name;
        public List<string> parameters;
        public bool isEvent;
        public string returnType;
        public bool acceptsCancellationToken;
        public SourceLocation? location;
    }

    /// <summary>
    /// 描述命令绑定的目标 Element、成员及参数类型。
    /// </summary>
    public class CommandTargetDescriptor
    {
        /// <summary>
        /// 目标路径
        /// </summary>
        public string path;

        /// <summary>
        /// 类型
        /// </summary>
        public string type;

        /// <summary>
        /// 目标属性名字
        /// </summary>
        public string propertyName;

        /// <summary>
        /// 参数类型
        /// </summary>
        public List<string> parameters;
    }

    /// <summary>
    /// 命令绑定信息
    /// </summary>
    public class CommandBindingDescriptor
    {
        public CommandDescriptor commandInfo;
        public CommandTargetDescriptor targetInfo;
    }

    /// <summary>
    /// 绑定上下文信息
    /// </summary>
    public class BindingManifest
    {
        public string viewModelType;
        public string viewModelNamespace;
        public string viewName;
        public string baseViewModelType;
        public string baseViewModelNamespace;
        public string baseViewName;
        public List<PropertyBindingDescriptor> properties = new List<PropertyBindingDescriptor>();
        public List<CommandBindingDescriptor> commands = new List<CommandBindingDescriptor>();
    }
}
