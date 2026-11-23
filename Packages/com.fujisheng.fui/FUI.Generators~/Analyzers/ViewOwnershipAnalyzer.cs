using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FUI.Generators.Analyzers
{
    /// <summary>Prevents handwritten ViewModels and presenters from acquiring renderer-owned UI objects.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ViewOwnershipAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor rule = new(
            "FUI0301",
            "Use ViewModel bindings to drive the view",
            "Business code cannot access view objects of type '{0}'. Use ViewModel bindings.",
            "FUI.Binding",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var compilation = start.Compilation;
                var viewModel = compilation.GetTypeByMetadataName("FUI.Presentation.ViewModel");
                var observable = compilation.GetTypeByMetadataName("FUI.Binding.ObservableObject");
                var presenter = compilation.GetTypeByMetadataName("FUI.Presentation.IPresenter");
                var viewContract = compilation.GetTypeByMetadataName("FUI.Binding.ViewContractAttribute");
                var bind = compilation.GetTypeByMetadataName("FUI.Binding.BindAttribute");
                var command = compilation.GetTypeByMetadataName("FUI.Binding.CommandAttribute");
                var view = compilation.GetTypeByMetadataName("FUI.Rendering.IView");
                if (view == null || viewModel == null)
                {
                    return;
                }

                var rendererTypes = new[]
                {
                    view,
                    compilation.GetTypeByMetadataName("FUI.Rendering.IElement"),
                    compilation.GetTypeByMetadataName("FUI.Presentation.ProjectionTemplate"),
                    compilation.GetTypeByMetadataName("FUI.Presentation.ProjectionHandle`1"),
                    compilation.GetTypeByMetadataName("FUI.Resources.IViewLease"),
                    compilation.GetTypeByMetadataName("FUI.Resources.IViewProvider"),
                    compilation.GetTypeByMetadataName("URUI.Elements.Element"),
                    compilation.GetTypeByMetadataName("URUI.Documents.Document"),
                    compilation.GetTypeByMetadataName("UnityEngine.EventSystems.UIBehaviour"),
                    compilation.GetTypeByMetadataName("UnityEngine.Canvas"),
                    compilation.GetTypeByMetadataName("UnityEngine.CanvasGroup"),
                    compilation.GetTypeByMetadataName("UnityEngine.RectTransform")
                }.Where(type => type != null).ToImmutableHashSet(SymbolEqualityComparer.Default);

                var businessOwners = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

                bool IsBusinessOwner(INamedTypeSymbol owner)
                {
                    var isObservable = false;
                    for (var current = owner; current != null; current = current.BaseType)
                    {
                        if (SymbolEqualityComparer.Default.Equals(current, viewModel) ||
                            current.GetAttributes().Any(attribute =>
                                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, viewContract)))
                        {
                            return true;
                        }
                        isObservable |= SymbolEqualityComparer.Default.Equals(current, observable);
                    }

                    if (owner.AllInterfaces.Any(contract => SymbolEqualityComparer.Default.Equals(contract, presenter)))
                    {
                        return true;
                    }
                    if (!isObservable)
                    {
                        return false;
                    }

                    for (var current = owner; current != null; current = current.BaseType)
                    {
                        if (current.GetMembers().Any(member => member.GetAttributes().Any(attribute =>
                            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bind) ||
                            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, command))))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                bool IsBusinessType(INamedTypeSymbol type)
                {
                    for (var owner = type; owner != null; owner = owner.ContainingType)
                    {
                        if (businessOwners.GetOrAdd(owner, IsBusinessOwner))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                ITypeSymbol FindViewType(ITypeSymbol type)
                {
                    if (type is IArrayTypeSymbol array)
                    {
                        return FindViewType(array.ElementType);
                    }

                    if (!(type is INamedTypeSymbol named))
                    {
                        return null;
                    }

                    for (var current = named; current != null; current = current.BaseType)
                    {
                        if (rendererTypes.Contains(current.OriginalDefinition))
                        {
                            return type;
                        }
                    }

                    if (named.AllInterfaces.Any(contract => rendererTypes.Contains(contract.OriginalDefinition)))
                    {
                        return type;
                    }

                    foreach (var argument in named.TypeArguments)
                    {
                        var nested = FindViewType(argument);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }

                    return null;
                }

                start.RegisterSymbolAction(symbolContext =>
                {
                    var symbol = symbolContext.Symbol;
                    if (symbol.IsImplicitlyDeclared || !IsBusinessType(symbol.ContainingType))
                    {
                        return;
                    }

                    var type = symbol switch
                    {
                        IFieldSymbol field => field.Type,
                        IPropertySymbol property => property.Type,
                        IEventSymbol declaredEvent => declaredEvent.Type,
                        IParameterSymbol parameter => parameter.Type,
                        IMethodSymbol method => method.ReturnType,
                        _ => null
                    };
                    var illegal = FindViewType(type);
                    var location = symbol.Locations.FirstOrDefault(value => value.IsInSource);
                    if (illegal != null && location != null)
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(rule, location, illegal.ToDisplayString()));
                    }
                }, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event, SymbolKind.Parameter, SymbolKind.Method);

                start.RegisterOperationAction(operationContext =>
                {
                    if (!IsBusinessType(operationContext.ContainingSymbol.ContainingType))
                    {
                        return;
                    }

                    var operation = operationContext.Operation;
                    for (var parent = operation.Parent; parent != null; parent = parent.Parent)
                    {
                        if (parent.Kind == OperationKind.NameOf)
                        {
                            return;
                        }
                    }

                    var illegal = FindViewType(operation.Type);
                    if (illegal == null && operation is IInvocationOperation invocation)
                    {
                        illegal = FindViewType(invocation.Instance?.Type ?? invocation.TargetMethod.ContainingType);
                    }
                    if (illegal == null && operation is IVariableDeclaratorOperation declaration)
                    {
                        illegal = FindViewType(declaration.Symbol.Type);
                    }

                    if (illegal != null)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(rule, operation.Syntax.GetLocation(), illegal.ToDisplayString()));
                    }
                }, OperationKind.Invocation, OperationKind.Conversion, OperationKind.ObjectCreation,
                    OperationKind.FieldReference, OperationKind.PropertyReference, OperationKind.VariableDeclarator);
            });
        }
    }
}
