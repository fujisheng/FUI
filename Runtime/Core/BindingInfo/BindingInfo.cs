using System.Collections.Generic;
using FUI.Binding;

namespace FUI.Infrastructure.Binding
{
    /// <summary>
    /// 要绑定的属性信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SourcePropertyDescriptor
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
        public SourceLocation location;
    }

    /// <summary>
    /// 位置信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SourceLocation
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ConverterDescriptor
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
        public SourceLocation location;
    }

    /// <summary>
    /// 要绑定的目标信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ElementTargetDescriptor
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

    /// <summary>
    /// 描述生成代码识别到的命令名称、参数和源码位置。
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class CommandDescriptor
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
        public SourceLocation location;
    }

    /// <summary>
    /// 描述命令绑定的目标 Element、成员和参数签名。
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
    /// 属性绑定信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class PropertyBindingDescriptor
    {
        public SourcePropertyDescriptor propertyInfo;
        public ConverterDescriptor converterInfo;
        public ElementTargetDescriptor targetInfo;

        public BindingMode bindingMode;
    }

    /// <summary>
    /// 命令绑定信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class CommandBindingDescriptor
    {
        public CommandDescriptor commandInfo;
        public CommandTargetDescriptor targetInfo;
    }

    /// <summary>
    /// 上下文绑定信息
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class BindingManifest
    {
        public string viewName = string.Empty;
        public string viewModelType;
        public List<PropertyBindingDescriptor> properties = new List<PropertyBindingDescriptor>();
        public List<CommandBindingDescriptor> commands = new List<CommandBindingDescriptor>();
    }
}
