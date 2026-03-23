using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FUI.Analyzer
{
    internal static class Utility
    {
        /// <summary>
        /// 创建一个属性绑定规则
        /// </summary>
        /// <param name="id">规则id</param>
        /// <param name="messageFormat">消息</param>
        /// <param name="helpUrl">帮助链接</param>
        /// <returns></returns>
        internal static DiagnosticDescriptor CreateAttributeBindingRule(string id, string messageFormat, string helpUrl = "")
        {
            return new DiagnosticDescriptor(
                id: id,
                title: "InvalidBinding",
                messageFormat: messageFormat,
                category: "FUI",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: helpUrl
            );
        }

        /// <summary>
        /// 尝试获取表达式的字符串常量值
        /// </summary>
        internal static string TryGetStringConstant(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            if (expression == null)
            {
                return null;
            }

            var literal = expression as LiteralExpressionSyntax;
            if (literal != null)
            {
                var valueObj = literal.Token.Value;
                if (valueObj is string s)
                {
                    return s;
                }
            }

            var constant = semanticModel.GetConstantValue(expression);
            if (constant.HasValue && constant.Value is string cs)
            {
                return cs;
            }

            return null;
        }

        /// <summary>
        /// 在特性参数中查找第一个字符串常量（包含 const 与字面量）
        /// </summary>
        internal static string FindFirstStringConstantArgument(SemanticModel semanticModel, AttributeSyntax attribute)
        {
            if (attribute == null || attribute.ArgumentList == null)
            {
                return null;
            }

            foreach (var arg in attribute.ArgumentList.Arguments)
            {
                var value = TryGetStringConstant(semanticModel, arg.Expression);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
