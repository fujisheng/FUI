using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;

namespace FUI.Generators
{
    /// <summary>
    /// FUI 唯一的增量生成器入口。
    /// </summary>
    [Generator]
    public sealed class BindingSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 语法阶段只筛 Class，语义阶段再过滤 ObservableObject，减少无关节点进入后续流水线。
            var classes = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (syntaxContext, _) => GetPrimaryObservableClass(syntaxContext))
                .Where(static declaration => declaration != null)
                .Collect();

            var input = context.CompilationProvider.Combine(classes);
            context.RegisterSourceOutput(input, static (productionContext, pair) =>
            {
                var compilation = pair.Left;
                var bindingGenerator = new BindingContextGenerator();
                var observableGenerator = new ObservableObjectGenerator();
                var infoGenerator = new BindingContextInfoGenerator();

                var validDeclarations = new List<ClassDeclarationSyntax>();
                // 每个类型生成自身代码；全局 Binding Registry 与 Route Registry 只生成一次。
                foreach (var declaration in pair.Right)
                {
                    productionContext.CancellationToken.ThrowIfCancellationRequested();
                    if (!ElementTargetValidation.Validate(productionContext, compilation, declaration)) { continue; }
                    validDeclarations.Add(declaration);
                    observableGenerator.Generate(productionContext, compilation, declaration);
                    bindingGenerator.Generate(productionContext, compilation, declaration);
                    infoGenerator.Generate(productionContext, compilation, declaration);
                }

                new BindingFactoryGenerator().Generate(productionContext, compilation, validDeclarations);
                new RouteGenerator().Generate(productionContext, compilation, validDeclarations);
            });
        }

        static ClassDeclarationSyntax GetPrimaryObservableClass(GeneratorSyntaxContext context)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            var filePath = declaration.SyntaxTree.FilePath ?? string.Empty;
            if (filePath.EndsWith(".g.cs") || filePath.EndsWith(".generated.cs"))
            {
                return null;
            }

            var type = context.SemanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
            if (type == null || !type.IsObservableObject())
            {
                return null;
            }

            // partial 类型只由第一个声明触发，避免同一 ViewModel 重复生成 HintName。
            var primary = type.DeclaringSyntaxReferences.FirstOrDefault();
            if (primary == null || primary.SyntaxTree != declaration.SyntaxTree || primary.Span.Start != declaration.SpanStart)
            {
                return null;
            }

            return declaration;
        }
    }
}
