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
        /// 可观察对象必须为 partial
        /// </summary>
        static readonly DiagnosticDescriptor ObservableObjectMustBePartialRule = Utility.CreateAttributeBindingRule(
            RuleIds.ObservableObjectMustBePartialRuleId,
            "ObservableObject '{0}' must be declared as 'partial'.");

        /// <summary>
        /// 绑定的对象不是ObservableObject
        /// </summary>
        static readonly DiagnosticDescriptor BindingObjectNotObservableObjectRule = Utility.CreateAttributeBindingRule(
            RuleIds.BindingObjectNotObservableObjectRuleId,
            "Target object '{0}' not 'FUI.Bindable.ObservableObject'.");

        /// <summary>
        /// 绑定的对象参数数量不是1
        /// </summary>
        static readonly DiagnosticDescriptor BindingObjectArgsCountNotOneRule = Utility.CreateAttributeBindingRule(
            RuleIds.BindingObjectArgsCountNotOneRuleId,
            "ViewBinding attribute args count must be 1, but got {0}.");

        /// <summary>
        /// 绑定对象规则
        /// </summary>
        static readonly DiagnosticDescriptor[] BindingObjectRules = new DiagnosticDescriptor[]
        {
            ObservableObjectMustBePartialRule,
            BindingObjectNotObservableObjectRule,
            BindingObjectArgsCountNotOneRule,
        };

        void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classDeclaration))
            {
                return;
            }

            var attributes = classDeclaration.AttributeLists.SelectMany((list) => list.Attributes);
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type.IsType(typeof(FUI.ViewBindingAttribute)))
                {
                    AnalyzeClassAttribute(context, classDeclaration, attribute);
                }

                // 规则：如果类型是可观察对象，则必须声明为 partial
                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
                if (typeSymbol != null && typeSymbol.IsObservableObject())
                {
                    var isPartial = classDeclaration.Modifiers.Any(m => m.Text == "partial");
                    if (!isPartial)
                    {
                        var diagnostic = Diagnostic.Create(ObservableObjectMustBePartialRule, classDeclaration.Identifier.GetLocation(), typeSymbol.ToString());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// 分析类型的绑定标签是否合法
        /// </summary>
        void AnalyzeClassAttribute(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, AttributeSyntax attribute)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (!(symbol is INamedTypeSymbol namedType))
            {
                return;
            }

            if (!namedType.IsObservableObject())
            {
                var diagnostic = Diagnostic.Create(BindingObjectNotObservableObjectRule, attribute.GetLocation(), namedType.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            var argsCount = attribute.ArgumentList?.Arguments.Count ?? 0;
            if(argsCount != 1)
            {
                var diagnostic = Diagnostic.Create(BindingObjectArgsCountNotOneRule, attribute.GetLocation(), argsCount);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
