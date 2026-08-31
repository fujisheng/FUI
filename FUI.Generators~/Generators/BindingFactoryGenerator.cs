using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUI.Generators
{
    /// <summary>
    /// 为所有可观察类型生成按 ViewModel 类型索引的直接构造工厂。
    /// </summary>
    internal sealed class BindingFactoryGenerator
    {
        const string PresenterAttributeName = "FUI.Binding.PresenterForAttribute";

        /// <summary>
        /// 保存一个 ViewModel 对应的绑定上下文和 Presenter 工厂生成信息。
        /// </summary>
        sealed class FactoryInfo
        {
            public INamedTypeSymbol ViewModel;
            public INamedTypeSymbol Presenter;
            public string ViewName;
        }

        internal void Generate(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<ClassDeclarationSyntax> declarations)
        {
            // Presenter 选择在编译期完成，运行时只执行生成的直接构造委托。
            var allTypes = GetAllTypes(compilation.Assembly.GlobalNamespace).ToArray();
            var factories = new List<FactoryInfo>();
            foreach (var declaration in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
                var viewModel = semanticModel.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
                if (viewModel == null || viewModel.IsAbstract || viewModel.TypeParameters.Length > 0)
                {
                    continue;
                }

                var bindingInfos = new ContextInfoByAttributeGenerator().Generate(
                    compilation,
                    declaration);
                // 没有 ViewContract 的 List Item、Prototype 也会产生 bindingInfo，因此同样注册工厂。
                foreach (var bindingInfo in bindingInfos)
                {
                    factories.Add(new FactoryInfo
                    {
                        ViewModel = viewModel,
                        Presenter = SelectDefaultPresenter(viewModel, allTypes),
                        ViewName = bindingInfo.viewName ?? string.Empty,
                    });
                }
            }

            if (factories.Count == 0)
            {
                return;
            }

            var source = BuildRegistry(Utility.GetModuleNamespace(compilation), factories);
            context.AddSource("BindingRegistry.g.cs", Utility.FormatCode(source));
        }

        static string BuildRegistry(string moduleNamespace, IEnumerable<FactoryInfo> factories)
        {
            var builder = new StringBuilder();
            builder.AppendLine(Utility.FileHeader);
            builder.AppendLine($"namespace {moduleNamespace}");
            builder.AppendLine("{");
            builder.AppendLine("internal static class BindingRegistry");
            builder.AppendLine("{");
            builder.AppendLine("    static bool initialized;");
            builder.AppendLine("    public static void Initialize()");
            builder.AppendLine("    {");
            builder.AppendLine("        if (initialized) return;");
            builder.AppendLine("        initialized = true;");

            foreach (var factory in factories.OrderBy(
                         item => item.ViewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                         StringComparer.Ordinal))
            {
                builder.AppendLine(BuildRegistration(factory));
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            builder.AppendLine("}");
            return builder.ToString();
        }

        static string BuildRegistration(FactoryInfo factory)
        {
            var viewModel = factory.ViewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var presenter = factory.Presenter?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                ?? "global::FUI.Presentation.EmptyPresenter";
            var contextName = $"{viewModel}.{factory.ViewModel.Name}_{factory.ViewName}_BindingContext";
            return $@"
        global::FUI.Infrastructure.GeneratedBindingRegistry.Register<{viewModel}>(
            static (view, viewModel) => new {contextName}(view, viewModel),
            static () => new {presenter}());";
        }

        static INamedTypeSymbol SelectDefaultPresenter(
            INamedTypeSymbol viewModel,
            IEnumerable<INamedTypeSymbol> allTypes)
        {
            // 显式 PresenterForAttribute 优先，确保一个 ViewModel 有多个 Presenter 时选择确定。
            var explicitPresenters = allTypes
                .Where(type => !type.IsAbstract && type.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == PresenterAttributeName
                    && attribute.ConstructorArguments.Length > 0
                    && SymbolEqualityComparer.Default.Equals(
                        attribute.ConstructorArguments[0].Value as ITypeSymbol,
                        viewModel)))
                .OrderBy(
                    type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    StringComparer.Ordinal)
                .ToArray();
            if (explicitPresenters.Length > 0)
            {
                return explicitPresenters[0];
            }

            // 未显式指定时仅在唯一兼容 Presenter 的情况下自动选择，避免依赖扫描顺序。
            var compatiblePresenters = allTypes
                .Where(type => !type.IsAbstract && type.AllInterfaces.Any(@interface =>
                    @interface.OriginalDefinition.ToDisplayString() == "FUI.Presentation.IPresenter<TObservableObject>"
                    && @interface.TypeArguments.Length == 1
                    && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], viewModel)))
                .OrderBy(
                    type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    StringComparer.Ordinal)
                .ToArray();
            return compatiblePresenters.Length == 1 ? compatiblePresenters[0] : null;
        }

        static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
        {
            foreach (var type in root.GetTypeMembers())
            {
                yield return type;
                foreach (var nested in GetNestedTypes(type))
                {
                    yield return nested;
                }
            }

            foreach (var child in root.GetNamespaceMembers())
            {
                foreach (var type in GetAllTypes(child))
                {
                    yield return type;
                }
            }
        }

        static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (var descendant in GetNestedTypes(nested))
                {
                    yield return descendant;
                }
            }
        }

    }
}
