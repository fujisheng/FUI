using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Linq;
using FUI.SourceGenerator;

namespace FUI.Analyzer.AttributeBinding
{
    public partial class AttributeBindingAnalyzer
    {
        /// <summary>
        /// 命令参数不匹配
        /// </summary>
        static readonly DiagnosticDescriptor CommandParameterMismatchRule = Utility.CreateAttributeBindingRule(
        RuleIds.CommandParameterMismatchRuleId,
        "Command parameter mismatch, expected '{0}' but got '{1}'.");

        /// <summary>
        /// 命令绑定规则
        /// </summary>
        static readonly DiagnosticDescriptor[] CommandBindingRules = new DiagnosticDescriptor[]
        {
            CommandParameterMismatchRule,
        };

        /// <summary>
        /// 分析一个事件字段
        /// </summary>
        /// <param name="context"></param>
        void AnalyzeEventField(SyntaxNodeAnalysisContext context)
        {
            if(!(context.Node is EventFieldDeclarationSyntax eventField))
            {
                return;
            }

            var attributes = eventField.AttributeLists.SelectMany((list) => list.Attributes);
            foreach(var attribute in attributes)
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type.IsType(typeof(FUI.CommandAttribute)))
                {
                    var type = eventField.Declaration.Type;
                    var typeInfo = context.SemanticModel.GetTypeInfo(type);
                    if (!(typeInfo.Type is INamedTypeSymbol namedType))
                    {
                        continue;
                    }

                    AnalyzeCommandAttribute(context, attribute, namedType.TypeArguments.ToArray());
                }
            }
        }

        /// <summary>
        /// 分析一个方法
        /// </summary>
        /// <param name="context"></param>
        void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax method))
            {
                return;
            }

            var attributes = method.AttributeLists.SelectMany((list) => list.Attributes);

            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type.IsType(typeof(FUI.CommandAttribute)))
                {
                    var methodParameter = method.ParameterList.Parameters.Select((parameter) => context.SemanticModel.GetTypeInfo(parameter.Type).Type).ToArray();
                    AnalyzeCommandAttribute(context, attribute, methodParameter);
                }
            }
        }

        /// <summary>
        /// 分析一个命令绑定是否合法
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attribute"></param>
        /// <param name="param"></param>
        void AnalyzeCommandAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, params ITypeSymbol[] param)
        {
            var targetPropertyTypeInfo = GetTargetPropertyTypeInfo(context, attribute, out var memberAccess);

            if (targetPropertyTypeInfo == null)
            {
                return;
            }

            var targetPropertyType = targetPropertyTypeInfo.Value.Type;
            if(!(targetPropertyType is INamedTypeSymbol namedType))
            {
                return;
            }

            //检查参数是否匹配
            var targetTypeArguments = namedType.TypeArguments.ToArray();
            if(!CompareTypes(param, targetTypeArguments))
            {
                var diagnostic = Diagnostic.Create(CommandParameterMismatchRule, attribute.GetLocation(), GetArgumentsString(targetTypeArguments), GetArgumentsString(param));
                context.ReportDiagnostic(diagnostic);
            }
        }

        string GetArgumentsString(ITypeSymbol[] symbols)
        {
            return $"({string.Join(", ", symbols.Select((symbol) => symbol.ToString()))})";
        }

        bool CompareTypes(ITypeSymbol[] symbolsL, ITypeSymbol[] symbolsR)
        {
            if (symbolsL.Length != symbolsR.Length)
            {
                return false;
            }

            for (int i = 0; i < symbolsL.Length; i++)
            {
                var l = symbolsL[i];
                var r = symbolsR[i];
                if (l == null || r == null || !SymbolEqualityComparer.Default.Equals(l, r))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
