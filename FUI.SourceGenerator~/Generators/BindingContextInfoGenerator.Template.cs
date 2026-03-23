namespace FUI.SourceGenerator.Generators
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
        public static string BuildContextInfoCode(ContextBindingInfo info)
        {
            var propertyBindingList = string.Join(",\n", info.properties.ConvertAll(p => BuildPropertyBindingInfoCode(p)));
            var commandBindingList = string.Join(",\n", info.commands.ConvertAll(c => BuildCommandBindingInfoCode(c)));

            return $@"#if UNITY_EDITOR
{Utility.FileHeader}

using System.Collections.Generic;
namespace FUI.Generated.ContextInfo
{{
    public static class {Utility.GetBindingContextTypeName(info)}
    {{
        public static FUI.ContextBindingInfo Info => new FUI.ContextBindingInfo
        {{
            viewModelType = ""{info.viewModelType}"",
            viewName = ""{info.viewName}"",
            properties = new List<FUI.PropertyBindingInfo>
            {{
{propertyBindingList}
            }},
            commands = new List<FUI.CommandBindingInfo>
            {{
{commandBindingList}
            }}
        }};
    }}
}}
#endif ";
        }

        /// <summary>
        /// 生成 PropertyBindingInfo 初始化代码
        /// </summary>
        private static string BuildPropertyBindingInfoCode(PropertyBindingInfo info)
        {
            return $@"new FUI.PropertyBindingInfo
{{
    propertyInfo = {BuildPropertyInfoCode(info.propertyInfo)},
    converterInfo = {BuildConverterInfoCode(info.converterInfo)},
    targetInfo = {BuildTargetInfoCode(info.targetInfo)},
    bindingMode = FUI.BindingMode.{info.bindingMode}
}}";
        }

        /// <summary>
        /// 生成 CommandBindingInfo 初始化代码
        /// </summary>
        private static string BuildCommandBindingInfoCode(CommandBindingInfo info)
        {
            return $@"new FUI.CommandBindingInfo
{{
    commandInfo = {BuildCommandInfoCode(info.commandInfo)},
    targetInfo = {BuildCommandTargetInfoCode(info.targetInfo)}
}}";
        }

        /// <summary>
        /// 生成 PropertyInfo 初始化代码
        /// </summary>
        private static string BuildPropertyInfoCode(PropertyInfo info)
        {
            return $@"new FUI.PropertyInfo
{{
    name = ""{info.name}"",
    type = ""{info.type}"",
    isList = {info.isList.ToString().ToLower()},
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 ConverterInfo 初始化代码
        /// </summary>
        private static string BuildConverterInfoCode(ConverterInfo? info)
        {
            if (info == null)
            {
                return "null";
            }

            return $@"new FUI.ConverterInfo
{{
    type = ""{info.type}"",
    sourceType = ""{info.sourceType}"",
    targetType = ""{info.targetType}"",
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 TargetInfo 初始化代码
        /// </summary>
        private static string BuildTargetInfoCode(TargetInfo info)
        {
            return $@"new FUI.TargetInfo
{{
    path = @""{info.path}"",
    type = ""{info.type}"",
    propertyType = ""{info.propertyType}"",
    propertyName = ""{info.propertyName}"",
    propertyValueType = ""{info.propertyValueType}""
}}";
        }

        /// <summary>
        /// 生成 CommandInfo 初始化代码
        /// </summary>
        private static string BuildCommandInfoCode(CommandInfo info)
        {
            var parameters = info.parameters != null && info.parameters.Count > 0
                ? $"new System.Collections.Generic.List<string> {{ {string.Join(", ", info.parameters.ConvertAll(p => $"\"{p}\""))} }}"
                : "new System.Collections.Generic.List<string>()";

            return $@"new FUI.CommandInfo
{{
    name = ""{info.name}"",
    parameters = {parameters},
    isEvent = {info.isEvent.ToString().ToLower()},
    location = {BuildLocationInfoCode(info.location)}
}}";
        }

        /// <summary>
        /// 生成 CommandTargetInfo 初始化代码
        /// </summary>
        private static string BuildCommandTargetInfoCode(CommandTargetInfo info)
        {
            var parameters = info.parameters != null && info.parameters.Count > 0
                ? $"new System.Collections.Generic.List<string> {{ {string.Join(", ", info.parameters.ConvertAll(p => $"\"{p}\""))} }}"
                : "new System.Collections.Generic.List<string>()";

            return $@"new FUI.CommandTargetInfo
{{
    path = @""{info.path}"",
    type = ""{info.type}"",
    propertyName = ""{info.propertyName}"",
    parameters = {parameters}
}}";
        }

        /// <summary>
        /// 生成 LocationInfo 初始化代码
        /// </summary>
        private static string BuildLocationInfoCode(LocationInfo? info)
        {
            if (info == null)
            {
                return "null";
            }

            return $@"new FUI.LocationInfo
{{
    path = @""{info.path}"",
    line = {info.line},
    column = {info.column}
}}";
        }
        #endregion
    }
}
