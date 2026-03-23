using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;

namespace FUI.Analyzer.AttributeBinding
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public partial class AttributeBindingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray
            .CreateRange(PropertyBindingRules)
            .AddRange(CommandBindingRules)
            .AddRange(BindingObjectRules);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //属性绑定
            //context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);

            //命令绑定
            //context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            //context.RegisterSyntaxNodeAction(AnalyzeEventField, SyntaxKind.EventFieldDeclaration);

            //ViewModel标签
            //context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }
    }
}
