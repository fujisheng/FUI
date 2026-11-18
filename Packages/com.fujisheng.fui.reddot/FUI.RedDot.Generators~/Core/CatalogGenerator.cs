using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace FUI.RedDot.Generators
{
    /// <summary>
    /// 编译期生成 Catalog 注册代码，并诊断无效节点、父边、动态工厂、环和聚合器类型。
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class CatalogGenerator : IIncrementalGenerator
    {
        const string RedDotCatalogAttribute = "FUI.RedDot.RedDotCatalogAttribute";
        const string RedDotNodeAttribute = "FUI.RedDot.RedDotNodeAttribute";
        const string RedDotFactoryAttribute = "FUI.RedDot.RedDotFactoryAttribute";
        const string KeyType = "FUI.RedDot.RedDotKey";
        const string AggregatorInterface = "FUI.RedDot.IRedDotAggregator";

        static readonly DiagnosticDescriptor InvalidCatalog = Create("RDT0001", "Invalid red dot catalog", "Red dot catalog '{0}' must be a top-level static partial class");
        static readonly DiagnosticDescriptor InvalidNode = Create("RDT0002", "Invalid red dot node", "Red dot node '{0}' must be a public static readonly RedDotKey with a resolvable Root or Child initializer");
        static readonly DiagnosticDescriptor DuplicateKey = Create("RDT0003", "Duplicate red dot key", "Red dot key '{0}' is declared more than once");
        static readonly DiagnosticDescriptor MissingParent = Create("RDT0004", "Missing red dot parent", "Red dot member '{0}' references missing parent field '{1}'");
        static readonly DiagnosticDescriptor StaticCycle = Create("RDT0005", "Static red dot cycle", "A static red dot cycle includes field '{0}'");
        static readonly DiagnosticDescriptor InvalidFactory = Create("RDT0006", "Invalid red dot factory", "Red dot factory '{0}' must be public static, return RedDotKey, and return RedDotKey.Dynamic with a constant segment and every factory parameter in declaration order");
        static readonly DiagnosticDescriptor InvalidParameter = Create("RDT0007", "Invalid dynamic key parameter", "Red dot factory parameter '{0}' must be int, long, string, or Guid");
        static readonly DiagnosticDescriptor InvalidAggregator = Create("RDT0008", "Invalid red dot aggregator", "Aggregator '{0}' must be public, stateless, non-abstract, implement IRedDotAggregator, and have a public parameterless constructor");

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var catalogs = context.SyntaxProvider.ForAttributeWithMetadataName(
                RedDotCatalogAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (generatorContext, _) => (INamedTypeSymbol)generatorContext.TargetSymbol).Collect();

            context.RegisterSourceOutput(context.CompilationProvider.Combine(catalogs),
                static (productionContext, pair) => Generate(productionContext, pair.Left, pair.Right));
        }

        static void Generate(SourceProductionContext context, Compilation compilation, ImmutableArray<INamedTypeSymbol> catalogs)
        {
            for (var index = 0; index < catalogs.Length; index++)
            {
                GenerateCatalog(context, compilation, catalogs[index]);
            }
        }

        static void GenerateCatalog(SourceProductionContext context, Compilation compilation, INamedTypeSymbol catalog)
        {
            if (catalog.ContainingType != null || !catalog.IsStatic || !IsPartial(catalog))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidCatalog, catalog.Locations.FirstOrDefault(), catalog.Name));
                return;
            }

            var fields = catalog.GetMembers().OfType<IFieldSymbol>()
                .Where(field => FindAttribute(field, RedDotNodeAttribute) != null)
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .ToArray();
            var methods = catalog.GetMembers().OfType<IMethodSymbol>()
                .Where(method => FindAttribute(method, RedDotFactoryAttribute) != null)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ToArray();
            var fieldByName = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
            var resolvedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            var parentNames = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var aggregators = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
            var hasErrors = false;

            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                var attribute = FindAttribute(field, RedDotNodeAttribute);
                if (field.DeclaredAccessibility != Accessibility.Public || !field.IsStatic || !field.IsReadOnly || field.Type.ToDisplayString() != KeyType ||
                    !TryResolveStaticKey(compilation, field, fieldByName, resolvedKeys, new HashSet<string>(), out var key))
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidNode, field.Locations.FirstOrDefault(), field.Name));
                    hasErrors = true;
                    continue;
                }
                resolvedKeys[field.Name] = key;
                parentNames[field.Name] = ReadParents(attribute);
                if (!ValidateParents(context, field, parentNames[field.Name], fieldByName)) hasErrors = true;
                if (!TryReadAggregator(context, attribute, out var aggregator)) hasErrors = true;
                aggregators[field.Name] = aggregator;
            }

            foreach (var duplicate in resolvedKeys.GroupBy(pair => pair.Value, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateKey, catalog.Locations.FirstOrDefault(), duplicate.Key));
                hasErrors = true;
            }
            if (!ValidateCycles(context, fields, parentNames)) hasErrors = true;

            var factories = new List<FactoryInfo>();
            for (var index = 0; index < methods.Length; index++)
            {
                var method = methods[index];
                var attribute = FindAttribute(method, RedDotFactoryAttribute);
                var factoryParents = ReadParents(attribute);
                var factoryValid = TryReadFactory(compilation, method, out var segment, out var canonicalParent) &&
                                   factoryParents.Length > 0 && factoryParents[0] == canonicalParent;
                if (!factoryValid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidFactory, method.Locations.FirstOrDefault(), method.Name));
                    hasErrors = true;
                }
                if (!ValidateParents(context, method, factoryParents, fieldByName)) hasErrors = true;
                for (var parameterIndex = 0; parameterIndex < method.Parameters.Length; parameterIndex++)
                {
                    if (!IsAllowedParameter(method.Parameters[parameterIndex].Type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidParameter, method.Parameters[parameterIndex].Locations.FirstOrDefault(), method.Parameters[parameterIndex].Name));
                        hasErrors = true;
                    }
                }
                if (!TryReadAggregator(context, attribute, out var aggregator)) hasErrors = true;
                if (factoryValid)
                {
                    factories.Add(new FactoryInfo(method, segment, factoryParents, aggregator));
                }
            }

            foreach (var duplicate in factories
                         .Where(factory => resolvedKeys.ContainsKey(factory.Parents[0]))
                         .GroupBy(factory => resolvedKeys[factory.Parents[0]] + "/" + Uri.EscapeDataString(factory.Segment), StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateKey, catalog.Locations.FirstOrDefault(), duplicate.Key));
                hasErrors = true;
            }

            if (hasErrors)
            {
                return;
            }

            var source = BuildSource(catalog, fields, parentNames, aggregators, factories);
            context.AddSource(catalog.Name + ".Catalog.g.cs", source);
        }

        static string BuildSource(
            INamedTypeSymbol catalog,
            IFieldSymbol[] fields,
            Dictionary<string, string[]> parents,
            Dictionary<string, INamedTypeSymbol> aggregators,
            List<FactoryInfo> factories)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable disable");
            var namespaceName = catalog.ContainingNamespace.IsGlobalNamespace ? null : catalog.ContainingNamespace.ToDisplayString();
            if (namespaceName != null) builder.Append("namespace ").Append(namespaceName).AppendLine(" {");
            builder.Append("public static partial class ").Append(catalog.Name).AppendLine(" {");
            builder.AppendLine("/// <summary>将生成的静态节点、父边和动态模板注册到指定中心。</summary>");
            builder.AppendLine("public static void Register(global::FUI.RedDot.RedDotCenter center) {");
            builder.AppendLine("if (center == null) throw new global::System.ArgumentNullException(nameof(center));");
            builder.AppendLine("center.RegisterCatalog(new global::FUI.RedDot.RedDotCatalogDefinition(");
            builder.AppendLine("new global::FUI.RedDot.RedDotNodeDefinition[] {");
            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                builder.Append("new global::FUI.RedDot.RedDotNodeDefinition(").Append(field.Name).Append(", @\"")
                    .Append(Escape(field.Name)).Append("\", ").Append(AggregatorExpression(aggregators[field.Name])).AppendLine("),");
            }
            builder.AppendLine("}, new global::FUI.RedDot.RedDotEdgeDefinition[] {");
            for (var fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                var field = fields[fieldIndex];
                var fieldParents = parents[field.Name];
                for (var parentIndex = 0; parentIndex < fieldParents.Length; parentIndex++)
                {
                    builder.Append("new global::FUI.RedDot.RedDotEdgeDefinition(").Append(fieldParents[parentIndex]).Append(", ").Append(field.Name).AppendLine("),");
                }
            }
            builder.AppendLine("}, new global::FUI.RedDot.RedDotDynamicTemplate[] {");
            for (var index = 0; index < factories.Count; index++)
            {
                var factory = factories[index];
                builder.Append("new global::FUI.RedDot.RedDotDynamicTemplate(new global::FUI.RedDot.RedDotKey[] { ");
                for (var parentIndex = 0; parentIndex < factory.Parents.Length; parentIndex++)
                {
                    if (parentIndex > 0) builder.Append(", ");
                    builder.Append(factory.Parents[parentIndex]);
                }
                builder.Append(" }, @\"").Append(Escape(factory.Segment)).Append("\", @\"").Append(Escape(factory.Method.Name))
                    .Append("\", ").Append(AggregatorExpression(factory.Aggregator)).AppendLine("),");
            }
            builder.AppendLine("}));");
            builder.AppendLine("}");
            builder.AppendLine("}");
            if (namespaceName != null) builder.AppendLine("}");
            return builder.ToString();
        }

        static bool TryResolveStaticKey(
            Compilation compilation,
            IFieldSymbol field,
            Dictionary<string, IFieldSymbol> fields,
            Dictionary<string, string> cache,
            HashSet<string> visiting,
            out string key)
        {
            if (cache.TryGetValue(field.Name, out key)) return true;
            if (!visiting.Add(field.Name)) return false;
            var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            var invocation = syntax?.Initializer?.Value as InvocationExpressionSyntax;
            if (invocation == null)
            {
                key = null;
                visiting.Remove(field.Name);
                return false;
            }
            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var invokedMethod = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var arguments = invocation.ArgumentList.Arguments;
            if (IsRedDotKeyMethod(invokedMethod, "Root") && arguments.Count == 1 &&
                TryGetConstantString(compilation, arguments[0].Expression, out var root))
            {
                key = Uri.EscapeDataString(root);
                visiting.Remove(field.Name);
                return true;
            }
            if (IsRedDotKeyMethod(invokedMethod, "Child") && arguments.Count == 2 &&
                TryGetReferencedFieldName(semanticModel, arguments[0].Expression, out var parentName) &&
                fields.TryGetValue(parentName, out var parent) &&
                SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(arguments[0].Expression).Symbol, parent) &&
                TryResolveStaticKey(compilation, parent, fields, cache, visiting, out var parentKey) &&
                TryGetConstantString(compilation, arguments[1].Expression, out var child))
            {
                key = parentKey + "/" + Uri.EscapeDataString(child);
                visiting.Remove(field.Name);
                return true;
            }
            key = null;
            visiting.Remove(field.Name);
            return false;
        }

        static bool TryReadFactory(Compilation compilation, IMethodSymbol method, out string segment, out string canonicalParent)
        {
            segment = null;
            canonicalParent = null;
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public || method.ReturnType.ToDisplayString() != KeyType)
            {
                return false;
            }
            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
            ExpressionSyntax expression = syntax?.ExpressionBody?.Expression;
            if (expression == null && syntax?.Body != null)
            {
                expression = syntax.Body.Statements.OfType<ReturnStatementSyntax>().SingleOrDefault()?.Expression;
            }
            var invocation = expression as InvocationExpressionSyntax;
            if (invocation == null || method.Parameters.Length == 0)
            {
                return false;
            }
            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var invokedMethod = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var arguments = invocation.ArgumentList.Arguments;
            var parentField = arguments.Count > 0 ? semanticModel.GetSymbolInfo(arguments[0].Expression).Symbol as IFieldSymbol : null;
            if (!IsRedDotKeyMethod(invokedMethod, "Dynamic") || arguments.Count != method.Parameters.Length + 2 ||
                parentField == null || !SymbolEqualityComparer.Default.Equals(parentField.ContainingType, method.ContainingType) ||
                !TryGetConstantString(compilation, arguments[1].Expression, out segment))
            {
                return false;
            }
            canonicalParent = parentField.Name;

            // 工厂参数必须按声明顺序完整参与 Key，避免常量替代、遗漏、重复或换序造成静默冲突。
            for (var index = 0; index < method.Parameters.Length; index++)
            {
                var argumentSymbol = semanticModel.GetSymbolInfo(arguments[index + 2].Expression).Symbol;
                if (!SymbolEqualityComparer.Default.Equals(argumentSymbol, method.Parameters[index])) return false;
            }
            return true;
        }

        static bool ValidateParents(SourceProductionContext context, ISymbol member, string[] parents, Dictionary<string, IFieldSymbol> fields)
        {
            var valid = parents.Length > 0 || member is IFieldSymbol;
            for (var index = 0; index < parents.Length; index++)
            {
                if (!fields.ContainsKey(parents[index]))
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingParent, member.Locations.FirstOrDefault(), member.Name, parents[index]));
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateCycles(SourceProductionContext context, IFieldSymbol[] fields, Dictionary<string, string[]> parents)
        {
            var valid = true;
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < fields.Length; index++)
            {
                if (!Visit(fields[index].Name, parents, visiting, visited))
                {
                    context.ReportDiagnostic(Diagnostic.Create(StaticCycle, fields[index].Locations.FirstOrDefault(), fields[index].Name));
                    valid = false;
                }
            }
            return valid;
        }

        static bool Visit(string field, Dictionary<string, string[]> parents, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(field)) return true;
            if (!visiting.Add(field)) return false;
            if (parents.TryGetValue(field, out var fieldParents))
            {
                for (var index = 0; index < fieldParents.Length; index++)
                {
                    if (!Visit(fieldParents[index], parents, visiting, visited)) return false;
                }
            }
            visiting.Remove(field);
            visited.Add(field);
            return true;
        }

        static bool TryReadAggregator(SourceProductionContext context, AttributeData attribute, out INamedTypeSymbol aggregator)
        {
            aggregator = null;
            for (var index = 0; index < attribute.NamedArguments.Length; index++)
            {
                if (attribute.NamedArguments[index].Key == "Aggregator")
                {
                    aggregator = attribute.NamedArguments[index].Value.Value as INamedTypeSymbol;
                    break;
                }
            }
            if (aggregator == null) return true;
            var implements = aggregator.AllInterfaces.Any(item => item.ToDisplayString() == AggregatorInterface);
            var parameterless = aggregator.InstanceConstructors.Any(ctor => ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.Length == 0);
            var stateless = !aggregator.GetMembers().OfType<IFieldSymbol>().Any(field => !field.IsStatic);
            if (aggregator.DeclaredAccessibility == Accessibility.Public && !aggregator.IsAbstract && implements && parameterless && stateless)
            {
                return true;
            }
            context.ReportDiagnostic(Diagnostic.Create(InvalidAggregator, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(), aggregator.ToDisplayString()));
            return false;
        }

        static string[] ReadParents(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0) return Array.Empty<string>();
            var argument = attribute.ConstructorArguments[0];
            if (argument.Kind != TypedConstantKind.Array) return Array.Empty<string>();
            return argument.Values.Select(value => value.Value as string).Where(value => value != null).ToArray();
        }

        static bool IsAllowedParameter(ITypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Int32 || type.SpecialType == SpecialType.System_Int64 ||
                   type.SpecialType == SpecialType.System_String || type.ToDisplayString() == "System.Guid";
        }

        static bool TryGetConstantString(Compilation compilation, ExpressionSyntax expression, out string value)
        {
            var constant = compilation.GetSemanticModel(expression.SyntaxTree).GetConstantValue(expression);
            value = constant.HasValue ? constant.Value as string : null;
            return !string.IsNullOrEmpty(value);
        }

        static bool TryGetReferencedFieldName(SemanticModel semanticModel, ExpressionSyntax expression, out string name)
        {
            if (semanticModel.GetSymbolInfo(expression).Symbol is IFieldSymbol field)
            {
                name = field.Name;
                return true;
            }
            name = null;
            return false;
        }

        static bool IsRedDotKeyMethod(IMethodSymbol method, string name)
        {
            return method != null && method.IsStatic && method.Name == name && method.ContainingType.ToDisplayString() == KeyType;
        }

        static bool IsPartial(INamedTypeSymbol symbol)
        {
            return symbol.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax() is ClassDeclarationSyntax declaration &&
                declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));
        }

        static AttributeData FindAttribute(ISymbol symbol, string name)
        {
            return symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == name);
        }

        static string AggregatorExpression(INamedTypeSymbol aggregator)
        {
            return aggregator == null
                ? "new global::FUI.RedDot.SumAggregator()"
                : "new " + aggregator.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "()";
        }

        static string Escape(string value) => value.Replace("\"", "\"\"");

        static DiagnosticDescriptor Create(string id, string title, string message)
        {
            return new DiagnosticDescriptor(id, title, message, "FUI.RedDot", DiagnosticSeverity.Error, true);
        }

        sealed class FactoryInfo
        {
            public IMethodSymbol Method { get; }
            public string Segment { get; }
            public string[] Parents { get; }
            public INamedTypeSymbol Aggregator { get; }

            public FactoryInfo(IMethodSymbol method, string segment, string[] parents, INamedTypeSymbol aggregator)
            {
                Method = method;
                Segment = segment;
                Parents = parents;
                Aggregator = aggregator;
            }
        }
    }
}
