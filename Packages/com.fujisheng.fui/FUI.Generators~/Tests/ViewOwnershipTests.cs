extern alias RuntimeCore;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace FUI.Generators.Tests
{
    public sealed class ViewOwnershipTests
    {
        [TestCase("IView retainedView;")]
        [TestCase("ProjectionTemplate retainedTemplate;")]
        [TestCase("System.Collections.Generic.List<ProjectionHandle<GameModel>> rows;")]
        [TestCase("void Make(ProjectionTemplate template) => template.Create(this).Activate();")]
        [TestCase("IElement GetControl(IView view) => view.GetElement<IElement>(\"Title\");")]
        [TestCase("System.Action<IView> callback;")]
        [TestCase("string ReadName() => (new Renderer().Create() as IView).Name;")]
        public async Task RejectsViewOwnershipInAViewModel(string member)
        {
            var diagnostics = await Analyze(
                "public sealed class GameModel : ViewModel { " + member + " }");
            Assert.That(diagnostics.Select(value => value.Id), Does.Contain("FUI0301"));
        }

        [Test]
        public async Task RejectsViewOwnershipInAPresenter()
        {
            var diagnostics = await Analyze(
                "public sealed class GameModel : ViewModel { } " +
                "public sealed class GamePresenter : Presenter<GameModel> { IView retainedView; }");
            Assert.That(diagnostics.Select(value => value.Id), Does.Contain("FUI0301"));
        }

        [Test]
        public async Task RejectsViewOwnershipInABoundListItem()
        {
            var diagnostics = await Analyze(
                "public sealed class Row : ObservableObject { " +
                "[Bind(\"Title\", nameof(ITextElement.Text))] public string Title { get; set; } " +
                "IView retainedView; }");
            Assert.That(diagnostics.Select(value => value.Id), Does.Contain("FUI0301"));
        }

        [Test]
        public async Task AllowsBindingMetadataAndRendererImplementations()
        {
            var diagnostics = await Analyze(
                "public sealed class GameModel : ViewModel { " +
                "[Bind(\"Title\", nameof(ITextElement.Text))] public string Title { get; set; } } " +
                "public sealed class RendererAdapter { public string Name(IView view) => view.Name; }");
            Assert.That(diagnostics.Select(value => value.Id), Does.Not.Contain("FUI0301"));
        }

        static async Task<ImmutableArray<Diagnostic>> Analyze(string declaration)
        {
            var code = "using FUI.Binding; using FUI.Presentation; using FUI.Rendering; " +
                "public sealed class Renderer { public object Create() => null; } " + declaration;
            var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path => !Path.GetFileName(path).StartsWith("FUI.", StringComparison.Ordinal));
            var references = platform.Append(typeof(RuntimeCore::FUI.Presentation.ViewModel).Assembly.Location)
                .Distinct()
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create("GameOwnership",
                new[] { CSharpSyntaxTree.ParseText(code) },
                references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.That(compilation.GetDiagnostics().Where(value => value.Severity == DiagnosticSeverity.Error), Is.Empty);

            var analyzers = typeof(BindingSourceGenerator).Assembly.GetTypes()
                .Where(type => type.IsDefined(typeof(DiagnosticAnalyzerAttribute), false))
                .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
                .ToImmutableArray();
            return await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        }
    }
}
