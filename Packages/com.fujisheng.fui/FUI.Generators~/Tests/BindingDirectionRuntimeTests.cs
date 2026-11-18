extern alias RuntimeCore;
extern alias RuntimeNavigation;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace FUI.Generators.Tests
{
    public sealed class BindingDirectionRuntimeTests
    {
        [TestCase(0)]
        [TestCase(5)]
        public void SourceOnlyBindingNeverWritesTargetDuringBindingSynchronizationOrModelChanges(int initial)
        {
            var result = Execute("OneWayToSource", initial);
            Assert.That(result[0], Is.EqualTo(17), "Initial source value must come from the authored element.");
            Assert.That(result[1], Is.Zero, "Reading a source cannot run the target normalizer/writer.");
            Assert.That(result[2], Is.EqualTo(23), "Physical element changes must reach the model.");
            Assert.That(result[3], Is.EqualTo(23), "Model edits cannot overwrite a source-only target.");
            Assert.That(result[4], Is.Zero);
            Assert.That(result[5], Is.EqualTo(99), "Unbinding must detach reverse listeners.");
        }

        [Test]
        public void TwoWayBindingStillHonorsNondefaultModelAndPropagatesBothDirections()
        {
            var result = Execute("TwoWay", 5);
            Assert.That(result[0], Is.EqualTo(5));
            Assert.That(result[1], Is.GreaterThan(0));
            Assert.That(result[2], Is.EqualTo(23));
            Assert.That(result[3], Is.EqualTo(99));
            Assert.That(result[5], Is.EqualTo(99));
        }

        static int[] Execute(string mode, int initial)
        {
            var source = """
using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Rendering;
[assembly: ViewModule("DirectionProbe.Generated", PropertyMode = PropertyGenerationMode.Pure)]
namespace DirectionProbe
{
    public sealed class ProbeElement : IElement, IExternalValueSource
    {
        bool publishing;
        public int Writes;
        public string Name => "Root";
        public IElement Parent => null;
        public BindableProperty<int> WorldPositionValue { get; }
        public ProbeElement()
        {
            WorldPositionValue = new BindableProperty<int>(17, null, value => { if (!publishing) Writes++; return value; });
            Writes = 0;
        }
        public void Publish(int value) { publishing = true; try { WorldPositionValue.Value = value; } finally { publishing = false; } }
        public void SynchronizeExternalValues() { }
    }
    public sealed class ProbeView : IView
    {
        public readonly ProbeElement Target = new ProbeElement();
        public string Name => "Probe";
        public int Layer { get; set; }
        public int Order { get; set; }
        public bool Visible { get; set; }
        public bool Interactable { get; set; }
        public IReadOnlyList<IElement> Elements => new IElement[] { Target };
        public T GetElement<T>(string path) where T : IElement => (T)GetElement(path, typeof(T));
        public IElement GetElement(string path, Type type) => path == "Root" && type.IsInstanceOfType(Target) ? Target : null;
        public void Initialize() { }
        public void Destroy() { }
    }
    [ViewContract("Probe")]
    public partial class Vm : ObservableObject
    {
        [ObservableProperty]
        [Bind("Root", nameof(ProbeElement.WorldPositionValue), bindingMode: BindingMode.MODE)]
        int position;
    }
    public sealed class ProbeContext : Vm.Vm_Probe_BindingContext
    {
        public ProbeContext(IView view, Vm model) : base(view, model) { }
        public void Start() { base.OnBinding(); base.OnSynchronize(); }
        public void Sync() => base.OnSynchronize();
        public void Stop() => base.OnUnbinding();
    }
    public static class Entry
    {
        public static int[] Run(int initial)
        {
            var view = new ProbeView(); var model = new Vm { Position = initial };
            var context = new ProbeContext(view, model);
            context.Start();
            var first = model.Position; var initialWrites = view.Target.Writes;
            view.Target.Publish(23); var reverse = model.Position;
            model.Position = 99; context.Sync();
            var current = view.Target.WorldPositionValue.Value; var writes = view.Target.Writes;
            context.Stop(); view.Target.Publish(31);
            return new[] { first, initialWrites, reverse, current, writes, model.Position };
        }
    }
}
""".Replace("BindingMode.MODE", "BindingMode." + mode);
            var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
                .Where(path => !Path.GetFileName(path).StartsWith("FUI.", StringComparison.Ordinal));
            var references = platform.Concat(new[] { typeof(RuntimeCore::FUI.Binding.ObservableObject).Assembly.Location, typeof(RuntimeNavigation::FUI.Navigation.Route).Assembly.Location })
                .Distinct().Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create("DirectionProbe" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(source) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingSourceGenerator().AsSourceGenerator());
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
            Assert.That(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            using var stream = new MemoryStream();
            var emitted = output.Emit(stream);
            Assert.That(emitted.Success, Is.True, string.Join("\n", emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
            var assembly = Assembly.Load(stream.ToArray());
            return (int[])assembly.GetType("DirectionProbe.Entry").GetMethod("Run").Invoke(null, new object[] { initial });
        }
    }
}
