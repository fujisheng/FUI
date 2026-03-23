using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FUI.SourceGenerator
{
    /// <summary>
    /// FUI 诊断报告器 - 用于在源文件中精确定位和报告诊断信息。
    /// </summary>
    public class FUIDiagnosticReporter
    {
        private readonly GeneratorExecutionContext context;

        public FUIDiagnosticReporter(GeneratorExecutionContext context)
        {
            this.context = context;
        }

        public void ReportError(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            Report(descriptor, location, messageArgs);
        }

        public void ReportWarning(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            Report(descriptor, location, messageArgs);
        }

        public void ReportInfo(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            Report(descriptor, location, messageArgs);
        }

        private void Report(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        public static Location CreateLocation(string filePath, LocationInfo locationInfo)
        {
            if (string.IsNullOrEmpty(filePath) || locationInfo == null)
            {
                return Location.None;
            }

            var lineSpan = new LinePositionSpan(
                new LinePosition(locationInfo.line - 1, locationInfo.column - 1),
                new LinePosition(locationInfo.line - 1, locationInfo.column));

            return Location.Create(filePath, new TextSpan(0, 0), lineSpan);
        }

        public static Location CreateLocation(SyntaxNode node)
        {
            return node?.GetLocation() ?? Location.None;
        }

        public static Location CreateAttributeLocation(AttributeSyntax attribute)
        {
            return attribute?.Name?.GetLocation() ?? Location.None;
        }

        public static Location GetArgumentLocation(AttributeSyntax attribute, int argumentIndex)
        {
            if (attribute?.ArgumentList?.Arguments == null)
            {
                return attribute?.GetLocation() ?? Location.None;
            }

            var arguments = attribute.ArgumentList.Arguments;
            if (argumentIndex < arguments.Count)
            {
                return arguments[argumentIndex].Expression.GetLocation();
            }

            return attribute.GetLocation();
        }

        public void ReportMissingObservableObject(ClassDeclarationSyntax classDeclaration, string className)
        {
            ReportError(DiagnosticDescriptors.MissingObservableObject, classDeclaration.Identifier.GetLocation(), className);
        }

        public void ReportInvalidBindingTarget(PropertyDeclarationSyntax property, string reason)
        {
            ReportError(DiagnosticDescriptors.InvalidBindingTarget, property.Identifier.GetLocation(), property.Identifier.Text, reason);
        }

        public void ReportMissingElementPath(PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            ReportError(DiagnosticDescriptors.MissingElementPath, CreateAttributeLocation(attribute), property.Identifier.Text);
        }

        public void ReportInvalidNameofExpression(PropertyDeclarationSyntax property, AttributeSyntax attribute)
        {
            var location = GetArgumentLocation(attribute, 1);
            if (location == Location.None)
            {
                location = property.Identifier.GetLocation();
            }

            ReportError(DiagnosticDescriptors.InvalidNameofExpression, location, property.Identifier.Text);
        }

        public void ReportTwoWayBindingRequiresSetter(PropertyDeclarationSyntax property)
        {
            ReportError(DiagnosticDescriptors.TwoWayBindingRequiresSetter, property.Identifier.GetLocation(), property.Identifier.Text);
        }

        public void ReportMissingCommandElementPath(MemberDeclarationSyntax member, string memberName, AttributeSyntax attribute)
        {
            var location = GetArgumentLocation(attribute, 0);
            if (location == Location.None)
            {
                location = member switch
                {
                    MethodDeclarationSyntax method => method.Identifier.GetLocation(),
                    EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.First().Identifier.GetLocation(),
                    _ => member.GetLocation(),
                };
            }

            ReportError(DiagnosticDescriptors.MissingCommandElementPath, location, memberName);
        }

        public void ReportInvalidValueConverter(PropertyDeclarationSyntax property, string converterTypeName)
        {
            ReportError(DiagnosticDescriptors.InvalidValueConverter, property.Identifier.GetLocation(), property.Identifier.Text, converterTypeName);
        }

        public void ReportTypeMismatch(PropertyDeclarationSyntax property, string sourceType, string targetType)
        {
            ReportWarning(DiagnosticDescriptors.TypeMismatch, property.Identifier.GetLocation(), property.Identifier.Text, sourceType, targetType);
        }

        public void ReportMissingViewBinding(ClassDeclarationSyntax classDeclaration, string className)
        {
            ReportWarning(DiagnosticDescriptors.MissingViewBinding, classDeclaration.Identifier.GetLocation(), className);
        }

        public void ReportGeneratorException(string className, string exceptionMessage)
        {
            ReportError(DiagnosticDescriptors.GeneratorException, Location.None, className, exceptionMessage);
        }

        [Conditional("DEBUG")]
        public void ReportDebugInfo(string message)
        {
            ReportInfo(DiagnosticDescriptors.DebugInfo, Location.None, message);
        }
    }
}
