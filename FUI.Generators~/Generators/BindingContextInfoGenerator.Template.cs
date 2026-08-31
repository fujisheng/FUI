namespace FUI.Generators
{
    /// <summary>
    /// BindingContextInfo 代码模板
    /// </summary>
    public partial class BindingContextInfoGenerator
    {
        #region 生成绑定信息
        /// <summary>
        /// 生成绑定信息代码，用于编辑器工具
        /// </summary>
        public static string BuildContextInfoCode(BindingManifest info, string manifestNamespace)
        {
            var propertyBindingList = string.Join(",\n", info.properties.ConvertAll(p => BuildPropertyBindingInfoCode(p)));
            var commandBindingList = string.Join(",\n", info.commands.ConvertAll(c => BuildCommandBindingInfoCode(c)));

            return $@"#if UNITY_EDITOR
{Utility.FileHeader}

using System.Collections.Generic;
namespace {manifestNamespace}
{{
    public static class {Utility.GetBindingContextTypeName(info)}
    {{
        public static FUI.Infrastructure.Binding.BindingManifest Info => new FUI.Infrastructure.Binding.BindingManifest
        {{
            viewModelType = ""{info.viewModelType}"",
            viewName = ""{info.viewName}"",
            properties = new List<FUI.Infrastructure.Binding.PropertyBindingDescriptor>
            {{
{propertyBindingList}
            }},
            commands = new List<FUI.Infrastructure.Binding.CommandBindingDescriptor>
            {{
{commandBindingList}
            }}
        }};
    }}
}}
#endif ";
        }

        /// <summary>
        /// 生成 PropertyBindingDescriptor 初始化代码
        /// </summary>
        private static string BuildPropertyBindingInfoCode(PropertyBindingDescriptor info)
        {
            return $@"new FUI.Infrastructure.Binding.PropertyBindingDescriptor
{{
    propertyInfo = {BuildPropertyInfoCode(info.propertyInfo)},
    converterInfo = {BuildConverterInfoCode(info.converterInfo)},
    targetInfo = {BuildTargetInfoCode(info.targetInfo)},
    bindingMode = FUI.Binding.BindingMode.{info.bindingMode}
}}";
        }

        /// <summary>
        /// 生成 CommandBindingDescriptor 初始化代码
        /// </summary>
        private static string BuildCommandBindingInfoCode(CommandBindingDescriptor info)
        {
            return $@"new FUI.Infrastructure.Binding.CommandBindingDescriptor
{{
    commandInfo = {BuildCommandInfoCode(info.commandInfo)},
    targetInfo = {BuildCommandTargetInfoCode(info.targetInfo)}
}}";
        }

        /// <summary>
        /// 生成 SourcePropertyDescriptor 初始化代码
        /// </summary>
        private static string BuildPropertyInfoCode(SourcePropertyDescriptor info)
        {
            return $@"new FUI.Infrastructure.Binding.SourcePropertyDescriptor
{{
    name = ""{info.name}"",
    type = ""{info.type}"",
    isList = {info.isList.ToString().ToLower()},
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 ConverterDescriptor 初始化代码
        /// </summary>
        private static string BuildConverterInfoCode(ConverterDescriptor? info)
        {
            if (info == null)
            {
                return "null";
            }

            return $@"new FUI.Infrastructure.Binding.ConverterDescriptor
{{
    type = ""{info.type}"",
    sourceType = ""{info.sourceType}"",
    targetType = ""{info.targetType}"",
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 ElementTargetDescriptor 初始化代码
        /// </summary>
        private static string BuildTargetInfoCode(ElementTargetDescriptor info)
        {
            return $@"new FUI.Infrastructure.Binding.ElementTargetDescriptor
{{
    path = @""{info.path}"",
    type = ""{info.type}"",
    propertyType = ""{info.propertyType}"",
    propertyName = ""{info.propertyName}"",
    propertyValueType = ""{info.propertyValueType}""
}}";
        }

        /// <summary>
        /// 生成 CommandDescriptor 初始化代码
        /// </summary>
        private static string BuildCommandInfoCode(CommandDescriptor info)
        {
            var parameters = info.parameters != null && info.parameters.Count > 0
                ? $"new System.Collections.Generic.List<string> {{ {string.Join(", ", info.parameters.ConvertAll(p => $"\"{p}\""))} }}"
                : "new System.Collections.Generic.List<string>()";

            return $@"new FUI.Infrastructure.Binding.CommandDescriptor
{{
    name = ""{info.name}"",
    parameters = {parameters},
    isEvent = {info.isEvent.ToString().ToLower()},
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 CommandTargetDescriptor 初始化代码
        /// </summary>
        private static string BuildCommandTargetInfoCode(CommandTargetDescriptor info)
        {
            var parameters = info.parameters != null && info.parameters.Count > 0
                ? $"new System.Collections.Generic.List<string> {{ {string.Join(", ", info.parameters.ConvertAll(p => $"\"{p}\""))} }}"
                : "new System.Collections.Generic.List<string>()";

            return $@"new FUI.Infrastructure.Binding.CommandTargetDescriptor
{{
    path = @""{info.path}"",
    type = ""{info.type}"",
    propertyName = ""{info.propertyName}"",
    parameters = {parameters}
}}";
        }

        /// <summary>
        /// 生成 SourceLocation 初始化代码
        /// </summary>
        private static string BuildLocationInfoCode(SourceLocation? info)
        {
            if (info == null)
            {
                return "null";
            }

            return $@"new FUI.Infrastructure.Binding.SourceLocation
{{
    path = @""{info.path}"",
    line = {info.line},
    column = {info.column}
}}";
        }
        #endregion
    }
}
