namespace FUI.Analyzer
{
    internal class RuleIds
    {
        /// <summary>
        /// 绑定目标不是FUI.IElement
        /// </summary>
        internal const string TargetNotElementRuleId = "FUI0001";

        /// <summary>
        /// 绑定目标属性不可绑定
        /// </summary>
        internal const string TargetPropertyNotBindableRuleId = "FUI0002";

        /// <summary>
        /// 转换器不是FUI.IConverter
        /// </summary>
        internal const string ConverterNotIConverterRuleId = "FUI0003";

        /// <summary>
        /// 属性到目标值无法转换
        /// </summary>
        internal const string PropertyToTargetWithoutConverterRuleId = "FUI0004";

        /// <summary>
        /// 属性到目标通过转换器无法转换
        /// </summary>
        internal const string PropertyToTargetWithConverterRuleId = "FUI0005";

        /// <summary>
        /// 目标字段字符串赋值必须使用nameof(xxx.yyy)
        /// </summary>
        internal const string TargetMustBeNameOfRuleId = "FUI0006";

        /// <summary>
        /// 命令参数不匹配
        /// </summary>
        internal const string CommandParameterMismatchRuleId = "FUI0007";

        /// <summary>
        /// 绑定目标不是ObservableObject
        /// </summary>
        internal const string BindingObjectNotObservableObjectRuleId = "FUI0008";

        /// <summary>
        /// 绑定对象参数个数不为1
        /// </summary>
        internal const string BindingObjectArgsCountNotOneRuleId = "FUI0009";

        /// <summary>
        /// 可观察对象必须使用 partial 修饰符
        /// </summary>
        internal const string ObservableObjectMustBePartialRuleId = "FUI0010";
    }
}
