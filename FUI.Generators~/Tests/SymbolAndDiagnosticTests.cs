using FUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using AnalyzerUtility = FUI.Analyzer.Utility;

namespace FUI.Generators.Tests
{
    public sealed class SymbolAndDiagnosticTests
    {
        const string FrameworkSource = @"
using System;
namespace FUI.Binding
{
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
    public interface IValueConverter<TSource, TTarget> { }
    public class CommandTemplate<T> { }
    public class ObservableObject { }
}
namespace FUI.Presentation
{
    public interface ISynchronizeProperties { void Synchronize(); }
}
namespace FUI.Infrastructure.Binding
{
    public class ContextDescriptor<T> { }
}
public sealed class ObservableItem : FUI.Binding.INotifyPropertyChanged, FUI.Presentation.ISynchronizeProperties
{
    public void Synchronize() { }
}
public sealed class ObservableCollection : FUI.Binding.INotifyCollectionChanged { }
public sealed class StringProperty : FUI.Binding.IBindableProperty<string> { }
public sealed class NumberConverter : FUI.Binding.IValueConverter<int, string> { }
public sealed class TypedCommand : FUI.Binding.CommandTemplate<Action<int, string>> { }
public sealed class ItemDescriptor : FUI.Infrastructure.Binding.ContextDescriptor<ObservableItem> { }
public sealed class PlainType { }
";

        [Test]
        public void FrameworkSymbolExtensionsRecognizeBindingContracts()
        {
            var compilation = CreateCompilation(FrameworkSource);
            var observable = compilation.GetTypeByMetadataName("ObservableItem");
            var collection = compilation.GetTypeByMetadataName("ObservableCollection");
            var property = compilation.GetTypeByMetadataName("StringProperty");
            var converter = compilation.GetTypeByMetadataName("NumberConverter");
            var command = compilation.GetTypeByMetadataName("TypedCommand");
            var descriptor = compilation.GetTypeByMetadataName("ItemDescriptor");
            var plain = compilation.GetTypeByMetadataName("PlainType");

            Assert.That(observable.IsObservableObject(), Is.True);
            Assert.That(observable.IsISynchronizeProperties(), Is.True);
            Assert.That(collection.IsObservableList(), Is.True);
            Assert.That(property.IsBindableProperty(out var propertyType), Is.True);
            Assert.That(propertyType.SpecialType, Is.EqualTo(SpecialType.System_String));
            Assert.That(converter.IsValueConverter(out var sourceType, out var targetType), Is.True);
            Assert.That(sourceType.SpecialType, Is.EqualTo(SpecialType.System_Int32));
            Assert.That(targetType.SpecialType, Is.EqualTo(SpecialType.System_String));
            Assert.That(command.IsCommand(out var commandArguments), Is.True);
            Assert.That(commandArguments.Select(item => item.SpecialType), Is.EqualTo(new[]
            {
                SpecialType.System_Int32,
                SpecialType.System_String,
            }));
            Assert.That(descriptor.IsContextDescriptor(out var viewModelType), Is.True);
            Assert.That(viewModelType, Is.EqualTo(observable));
            Assert.That(plain.IsObservableObject(), Is.False);
            Assert.That(plain.IsBindableProperty(out _), Is.False);
            Assert.That(plain.IsCommand(out _), Is.False);
            Assert.That(plain.IsValueConverter(out _, out _), Is.False);
            Assert.That(plain.IsContextDescriptor(out _), Is.False);
        }

        [Test]
        public void TypeSymbolExtensionsMatchSpecialArrayGenericAndInheritanceTypes()
        {
            var compilation = CreateCompilation(FrameworkSource);
            var observable = compilation.GetTypeByMetadataName("ObservableItem");
            var notify = compilation.GetTypeByMetadataName("FUI.Binding.INotifyPropertyChanged");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
            var floatType = compilation.GetSpecialType(SpecialType.System_Single);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            var arrayType = compilation.CreateArrayTypeSymbol(intType);

            Assert.That(observable.Extends(notify), Is.True);
            Assert.That(notify.Extends(observable), Is.False);
            Assert.That(intType.Matches(typeof(int)), Is.True);
            Assert.That(boolType.Matches(typeof(bool)), Is.True);
            Assert.That(floatType.Matches(typeof(float)), Is.True);
            Assert.That(voidType.Matches(typeof(void)), Is.True);
            Assert.That(arrayType.Matches(typeof(int[])), Is.True);
            Assert.That(observable.GetBaseTypesAndThis().First(), Is.EqualTo(observable));
            Assert.That(((ITypeSymbol)null).Extends(notify), Is.False);
        }

        [Test]
        public void AnalyzerUtilityReadsLiteralAndConstStringArguments()
        {
            const string source = @"
using System;
public sealed class PathAttribute : Attribute { public PathAttribute(string path) { } }
public sealed class Target
{
    const string ElementPath = ""Root"";
    [Path(ElementPath)]
    public void Execute() { }
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();
            var argument = attribute.ArgumentList.Arguments.Single().Expression;
            var literal = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("Literal"));

            Assert.That(AnalyzerUtility.TryGetStringConstant(model, argument), Is.EqualTo("Root"));
            Assert.That(AnalyzerUtility.TryGetStringConstant(model, literal), Is.EqualTo("Literal"));
            Assert.That(AnalyzerUtility.TryGetStringConstant(model, null), Is.Null);
            Assert.That(AnalyzerUtility.FindFirstStringConstantArgument(model, attribute), Is.EqualTo("Root"));
            Assert.That(AnalyzerUtility.FindFirstStringConstantArgument(model, null), Is.Null);
        }

        [Test]
        public void DiagnosticCatalogUsesStableFuiIdentifiersAndSeverities()
        {
            var descriptors = typeof(DiagnosticDescriptors)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(field => (DiagnosticDescriptor)field.GetValue(null))
                .ToArray();

            Assert.That(descriptors.Select(item => item.Id), Is.Unique);
            Assert.That(descriptors.All(item => item.Id.StartsWith("FUI", StringComparison.Ordinal)), Is.True);
            Assert.That(descriptors.Count(item => item.DefaultSeverity == DiagnosticSeverity.Error), Is.EqualTo(9));
            Assert.That(descriptors.Count(item => item.DefaultSeverity == DiagnosticSeverity.Warning), Is.EqualTo(4));
            Assert.That(descriptors.Count(item => item.DefaultSeverity == DiagnosticSeverity.Info), Is.EqualTo(3));
        }

        static CSharpCompilation CreateCompilation(string source)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            return CSharpCompilation.Create(
                "SymbolTest",
                new[] { CSharpSyntaxTree.ParseText(source, path: "SymbolTest.cs") },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
