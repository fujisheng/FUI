using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// 要绑定的属性信息
    /// </summary>
    public class PropertyInfo
    {
        /// <summary>
        /// 属性名
        /// </summary>
        public string name;

        /// <summary>
        /// 属性类型
        /// </summary>
        public string type;

        /// <summary>
        /// 是否是列表类型
        /// </summary>
        public bool isList;

        /// <summary>
        /// 位置信息
        /// </summary>
        public LocationInfo location;
    }

    /// <summary>
    /// 位置信息
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string path;

        /// <summary>
        /// 行数
        /// </summary>
        public int line;

        /// <summary>
        /// 列
        /// </summary>
        public int column;
    }

    /// <summary>
    /// 要绑定的转换器信息
    /// </summary>
    public class ConverterInfo
    {
        /// <summary>
        /// 转换器类型
        /// </summary>
        public string type;

        /// <summary>
        /// 转换器源类型
        /// </summary>
        public string sourceType;

        /// <summary>
        /// 转换器目标类型
        /// </summary>
        public string targetType;

        /// <summary>
        /// 位置信息
        /// </summary>
        public LocationInfo location;
    }

    /// <summary>
    /// 要绑定的目标信息
    /// </summary>
    public class TargetInfo
    {
        /// <summary>
        /// 目标路径
        /// </summary>
        public string path;

        /// <summary>
        /// 目标类型
        /// </summary>
        public string type;

        /// <summary>
        /// 目标属性类型
        /// </summary>
        public string propertyType;

        /// <summary>
        /// 目标属性名字
        /// </summary>
        public string propertyName;

        /// <summary>
        /// 目标属性值类型
        /// </summary>
        public string propertyValueType;
    }

    public class CommandInfo
    {
        /// <summary>
        /// 名字
        /// </summary>
        public string name;

        /// <summary>
        /// 参数
        /// </summary>
        public List<string> parameters;

        /// <summary>
        /// 是否是event
        /// </summary>
        public bool isEvent;

        /// <summary>
        /// 在源文件中的位置信息
        /// </summary>
        public LocationInfo location;
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
    /// 属性绑定信息
    /// </summary>
    public class PropertyBindingInfo
    {
        public PropertyInfo propertyInfo;
        public ConverterInfo converterInfo;
        public TargetInfo targetInfo;

        public BindingMode bindingMode;
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
    /// 上下文绑定信息
    /// </summary>
    public class ContextBindingInfo
    {
        public string viewName = string.Empty;
        public string viewModelType;
        public List<PropertyBindingInfo> properties = new List<PropertyBindingInfo>();
        public List<CommandBindingInfo> commands = new List<CommandBindingInfo>();
    }
}
