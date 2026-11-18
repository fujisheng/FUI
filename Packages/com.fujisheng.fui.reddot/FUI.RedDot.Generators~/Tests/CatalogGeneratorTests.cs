using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace FUI.RedDot.Generators.Tests
{
    public sealed class CatalogGeneratorTests
    {
        const string Framework = @"
using System;
using FUI.RedDot;
namespace FUI.RedDot
{
	    public readonly struct RedDotKey
	    {
	        public static RedDotKey Root(string value) => default;
	        public static RedDotKey Child(RedDotKey parent, string value) => default;
	        public static RedDotKey Dynamic(RedDotKey parent, string value, int id) => default;
	        public static RedDotKey Dynamic(RedDotKey parent, string value, params RedDotArgument[] ids) => default;
	    }
	    public readonly struct RedDotArgument
	    {
	        public static implicit operator RedDotArgument(int value) => default;
	        public static implicit operator RedDotArgument(long value) => default;
	        public static implicit operator RedDotArgument(string value) => default;
	        public static implicit operator RedDotArgument(Guid value) => default;
	    }
    public readonly struct RedDotValue { }
    public readonly struct RedDotAggregationInput { }
    public interface IRedDotAggregator { RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> input); }
    public sealed class SumAggregator : IRedDotAggregator { public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> input) => default; }
    public sealed class RedDotCenter { public void RegisterCatalog(RedDotCatalogDefinition value) { } }
    public sealed class RedDotCatalogDefinition { public RedDotCatalogDefinition(RedDotNodeDefinition[] n, RedDotEdgeDefinition[] e, RedDotDynamicTemplate[] d) { } }
    public readonly struct RedDotNodeDefinition { public RedDotNodeDefinition(RedDotKey key, string name, IRedDotAggregator aggregator) { } }
    public readonly struct RedDotEdgeDefinition { public RedDotEdgeDefinition(RedDotKey parent, RedDotKey child) { } }
    public readonly struct RedDotDynamicTemplate { public RedDotDynamicTemplate(RedDotKey[] parents, string segment, string name, IRedDotAggregator aggregator) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class RedDotCatalogAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RedDotNodeAttribute : Attribute
    {
        public Type Aggregator { get; set; }
        public RedDotNodeAttribute(params string[] parents) { }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RedDotFactoryAttribute : Attribute
    {
        public Type Aggregator { get; set; }
        public RedDotFactoryAttribute(params string[] parents) { }
    }
}
";

        [Test]
        public void GeneratesStaticEdgesAndDynamicTemplate()
        {
            const string source = Framework + @"
[RedDotCatalog]
public static partial class GameRedDots
{
    [RedDotNode] public static readonly RedDotKey Home = RedDotKey.Root(""Home"");
    [RedDotNode(nameof(Home), Aggregator = typeof(SumAggregator))]
    public static readonly RedDotKey Mail = RedDotKey.Child(Home, ""Mail"");
    [RedDotFactory(nameof(Home))]
    public static RedDotKey HeroUpgrade(int heroId) { return RedDotKey.Dynamic(Home, ""HeroUpgrade"", heroId); }
}
";
            var result = Run(source);
            Assert.That(result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            var generated = result.GeneratedTrees.Single().ToString();
            Assert.That(generated, Does.Contain("Register"));
            Assert.That(generated, Does.Contain("RedDotEdgeDefinition(Home, Mail)"));
            Assert.That(generated, Does.Contain("RedDotDynamicTemplate"));
            Assert.That(generated, Does.Contain("@\"HeroUpgrade\""));
            Assert.That(generated, Does.Contain("Register(global::FUI.RedDot.RedDotCenter center)"));
            Assert.That(generated, Does.Not.Contain("global::FUI.RedDot." + "Center"));
            Assert.That(generated, Does.Not.Contain("global::FUI.RedDot." + "Key[]"));
        }

        [Test]
        public void ReportsDuplicateResolvedKeys()
        {
            const string source = Framework + @"
[RedDotCatalog]
public static partial class BadDots
{
    [RedDotNode] public static readonly RedDotKey First = RedDotKey.Root(""Same"");
    [RedDotNode] public static readonly RedDotKey Second = RedDotKey.Root(""Same"");
}
";
            var result = Run(source);
            Assert.That(result.Diagnostics.Select(item => item.Id), Does.Contain("RDT0003"));
        }

        [Test]
        public void ReportsInvalidFieldAndFactoryShape()
        {
            const string source = Framework + @"
[RedDotCatalog]
public static partial class BadShapes
{
    [RedDotNode] static readonly RedDotKey Hidden = RedDotKey.Root(""Hidden"");
    [RedDotFactory(nameof(Hidden))]
    public static int Wrong(int id) => id;
}
";
            var result = Run(source);
            var ids = result.Diagnostics.Select(item => item.Id).ToArray();
            Assert.That(ids, Does.Contain("RDT0002"));
            Assert.That(ids, Does.Contain("RDT0006"));
        }

        [Test]
        public void ReportsMissingParentCycleInvalidParameterAndAggregator()
        {
            const string source = Framework + @"
public sealed class StatefulAggregator : IRedDotAggregator
{
    int state;
    public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> input) => default;
}
[RedDotCatalog]
public static partial class BadDots
{
    [RedDotNode(nameof(B), Aggregator = typeof(StatefulAggregator))]
    public static readonly RedDotKey A = RedDotKey.Root(""A"");
    [RedDotNode(nameof(A))] public static readonly RedDotKey B = RedDotKey.Root(""B"");
    [RedDotFactory(""Missing"")]
    public static RedDotKey Dynamic(double id) => RedDotKey.Dynamic(A, ""Dynamic"", 1);
}
";
            var result = Run(source);
            var ids = result.Diagnostics.Select(item => item.Id).ToArray();
            Assert.That(ids, Does.Contain("RDT0004"));
            Assert.That(ids, Does.Contain("RDT0005"));
            Assert.That(ids, Does.Contain("RDT0007"));
            Assert.That(ids, Does.Contain("RDT0008"));
        }

        [Test]
        public void ReportsFactoriesThatIgnoreDuplicateOrReorderParameters()
        {
            const string source = Framework + @"
[RedDotCatalog]
public static partial class BadFactories
{
    [RedDotNode] public static readonly RedDotKey Home = RedDotKey.Root(""Home"");
    [RedDotFactory(nameof(Home))]
    public static RedDotKey Ignored(int heroId) => RedDotKey.Dynamic(Home, ""Ignored"", 1);
    [RedDotFactory(nameof(Home))]
    public static RedDotKey Duplicated(int heroId, long skinId) => RedDotKey.Dynamic(Home, ""Duplicated"", heroId, heroId);
    [RedDotFactory(nameof(Home))]
    public static RedDotKey Reordered(int heroId, long skinId) => RedDotKey.Dynamic(Home, ""Reordered"", skinId, heroId);
}
";
            var result = Run(source);
            Assert.That(result.Diagnostics.Count(item => item.Id == "RDT0006"), Is.EqualTo(3));
        }

        [Test]
        public void ReportsMethodsNamedLikeRedDotKeyMethodsFromAnotherType()
        {
            const string source = Framework + @"
public static class FakeKey
{
    public static RedDotKey Root(string value) => default;
    public static RedDotKey Dynamic(RedDotKey parent, string value, int id) => default;
}
[RedDotCatalog]
public static partial class BadSymbols
{
    [RedDotNode] public static readonly RedDotKey Home = FakeKey.Root(""Home"");
    [RedDotFactory(nameof(Home))]
    public static RedDotKey Entry(int id) => FakeKey.Dynamic(Home, ""Entry"", id);
}
";
            var result = Run(source);
            var ids = result.Diagnostics.Select(item => item.Id).ToArray();
            Assert.That(ids, Does.Contain("RDT0002"));
            Assert.That(ids, Does.Contain("RDT0006"));
        }

        [Test]
        public void AcceptsMultipleParametersWhenEveryParameterIsUsedInOrder()
        {
            const string source = Framework + @"
[RedDotCatalog]
public static partial class ValidFactories
{
    [RedDotNode] public static readonly RedDotKey Home = RedDotKey.Root(""Home"");
    [RedDotFactory(nameof(Home))]
    public static RedDotKey Entry(int heroId, long skinId) => RedDotKey.Dynamic(Home, ""Entry"", heroId, skinId);
}
";
            var result = Run(source);
            Assert.That(result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(result.GeneratedTrees, Has.Length.EqualTo(1));
        }

        [Test]
        public void ReportsInvalidCatalogDeclaration()
        {
            var result = Run(Framework + @"[RedDotCatalog] public partial class NonStaticCatalog { }");
            Assert.That(result.Diagnostics.Select(item => item.Id), Does.Contain("RDT0001"));
        }

        [Test]
        public void CompilesAllDynamicTypesAndMultipleParents()
        {
            var result = Run(Framework + @"
[RedDotCatalog] public static partial class AllTypes
{
    [RedDotNode] public static readonly RedDotKey A = RedDotKey.Root(""A"");
    [RedDotNode] public static readonly RedDotKey B = RedDotKey.Root(""B"");
    [RedDotNode(nameof(A), nameof(B))] public static readonly RedDotKey Shared = RedDotKey.Child(A, ""Shared"");
    [RedDotFactory(nameof(A))] public static RedDotKey Int(int id) => RedDotKey.Dynamic(A, ""Int"", id);
    [RedDotFactory(nameof(A))] public static RedDotKey Long(long id) => RedDotKey.Dynamic(A, ""Long"", id);
    [RedDotFactory(nameof(A))] public static RedDotKey Text(string id) => RedDotKey.Dynamic(A, ""Text"", id);
    [RedDotFactory(nameof(A))] public static RedDotKey GuidKey(Guid id) => RedDotKey.Dynamic(A, ""Guid"", id);
    [RedDotFactory(nameof(A), nameof(B))] public static RedDotKey Composite(Guid id, int stage) => RedDotKey.Dynamic(A, ""Composite"", id, stage);
}");
            Assert.That(result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(result.GeneratedTrees.Single().ToString(), Does.Contain("RedDotEdgeDefinition(B, Shared)"));
        }

        static GeneratorDriverRunResult Run(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var platformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
                ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
            var references = platformAssemblies
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(
                "Tests",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new CatalogGenerator().AsSourceGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
            if (!driver.GetRunResult().Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
            {
                Assert.That(output.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty,
                    "Successful generator output must compile, not just contain expected strings.");
            }
            return driver.GetRunResult();
        }
    }
}
