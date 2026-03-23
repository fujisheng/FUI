using System;
using System.Collections.Generic;

namespace FUI.SourceGenerator
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
    public class LocationInfo
    {
        public string path;
        public int line;
        public int column;
    }

    /// <summary>
    /// 属性信息
    /// </summary>
    public class PropertyInfo
    {
        public string name;
        public string type;
        public bool isList;
        public LocationInfo? location;
    }

    /// <summary>
    /// 转换器信息
    /// </summary>
    public class ConverterInfo
    {
        public string type;
        public string sourceType;
        public string targetType;
        public LocationInfo? location;
    }

    /// <summary>
    /// 目标信息
    /// </summary>
    public class TargetInfo
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
    public class PropertyBindingInfo
    {
        public PropertyInfo propertyInfo;
        public ConverterInfo? converterInfo;
        public TargetInfo targetInfo;
        public BindingMode bindingMode;
    }

    /// <summary>
    /// 命令信息
    /// </summary>
    public class CommandInfo
    {
        public string name;
        public List<string> parameters;
        public bool isEvent;
        public LocationInfo? location;
    }

    public class CommandTargetInfo
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
    public class CommandBindingInfo
    {
        public CommandInfo commandInfo;
        public CommandTargetInfo targetInfo;
    }

    /// <summary>
    /// 绑定上下文信息
    /// </summary>
    public class ContextBindingInfo
    {
        public string viewModelType;
        public string viewModelNamespace;
        public string viewName;
        public string baseViewModelType;
        public string baseViewModelNamespace;
        public string baseViewName;
        public List<PropertyBindingInfo> properties = new List<PropertyBindingInfo>();
        public List<CommandBindingInfo> commands = new List<CommandBindingInfo>();
    }
}