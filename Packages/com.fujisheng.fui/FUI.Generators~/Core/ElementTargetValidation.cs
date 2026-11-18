using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace FUI.Generators
{
    /// <summary>在生成前验证接口目标；保留使用者指定的控件类型。</summary>
    internal static class ElementTargetValidation
    {
        internal static bool Validate(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax declaration)
        {
            var type = compilation.GetSemanticModel(declaration.SyntaxTree).GetDeclaredSymbol(declaration) as INamedTypeSymbol;
            var valid = true;
            foreach (var part in Utility.GetSourceDeclarations(type))
            {
                var model = compilation.GetSemanticModel(part.SyntaxTree);
                foreach (var attribute in GetGeneratedAttributes(part))
                {
                    var constructor = model.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
                    var attributeName = constructor?.ContainingType.ToDisplayString();
                    if (attributeName != "FUI.Binding.BindAttribute" && attributeName != "FUI.Binding.CommandAttribute") { continue; }
                    var invocation = attribute.ArgumentList?.Arguments.Select(x => x.Expression).OfType<InvocationExpressionSyntax>().FirstOrDefault(x => x.Expression.ToString() == "nameof");
                    var member = invocation?.ArgumentList.Arguments.FirstOrDefault()?.Expression as MemberAccessExpressionSyntax;
                    if (member == null) { continue; }
                    var target = model.GetSymbolInfo(member.Expression).Symbol as INamedTypeSymbol;
                    if (target == null || target.TypeKind != TypeKind.Interface) { continue; }
                    var name = member.Name.Identifier.ValueText;
                    var candidates = target.GetMembers(name).Where(IsPublicBindingMember).ToArray();
                    if (candidates.Length == 0)
                    {
                        candidates = target.AllInterfaces.SelectMany(x => x.GetMembers(name)).Where(IsPublicBindingMember).Distinct(SymbolEqualityComparer.Default).ToArray();
                        // 更具体接口显式声明的同名成员遮蔽其祖先成员，符合C#接口查找。
                        candidates = candidates.Where(x => !candidates.Any(other => !SymbolEqualityComparer.Default.Equals(x, other) && other.ContainingType.AllInterfaces.Any(parent => SymbolEqualityComparer.Default.Equals(parent, x.ContainingType)))).ToArray();
                    }
                    if (candidates.Length == 1) { continue; }
                    valid = false;
                    context.ReportDiagnostic(Diagnostic.Create(candidates.Length == 0 ? DiagnosticDescriptors.MissingInterfaceTargetMember : DiagnosticDescriptors.AmbiguousInterfaceTargetMember, member.GetLocation(), target.ToDisplayString(), name));
                }
            }
            return valid;
        }

        // 与原 ContextInfoByAttributeGenerator 的直接成员及 Observable 字段边界保持一致。
        static IEnumerable<AttributeSyntax> GetGeneratedAttributes(ClassDeclarationSyntax declaration)
        {
            foreach (var member in declaration.Members)
            {
                if (member is PropertyDeclarationSyntax
                    || member is FieldDeclarationSyntax && Utility.HasAttribute(member.AttributeLists, "ObservableProperty", "ObservablePropertyAttribute"))
                {
                    if (Utility.TryGetPropertyBindingAttributes(member.AttributeLists, out var properties))
                    {
                        foreach (var attribute in properties) { yield return attribute; }
                    }
                }
                if (Utility.TryGetCommandAttributes(member.AttributeLists, out var commands))
                {
                    foreach (var attribute in commands) { yield return attribute; }
                }
            }
        }

        static bool IsPublicBindingMember(ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public && (symbol is IPropertySymbol || symbol is IFieldSymbol);
    }
}
