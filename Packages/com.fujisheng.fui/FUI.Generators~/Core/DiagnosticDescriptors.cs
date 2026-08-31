using Microsoft.CodeAnalysis;

namespace FUI.Generators
{
    /// <summary>
    /// FUI Source Generator 诊断描述符定义
    /// 错误码范围: FUI001-FUI099
    /// 警告码范围: FUI101-FUI199
    /// 信息码范围: FUI201-FUI299
    /// </summary>
    public static class DiagnosticDescriptors
    {
        // ========== 错误 (Errors) ==========

        /// <summary>
        /// FUI001: 类未继承 ObservableObject
        /// </summary>
        public static readonly DiagnosticDescriptor MissingObservableObject = new(
            id: "FUI001",
            title: "Class must inherit from ObservableObject",
            messageFormat: "Class '{0}' must inherit from ObservableObject to use FUI binding attributes",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Classes that use [Bind] or [Command] must inherit from ObservableObject.");

        /// <summary>
        /// FUI002: 无效的绑定目标
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidBindingTarget = new(
            id: "FUI002",
            title: "Invalid binding target",
            messageFormat: "Property '{0}' has an invalid binding target: {1}",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Binding targets must use the nameof(Element.Property) format.");

        /// <summary>
        /// FUI003: 缺少元素路径
        /// </summary>
        public static readonly DiagnosticDescriptor MissingElementPath = new(
            id: "FUI003",
            title: "Missing element path",
            messageFormat: "The [Bind] attribute on property '{0}' is missing an element path",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The [Bind] attribute must include an element path, for example [Bind(\"ElementName\", nameof(Element.Property))].");

        /// <summary>
        /// FUI004: 无效的 nameof 表达式
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidNameofExpression = new(
            id: "FUI004",
            title: "Invalid nameof expression",
            messageFormat: "The binding target for property '{0}' must be a valid nameof(Element.Property) expression",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI005: 双向绑定需要 Setter
        /// </summary>
        public static readonly DiagnosticDescriptor TwoWayBindingRequiresSetter = new(
            id: "FUI005",
            title: "Two-way binding requires a setter",
            messageFormat: "Property '{0}' uses TwoWay binding and must have a setter",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "TwoWay binding requires a setter so the ViewModel can be updated.");

        /// <summary>
        /// FUI006: 命令绑定缺少元素路径
        /// </summary>
        public static readonly DiagnosticDescriptor MissingCommandElementPath = new(
            id: "FUI006",
            title: "Command binding is missing an element path",
            messageFormat: "The [Command] attribute on method or event '{0}' is missing an element path",
            category: "FUI.Command",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI007: 无效的值转换器类型
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidValueConverter = new(
            id: "FUI007",
            title: "Invalid value converter",
            messageFormat: "Value converter '{1}' specified by property '{0}' does not implement IValueConverter<TSource, TTarget>",
            category: "FUI.Converter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI008: 生成器异常
        /// </summary>
        public static readonly DiagnosticDescriptor GeneratorException = new(
            id: "FUI008",
            title: "FUI generator exception",
            messageFormat: "An exception occurred while processing class '{0}': {1}",
            category: "FUI.Internal",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI009: 无效的绑定模式
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidBindingMode = new(
            id: "FUI009",
            title: "Invalid binding mode",
            messageFormat: "Property '{0}' specifies an invalid binding mode: {1}",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // ========== 警告 (Warnings) ==========

        /// <summary>
        /// FUI101: 类型不匹配
        /// </summary>
        public static readonly DiagnosticDescriptor TypeMismatch = new(
            id: "FUI101",
            title: "Potential binding type mismatch",
            messageFormat: "Property '{0}' has type '{1}', which does not match target property type '{2}'; consider adding a value converter",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI102: 属性没有可访问的 getter
        /// </summary>
        public static readonly DiagnosticDescriptor PropertyMissingGetter = new(
            id: "FUI102",
            title: "Property is missing a getter",
            messageFormat: "Property '{0}' does not have an accessible getter; OneWay binding may not work correctly",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI103: 建议添加 ViewContract 特性
        /// </summary>
        public static readonly DiagnosticDescriptor MissingViewBinding = new(
            id: "FUI103",
            title: "Consider adding ViewContract",
            messageFormat: "Class '{0}' uses FUI binding attributes but does not declare [ViewContract]",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Add [ViewContract] to explicitly declare the associated view type.");

        /// <summary>
        /// FUI104: 命名空间使用 auto-property
        /// </summary>
        public static readonly DiagnosticDescriptor UseAutoPropertySuggestion = new(
            id: "FUI104",
            title: "Consider using an auto-property",
            messageFormat: "Property '{0}' can be converted to an auto-property; the FUI generator will implement change notifications",
            category: "FUI.Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI105: 可能的拼写错误
        /// </summary>
        public static readonly DiagnosticDescriptor PossibleTypo = new(
            id: "FUI105",
            title: "Possible typo",
            messageFormat: "Element path '{0}' is similar to a known element type; did you mean '{1}'?",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // ========== 信息 (Info) ==========

        /// <summary>
        /// FUI201: 生成成功
        /// </summary>
        public static readonly DiagnosticDescriptor GenerationSuccess = new(
            id: "FUI201",
            title: "FUI binding context generated",
            messageFormat: "Generated a binding context for '{0}' ({1} property bindings, {2} command bindings)",
            category: "FUI.Generation",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false);  // 默认关闭，避免信息过多

        /// <summary>
        /// FUI202: 调试信息（仅在 DEBUG 编译时启用）
        /// </summary>
        public static readonly DiagnosticDescriptor DebugInfo = new(
            id: "FUI202",
            title: "FUI debug information",
            messageFormat: "[FUI Debug] {0}",
            category: "FUI.Debug",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false);
    }
}
