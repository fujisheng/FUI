using Microsoft.CodeAnalysis;

namespace FUI.SourceGenerator
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
            title: "类必须继承 ObservableObject",
            messageFormat: "类 '{0}' 必须继承 ObservableObject 才能使用 FUI 绑定特性",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "使用 [Binding] 或 [Command] 特性的类必须继承自 ObservableObject");

        /// <summary>
        /// FUI002: 无效的绑定目标
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidBindingTarget = new(
            id: "FUI002",
            title: "无效的绑定目标",
            messageFormat: "属性 '{0}' 的绑定目标无效: {1}",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "绑定目标必须使用 nameof(Element.Property) 格式指定");

        /// <summary>
        /// FUI003: 缺少元素路径
        /// </summary>
        public static readonly DiagnosticDescriptor MissingElementPath = new(
            id: "FUI003",
            title: "缺少元素路径",
            messageFormat: "属性 '{0}' 的 [Binding] 特性缺少元素路径参数",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "[Binding] 特性必须包含元素路径参数，如 [Binding(\"ElementName\", nameof(Element.Property))]");

        /// <summary>
        /// FUI004: 无效的 nameof 表达式
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidNameofExpression = new(
            id: "FUI004",
            title: "无效的 nameof 表达式",
            messageFormat: "属性 '{0}' 的绑定目标必须是有效的 nameof(Element.Property) 表达式",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI005: 双向绑定需要 Setter
        /// </summary>
        public static readonly DiagnosticDescriptor TwoWayBindingRequiresSetter = new(
            id: "FUI005",
            title: "双向绑定需要属性 Setter",
            messageFormat: "属性 '{0}' 使用 TwoWay 绑定模式，但必须具有 setter",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "TwoWay 绑定需要在属性值变化时更新 ViewModel");

        /// <summary>
        /// FUI006: 命令绑定缺少元素路径
        /// </summary>
        public static readonly DiagnosticDescriptor MissingCommandElementPath = new(
            id: "FUI006",
            title: "命令绑定缺少元素路径",
            messageFormat: "方法/事件 '{0}' 的 [Command] 特性缺少元素路径",
            category: "FUI.Command",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI007: 无效的值转换器类型
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidValueConverter = new(
            id: "FUI007",
            title: "无效的值转换器",
            messageFormat: "属性 '{0}' 指定的值转换器 '{1}' 未实现 IValueConverter<TSource, TTarget>",
            category: "FUI.Converter",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI008: 生成器异常
        /// </summary>
        public static readonly DiagnosticDescriptor GeneratorException = new(
            id: "FUI008",
            title: "FUI 生成器异常",
            messageFormat: "处理类 '{0}' 时发生异常: {1}",
            category: "FUI.Internal",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI009: 无效的绑定模式
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidBindingMode = new(
            id: "FUI009",
            title: "无效的绑定模式",
            messageFormat: "属性 '{0}' 指定了无效的绑定模式: {1}",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // ========== 警告 (Warnings) ==========

        /// <summary>
        /// FUI101: 类型不匹配
        /// </summary>
        public static readonly DiagnosticDescriptor TypeMismatch = new(
            id: "FUI101",
            title: "绑定类型可能不匹配",
            messageFormat: "属性 '{0}' 的类型 '{1}' 与目标属性类型 '{2}' 不匹配，建议添加值转换器",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI102: 属性没有可访问的 getter
        /// </summary>
        public static readonly DiagnosticDescriptor PropertyMissingGetter = new(
            id: "FUI102",
            title: "属性缺少 getter",
            messageFormat: "属性 '{0}' 没有可访问的 getter，OneWay 绑定可能无法正常工作",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI103: 建议添加 ViewBinding 特性
        /// </summary>
        public static readonly DiagnosticDescriptor MissingViewBinding = new(
            id: "FUI103",
            title: "建议添加 ViewBinding 特性",
            messageFormat: "类 '{0}' 使用了 FUI 绑定特性但未添加 [ViewBinding] 特性",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "添加 [ViewBinding] 特性可以显式声明关联的视图类型");

        /// <summary>
        /// FUI104: 命名空间使用 auto-property
        /// </summary>
        public static readonly DiagnosticDescriptor UseAutoPropertySuggestion = new(
            id: "FUI104",
            title: "建议使用自动属性",
            messageFormat: "属性 '{0}' 可以改为自动属性，FUI 生成器会自动实现变更通知",
            category: "FUI.Style",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>
        /// FUI105: 可能的拼写错误
        /// </summary>
        public static readonly DiagnosticDescriptor PossibleTypo = new(
            id: "FUI105",
            title: "可能的拼写错误",
            messageFormat: "元素路径 '{0}' 与已知元素类型相似但不完全匹配，是否想输入 '{1}'?",
            category: "FUI.Binding",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // ========== 信息 (Info) ==========

        /// <summary>
        /// FUI201: 生成成功
        /// </summary>
        public static readonly DiagnosticDescriptor GenerationSuccess = new(
            id: "FUI201",
            title: "FUI 绑定上下文已生成",
            messageFormat: "已为 '{0}' 生成绑定上下文 ({1} 个属性绑定, {2} 个命令绑定)",
            category: "FUI.Generation",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false);  // 默认关闭，避免信息过多

        /// <summary>
        /// FUI202: 调试信息（仅在 DEBUG 编译时启用）
        /// </summary>
        public static readonly DiagnosticDescriptor DebugInfo = new(
            id: "FUI202",
            title: "FUI 调试信息",
            messageFormat: "[FUI Debug] {0}",
            category: "FUI.Debug",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false);
    }
}
