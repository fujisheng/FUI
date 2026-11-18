using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUI.Generators
{
    /// <summary>
    /// 生成强类型 Route Registry，并在编译期固化 Settings、Presenter 和依赖关系。
    /// </summary>
    internal sealed class RouteGenerator
    {
        const string SettingsAttributeName = "FUI.Navigation.RoutePolicyAttribute";
        const string PresenterAttributeName = "FUI.Binding.PresenterForAttribute";

        static readonly DiagnosticDescriptor MissingDependency = new DiagnosticDescriptor(
            "FUI0011",
            "Route dependency does not exist",
            "Route '{0}' references missing dependency view '{1}'",
            "FUI.Routing",
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor DependencyCycle = new DiagnosticDescriptor(
            "FUI0012",
            "Static route dependency cycle",
            "A static route dependency cycle was detected: {0}",
            "FUI.Routing",
            DiagnosticSeverity.Error,
            true);

        /// <summary>
        /// 保存生成强类型 Route 所需的 ViewModel、Presenter、策略和依赖信息。
        /// </summary>
        sealed class RouteInfo
        {
            public INamedTypeSymbol ViewModel;
            public INamedTypeSymbol Presenter;
            public string ViewName;
            public string AssetKey;
            public string OwnViewName;
            public string PropertyName;
            public AttributeData Settings;
            public bool IsDefaultPresenter;
            public readonly List<(INamedTypeSymbol viewModel, bool requiredBefore)> Dependencies
                = new List<(INamedTypeSymbol, bool)>();
        }

        internal void Generate(SourceProductionContext context, Compilation compilation, IEnumerable<ClassDeclarationSyntax> declarations)
        {
            // Route 的唯一变体由有效 View、ViewModel 和 Presenter 共同决定。
            var declarationList = declarations.ToArray();
            var allTypes = GetAllTypes(compilation.Assembly.GlobalNamespace).ToArray();
            var routes = new List<RouteInfo>();
            var hasBindingRegistry = false;
            foreach (var declaration in declarationList)
            {
                var model = compilation.GetSemanticModel(declaration.SyntaxTree).GetDeclaredSymbol(declaration) as INamedTypeSymbol;
                if (model != null && !model.IsAbstract && model.TypeParameters.Length == 0)
                {
                    hasBindingRegistry = true;
                }
                if (model == null || !TryGetEffectiveView(model, out var viewName, out var assetKey, out var ownViewName))
                {
                    continue;
                }

                var presenters = SelectPresenters(model, allTypes);
                for (var presenterIndex = 0; presenterIndex < presenters.Length; presenterIndex++)
                {
                    var route = new RouteInfo
                    {
                        ViewModel = model,
                        Presenter = presenters[presenterIndex],
                        ViewName = viewName,
                        AssetKey = assetKey,
                        OwnViewName = ownViewName,
                        PropertyName = BuildPropertyName(model.Name),
                        Settings = model.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == SettingsAttributeName),
                        IsDefaultPresenter = presenterIndex == 0,
                    };
                    ReadDependencies(route);
                    routes.Add(route);
                }
            }

            // 纯列表项或 Prototype 程序集仍需要公开初始化其 BindingRegistry。
            if (!hasBindingRegistry)
            {
                return;
            }

            MakePropertyNamesUnique(routes);
            var defaultByView = routes
                .GroupBy(route => route.ViewName)
                .ToDictionary(
                    group => group.Key,
                    group => group.FirstOrDefault(route => route.OwnViewName == group.Key && route.IsDefaultPresenter)
                        ?? group.FirstOrDefault(route => route.IsDefaultPresenter)
                        ?? group.First());
            var defaultByModel = new Dictionary<INamedTypeSymbol, RouteInfo>(SymbolEqualityComparer.Default);
            foreach (var route in routes.Where(route => route.IsDefaultPresenter))
            {
                if (!defaultByModel.ContainsKey(route.ViewModel))
                {
                    defaultByModel.Add(route.ViewModel, route);
                }
            }

            if (!ValidateDependencies(context, routes, defaultByModel))
            {
                return;
            }


            var moduleNamespace = Utility.GetModuleNamespace(compilation);
            var source = BuildRegistry(moduleNamespace, routes, defaultByView, defaultByModel);
            context.AddSource("Routes.g.cs", Utility.FormatCode(source));
        }

        static string BuildRegistry(
            string moduleNamespace,
            List<RouteInfo> routes,
            Dictionary<string, RouteInfo> defaultByView,
            Dictionary<INamedTypeSymbol, RouteInfo> defaultByModel)
        {
            var builder = new StringBuilder();
            builder.AppendLine(Utility.FileHeader);
            builder.AppendLine($"namespace {moduleNamespace}");
            builder.AppendLine("{");
            builder.AppendLine("public static partial class Routes");
            builder.AppendLine("{");
            foreach (var route in routes.OrderBy(route => route.PropertyName, StringComparer.Ordinal))
            {
                builder.AppendLine(BuildRoute(route, defaultByModel));
            }

            builder.AppendLine("    public static void Initialize() => BindingRegistry.Initialize();");
            if (routes.Count > 0)
            {
                builder.AppendLine("    public static bool TryResolveAsset(string assetKey, out global::FUI.Navigation.Route route)");
                builder.AppendLine("    {");
                builder.AppendLine("        switch (assetKey)");
                builder.AppendLine("        {");
                foreach (var pair in defaultByView.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    builder.AppendLine($"            case @\"{Escape(pair.Key)}\": route = {pair.Value.PropertyName}; return true;");
                }
                builder.AppendLine("            default: route = null; return false;");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
            }
            builder.AppendLine("}");
            builder.AppendLine("}");
            return builder.ToString();
        }

        static string BuildRoute(RouteInfo route, Dictionary<INamedTypeSymbol, RouteInfo> defaultByModel)
        {
            var vm = route.ViewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var presenter = route.Presenter?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::FUI.Presentation.EmptyPresenter";
            var contextName = $"{vm}.{route.ViewModel.Name}_{route.OwnViewName}_BindingContext";
            var settings = BuildSettings(route, defaultByModel);
            return $@"
    public static global::FUI.Navigation.Route<{vm}> {route.PropertyName} {{ get; }} =
        global::FUI.Infrastructure.GeneratedRouteFactory.Create<{vm}>(
            @""{Escape(route.AssetKey)}"",
            typeof({presenter}),
            static () => new {vm}(),
            static (view, viewModel) => new {contextName}(view, viewModel),
            static () => new {presenter}(),
            {settings});";
        }

        static string BuildSettings(RouteInfo route, Dictionary<INamedTypeSymbol, RouteInfo> defaultByModel)
        {
            var layer = 0;
            var allowMultiple = false;
            var takeFocus = true;
            string enterProvider = null;
            string exitProvider = null;
            var history = "FUI.Navigation.HistoryMode.Stack";
            var coverage = "FUI.Navigation.CoverageMode.KeepVisible";
            var cache = "FUI.Navigation.CacheMode.None";
            var cacheCapacity = 1;
            var cacheDuration = 0f;

            if (route.Settings != null)
            {
                var args = route.Settings.ConstructorArguments;
                if (args.Length > 0 && args[0].Value is int layerValue) layer = layerValue;
                if (args.Length > 1 && args[1].Value is INamedTypeSymbol enter) enterProvider = enter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (args.Length > 2 && args[2].Value is INamedTypeSymbol exit) exitProvider = exit.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                foreach (var pair in route.Settings.NamedArguments)
                {
                    switch (pair.Key)
                    {
                        case "EnterTransitionProvider": enterProvider = (pair.Value.Value as INamedTypeSymbol)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat); break;
                        case "ExitTransitionProvider": exitProvider = (pair.Value.Value as INamedTypeSymbol)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat); break;
                        case "HistoryMode": history = $"(FUI.Navigation.HistoryMode){Convert.ToInt32(pair.Value.Value)}"; break;
                        case "CoverageMode": coverage = $"(FUI.Navigation.CoverageMode){Convert.ToInt32(pair.Value.Value)}"; break;
                        case "CacheMode": cache = $"(FUI.Navigation.CacheMode){Convert.ToInt32(pair.Value.Value)}"; break;
                        case "CacheCapacity": cacheCapacity = (int)pair.Value.Value; break;
                        case "CacheDuration": cacheDuration = Convert.ToSingle(pair.Value.Value); break;
                        case "TakeFocus": takeFocus = (bool)pair.Value.Value; break;
                        case "AllowMultiple": allowMultiple = (bool)pair.Value.Value; break;
                    }
                }
            }

            var dependencies = route.Dependencies.Count == 0
                ? "null"
                : "new FUI.Navigation.RouteDependency[] { " + string.Join(", ", route.Dependencies.Select(dependency =>
                {
                    var target = defaultByModel[dependency.viewModel];
                    var timing = dependency.requiredBefore ? "RequiredBefore" : "AttachedAfter";
                    return $"new FUI.Navigation.RouteDependency(FUI.Navigation.DependencyTiming.{timing}, static () => {target.PropertyName})";
                })) + " }";

            return $@"new FUI.Navigation.RoutePolicy(
                layer: {LayerExpression(layer)},
                historyMode: {history},
                coverageMode: {coverage},
                cacheMode: {cache},
                allowMultiple: {allowMultiple.ToString().ToLowerInvariant()},
                cacheCapacity: {cacheCapacity},
                cacheDuration: {cacheDuration.ToString(System.Globalization.CultureInfo.InvariantCulture)}f,
                enterTransitionProvider: {(enterProvider == null ? "null" : $"new {enterProvider}()")},
                exitTransitionProvider: {(exitProvider == null ? "null" : $"new {exitProvider}()")},
                dependencies: {dependencies}){(takeFocus ? "" : ".WithFocus(false)")}";
        }

        static bool ValidateDependencies(
            SourceProductionContext context,
            List<RouteInfo> routes,
            Dictionary<INamedTypeSymbol, RouteInfo> defaultByModel)
        {
            var valid = true;
            foreach (var route in routes)
            {
                foreach (var dependency in route.Dependencies)
                {
                    if (defaultByModel.ContainsKey(dependency.viewModel))
                    {
                        continue;
                    }
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingDependency,
                        route.ViewModel.Locations.FirstOrDefault(),
                        route.PropertyName,
                        dependency.viewModel.ToDisplayString()));
                    valid = false;
                }
            }

            if (!valid)
            {
                return false;
            }

            // 静态依赖环在编译期直接报错；运行时仍保留动态环防御。
            var visiting = new HashSet<RouteInfo>();
            var visited = new HashSet<RouteInfo>();
            var path = new List<RouteInfo>();
            foreach (var route in routes)
            {
                if (FindCycle(route, defaultByModel, visiting, visited, path, out var cycle))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DependencyCycle, route.ViewModel.Locations.FirstOrDefault(), string.Join(" -> ", cycle.Select(item => item.PropertyName))));
                    return false;
                }
            }
            return true;
        }

        static bool FindCycle(
            RouteInfo route,
            Dictionary<INamedTypeSymbol, RouteInfo> defaultByModel,
            HashSet<RouteInfo> visiting,
            HashSet<RouteInfo> visited,
            List<RouteInfo> path,
            out List<RouteInfo> cycle)
        {
            cycle = null;
            if (visited.Contains(route)) return false;
            if (!visiting.Add(route))
            {
                var start = path.IndexOf(route);
                cycle = path.Skip(start).Concat(new[] { route }).ToList();
                return true;
            }

            path.Add(route);
            foreach (var dependency in route.Dependencies)
            {
                if (FindCycle(defaultByModel[dependency.viewModel], defaultByModel, visiting, visited, path, out cycle))
                {
                    return true;
                }
            }
            path.RemoveAt(path.Count - 1);
            visiting.Remove(route);
            visited.Add(route);
            return false;
        }

        static void ReadDependencies(RouteInfo route)
        {
            if (route.Settings == null) return;
            foreach (var pair in route.Settings.NamedArguments)
            {
                if (pair.Key == "RequiredBefore")
                {
                    AddTypedDependencies(pair.Value, true, route.Dependencies);
                }
                else if (pair.Key == "AttachedAfter")
                {
                    AddTypedDependencies(pair.Value, false, route.Dependencies);
                }
            }
        }

        static void AddTypedDependencies(
            TypedConstant constant,
            bool requiredBefore,
            List<(INamedTypeSymbol viewModel, bool requiredBefore)> output)
        {
            if (constant.Kind != TypedConstantKind.Array || constant.IsNull) return;
            foreach (var value in constant.Values)
            {
                if (value.Value is INamedTypeSymbol viewModel
                    && TryGetEffectiveView(viewModel, out _, out _, out _))
                {
                    AddDependency(viewModel, requiredBefore, output);
                }
            }
        }

        static void AddDependency(
            INamedTypeSymbol viewModel,
            bool requiredBefore,
            List<(INamedTypeSymbol viewModel, bool requiredBefore)> output)
        {
            if (!output.Any(item =>
                    SymbolEqualityComparer.Default.Equals(item.viewModel, viewModel)
                    && item.requiredBefore == requiredBefore))
            {
                output.Add((viewModel, requiredBefore));
            }
        }

        static INamedTypeSymbol[] SelectPresenters(INamedTypeSymbol viewModel, INamedTypeSymbol[] allTypes)
        {
            var attributed = allTypes.Where(type => type.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == PresenterAttributeName
                    && attribute.ConstructorArguments.Length > 0
                    && SymbolEqualityComparer.Default.Equals(attribute.ConstructorArguments[0].Value as ITypeSymbol, viewModel)))
                .OrderBy(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal);

            var candidates = allTypes.Where(type => !type.IsAbstract && type.AllInterfaces.Any(@interface =>
                @interface.OriginalDefinition.ToDisplayString() == "FUI.Presentation.IPresenter<TObservableObject>"
                && @interface.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], viewModel)));
            var presenters = attributed
                .Concat(candidates.OrderBy(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal))
                .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                .ToArray();
            return presenters.Length == 0 ? new INamedTypeSymbol[] { null } : presenters;
        }

        static bool TryGetEffectiveView(INamedTypeSymbol model, out string viewName, out string assetKey, out string ownViewName)
        {
            viewName = null;
            assetKey = null;
            ownViewName = string.Empty;
            // 派生 ViewModel 可以继承基类 ViewContract，并形成自己的 Route 变体。
            for (var current = model; current != null; current = current.BaseType)
            {
                var attribute = current.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "FUI.Binding.ViewContractAttribute");
                if (attribute == null) continue;
                var name = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
                var source = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value as string : name;
                if (current.Equals(model, SymbolEqualityComparer.Default)) ownViewName = name ?? string.Empty;
                viewName = name;
                assetKey = source;
                return !string.IsNullOrEmpty(viewName);
            }
            return false;
        }

        static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
        {
            foreach (var type in root.GetTypeMembers())
            {
                yield return type;
                foreach (var nested in GetNestedTypes(type)) yield return nested;
            }
            foreach (var child in root.GetNamespaceMembers())
            {
                foreach (var type in GetAllTypes(child)) yield return type;
            }
        }

        static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (var descendant in GetNestedTypes(nested)) yield return descendant;
            }
        }

        static void MakePropertyNamesUnique(List<RouteInfo> routes)
        {
            // 默认 Presenter 保留最短属性名，其余变体追加 Presenter 名并做稳定去重。
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in routes
                .OrderByDescending(route => route.IsDefaultPresenter)
                .ThenBy(route => route.ViewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                .ThenBy(route => route.Presenter?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal))
            {
                var candidate = route.PropertyName;
                if (!route.IsDefaultPresenter && route.Presenter != null)
                {
                    candidate += TrimPresenterSuffix(route.Presenter.Name);
                }
                var unique = candidate;
                var suffix = 2;
                while (!used.Add(unique))
                {
                    unique = candidate + suffix++;
                }
                route.PropertyName = unique;
            }
        }

        static string TrimPresenterSuffix(string name)
        {
            return name.EndsWith("Presenter", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Presenter".Length)
                : name;
        }

        static string BuildPropertyName(string typeName)
        {
            if (typeName.EndsWith("ViewModel", StringComparison.Ordinal)) return typeName.Substring(0, typeName.Length - "Model".Length);
            if (typeName.EndsWith("Model", StringComparison.Ordinal)) return typeName.Substring(0, typeName.Length - "Model".Length) + "View";
            return typeName.EndsWith("View", StringComparison.Ordinal) ? typeName : typeName + "View";
        }

        static string LayerExpression(int value)
        {
            return value switch
            {
                -30000 => "FUI.Navigation.Layer.Background",
                -20000 => "FUI.Navigation.Layer.Scene",
                8000 => "FUI.Navigation.Layer.Popup",
                16000 => "FUI.Navigation.Layer.Tips",
                24000 => "FUI.Navigation.Layer.Overlay",
                30000 => "FUI.Navigation.Layer.Top",
                _ => "FUI.Navigation.Layer.Panel",
            };
        }

        static string Escape(string value) => value?.Replace("\"", "\"\"") ?? string.Empty;

    }
}
