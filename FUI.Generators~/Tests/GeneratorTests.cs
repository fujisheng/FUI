using FUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace FUI.Generators.Tests
{
    public class GeneratorTests
    {
        const string Source = @"
using System;

[assembly: FUI.Binding.ViewModule(""Tests.UI"", PropertyMode = FUI.Binding.PropertyGenerationMode.Mixed)]

namespace FUI.Binding
{
    public enum PropertyGenerationMode { Pure, Mixed }
    public sealed class ViewModuleAttribute : Attribute
    {
        public PropertyGenerationMode PropertyMode { get; set; }
        public ViewModuleAttribute(string moduleNamespace) { }
    }
    public sealed class ViewContractAttribute : Attribute
    {
        public ViewContractAttribute(string viewName) { }
    }

    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}

[FUI.Binding.ViewContract(""SampleView"")]
public partial class SampleViewModel : FUI.Binding.INotifyPropertyChanged
{
}
";

        [Test]
        public void SingleEntryPointGeneratesThreeCoreArtifacts()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(Source, path: "SampleViewModel.cs");
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(
                "GeneratorTest",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingSourceGenerator().AsSourceGenerator());
            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath), Has.Some.Contains("PropertyChanged.g.cs"));
            Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath), Has.Some.Contains("BindingContext.g"));
            Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath), Has.Some.Contains("BindingInfo.g"));
            var manifest = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingInfo.g")).ToString();
            Assert.That(manifest, Does.Contain("namespace Tests.UI.Infrastructure.BindingManifests"));
            Assert.That(manifest, Does.Not.Contain("FUI.Generated"));
            Assert.That(string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString())), Does.Not.Contain("GameUI"));
        }

        [Test]
        public void AssemblyExposesSingleGeneratorEntryPoint()
        {
            var generators = typeof(BindingSourceGenerator).Assembly.GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(GeneratorAttribute), false).Length > 0)
                .ToArray();

            Assert.That(generators, Is.EqualTo(new[] { typeof(BindingSourceGenerator) }));
            Assert.That(typeof(IIncrementalGenerator).IsAssignableFrom(generators[0]), Is.True);
        }

        [Test]
        public void GeneratesIndependentRoutesForBaseAndDerivedViewModels()
        {
            const string source = Source + @"

[FUI.Binding.ViewContract(""StatusView"")]
public partial class StatusViewModel : FUI.Binding.INotifyPropertyChanged
{
}

public partial class HeroStatusViewModel : StatusViewModel
{
}
";
            var result = RunGenerator(source);
            var registry = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();
            var bindings = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingRegistry.g.cs")).ToString();

            Assert.That(registry, Does.Contain("Route<global::StatusViewModel> StatusView"));
            Assert.That(registry, Does.Contain("Route<global::HeroStatusViewModel> HeroStatusView"));
            Assert.That(registry, Does.Contain("@\"StatusView\""));
            Assert.That(registry, Does.Not.Contain("RegisterGeneratedFactory"));
            Assert.That(bindings, Does.Contain("Register<global::StatusViewModel>"));
            Assert.That(bindings, Does.Contain("Register<global::HeroStatusViewModel>"));
            Assert.That(bindings, Does.Not.Contain("@\"StatusView\""));
            Assert.That(registry, Does.Contain("namespace Tests.UI"));
            Assert.That(registry, Does.Contain("public static void Initialize()"));
        }

        [Test]
        public void ObservableTypeWithoutViewContractStillGeneratesBindingFactory()
        {
            const string source = @"
using System;
[assembly: FUI.Binding.ViewModule(""Tests.Items"")]
namespace FUI.Binding
{
    public sealed class ViewModuleAttribute : Attribute { public ViewModuleAttribute(string name) { } }
    public interface INotifyPropertyChanged { }
}
public partial class StatusItem : FUI.Binding.INotifyPropertyChanged { }
";
            var result = RunGenerator(source);
            var bindings = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingRegistry.g.cs")).ToString();
            var routes = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(bindings, Does.Contain("Register<global::StatusItem>"));
            Assert.That(bindings, Does.Contain("StatusItem.StatusItem__BindingContext"));
            Assert.That(routes, Does.Contain("public static void Initialize()"));
            Assert.That(routes, Does.Not.Contain("FUI.Navigation"));
        }

        [Test]
        public void IncludesBindingsFromAllPartialDeclarations()
        {
            const string definitions = @"
using System;
[assembly: FUI.Binding.ViewModule(""Tests.UI"", PropertyMode = FUI.Binding.PropertyGenerationMode.Mixed)]
namespace FUI.Binding
{
    public enum PropertyGenerationMode { Pure, Mixed }
    public sealed class ViewModuleAttribute : Attribute
    {
        public PropertyGenerationMode PropertyMode { get; set; }
        public ViewModuleAttribute(string moduleNamespace) { }
    }
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public sealed class BindAttribute : Attribute { public BindAttribute(string path, string property) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
    public sealed class BindableProperty<T> : IBindableProperty<T> { }
}
public sealed class TestElement { public FUI.Binding.BindableProperty<string> Text { get; } }
[FUI.Binding.ViewContract(""SplitView"")]
public partial class SplitViewModel : FUI.Binding.INotifyPropertyChanged { }
";
            const string members = @"
public partial class SplitViewModel
{
    [FUI.Binding.Bind(""Title"", nameof(TestElement.Text))]
    public string Title { get; set; }
}
";
            var result = RunGenerator(definitions, members);
            var propertyCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("SplitViewModel.PropertyChanged.g.cs")).ToString();
            var bindingCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingContext.g")).ToString();

            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(propertyCode, Does.Contain("__Set_Title"));
            Assert.That(bindingCode, Does.Contain("this.vm.Title"));
        }

        [Test]
        public void DerivedObservableTypeOverridesBaseSynchronizeMethod()
        {
            const string source = Source + @"

public abstract partial class BaseItem : FUI.Binding.INotifyPropertyChanged { }
public partial class DerivedItem : BaseItem { }
";
            var result = RunGenerator(source);
            var baseCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BaseItem.PropertyChanged.g.cs")).ToString();
            var derivedCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("DerivedItem.PropertyChanged.g.cs")).ToString();

            Assert.That(baseCode, Does.Contain("public virtual void Synchronize()"));
            Assert.That(derivedCode, Does.Contain("public override void Synchronize()"));
            Assert.That(derivedCode, Does.Contain("base.Synchronize()"));
        }

        [Test]
        public void DoesNotGenerateEmptyRouteCatalogWithoutPageDeclarations()
        {
            var result = RunGenerator("public sealed class PlainType { }");

            Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath), Has.None.Contains("Routes.g.cs"));
        }

        [Test]
        public void DifferentPresentersForSameViewModelGenerateIndependentRouteVariants()
        {
            const string source = Source + @"

namespace FUI.Binding
{
    public sealed class PresenterForAttribute : Attribute
    {
        public PresenterForAttribute(Type viewModelType) { }
    }
}

[FUI.Binding.PresenterFor(typeof(SampleViewModel))]
public sealed class AlphaPresenter { }

[FUI.Binding.PresenterFor(typeof(SampleViewModel))]
public sealed class BetaPresenter { }
";
            var result = RunGenerator(source);
            var registry = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(registry, Does.Contain("SampleView { get; }"));
            Assert.That(registry, Does.Contain("SampleViewBeta { get; }"));
            Assert.That(registry, Does.Contain("typeof(global::AlphaPresenter)"));
            Assert.That(registry, Does.Contain("typeof(global::BetaPresenter)"));
        }

        [Test]
        public void RoutePolicyDependenciesSupportViewModelTypes()
        {
            const string source = Source + @"

namespace FUI.Navigation
{
    public enum Layer { Panel = 0 }
    public sealed class RoutePolicyAttribute : Attribute
    {
        public Type[] RequiredBefore { get; set; }
        public Type[] AttachedAfter { get; set; }
        public RoutePolicyAttribute(Layer layer = Layer.Panel) { }
    }
}

[FUI.Binding.ViewContract(""StatusView"")]
public partial class StatusViewModel : FUI.Binding.INotifyPropertyChanged { }

[FUI.Binding.ViewContract(""ShopView"")]
[FUI.Navigation.RoutePolicy(AttachedAfter = new[] { typeof(StatusViewModel) })]
public partial class ShopViewModel : FUI.Binding.INotifyPropertyChanged { }
";
            var result = RunGenerator(source);
            var registry = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(registry, Does.Contain("DependencyTiming.AttachedAfter"));
            Assert.That(registry, Does.Contain("static () => StatusView"));
        }

        [Test]
        public void SharedViewContractPreservesExactDependencyViewModelType()
        {
            const string source = Source + @"

namespace FUI.Navigation
{
    public enum Layer { Panel = 0 }
    public sealed class RoutePolicyAttribute : Attribute
    {
        public Type[] RequiredBefore { get; set; }
        public RoutePolicyAttribute(Layer layer = Layer.Panel) { }
    }
}

[FUI.Binding.ViewContract(""SharedView"")]
public partial class SharedViewModel : FUI.Binding.INotifyPropertyChanged { }
public partial class DerivedSharedViewModel : SharedViewModel { }

[FUI.Binding.ViewContract(""ConsumerView"")]
[FUI.Navigation.RoutePolicy(RequiredBefore = new[] { typeof(DerivedSharedViewModel) })]
public partial class ConsumerViewModel : FUI.Binding.INotifyPropertyChanged { }
";
            var result = RunGenerator(source);
            var registry = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(registry, Does.Contain("DependencyTiming.RequiredBefore, static () => DerivedSharedView"));
        }

        [Test]
        public void BindingCachesElementOnceAndManagesAsyncCommands()
        {
            const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: FUI.Binding.ViewModule(""Tests.UI"", PropertyMode = FUI.Binding.PropertyGenerationMode.Mixed)]

namespace FUI.Binding
{
    public enum PropertyGenerationMode { Pure, Mixed }
    public sealed class ViewModuleAttribute : Attribute
    {
        public PropertyGenerationMode PropertyMode { get; set; }
        public ViewModuleAttribute(string moduleNamespace) { }
    }
    public enum BindingMode { OneWay = 1, OneWayToSource = 2, TwoWay = 3 }
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public sealed class BindAttribute : Attribute
    {
        public BindAttribute(string path, string property, Type converter = null, BindingMode bindingMode = BindingMode.OneWay) { }
    }
    public sealed class CommandAttribute : Attribute { public CommandAttribute(string path, string command) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
    public sealed class BindableProperty<T> : IBindableProperty<T> { }
    public sealed class Command { }
}

public sealed class TestElement
{
    public FUI.Binding.BindableProperty<int> Position { get; }
    public FUI.Binding.Command OnClick { get; }
    public FUI.Binding.Command OnSubmit { get; }
}

[FUI.Binding.ViewContract(""AsyncView"")]
public partial class AsyncViewModel : FUI.Binding.INotifyPropertyChanged
{
    [FUI.Binding.Bind(""Root"", nameof(TestElement.Position), bindingMode: FUI.Binding.BindingMode.TwoWay)]
    public int Position { get; set; }

    [FUI.Binding.Command(""Root"", nameof(TestElement.OnClick))]
    public ValueTask SaveAsync(CancellationToken token) => default;

    [FUI.Binding.Command(""Root"", nameof(TestElement.OnSubmit))]
    public Task SubmitAsync() => Task.CompletedTask;
}
";
            var result = RunGenerator(source);
            var generated = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingContext.g")).ToString();

            Assert.That(generated.Split("GetElement<TestElement>").Length - 1, Is.EqualTo(1));
            Assert.That(generated, Does.Contain("TryGetElement<TestElement>"));
            Assert.That(generated, Does.Contain("TestElement element__Root__TestElement"));
            Assert.That(generated, Does.Contain("protected override void OnSynchronize()"));
            Assert.That(generated, Does.Contain("PropertyChanged__Position__Root_TestElement_Position(this.vm, this.vm.Position, this.vm.Position)"));
            Assert.That(generated, Does.Contain("RunAsyncCommand(token => this.vm.SaveAsync(token))"));
            Assert.That(generated, Does.Contain("RunAsyncCommand(token => new System.Threading.Tasks.ValueTask(this.vm.SubmitAsync()))"));
            Assert.That(generated, Does.Contain("SetExternalValueSources"));
        }

        [Test]
        public void PureModeGeneratesBindablePropertyFromField()
        {
            const string source = @"
using System;

namespace FUI.Binding
{
    public enum BindingMode { OneWay = 1 }
    public sealed class ObservablePropertyAttribute : Attribute { }
    public sealed class BindAttribute : Attribute
    {
        public BindAttribute(string path, string property, Type converter = null, BindingMode bindingMode = BindingMode.OneWay) { }
    }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
    public sealed class BindableProperty<T> : IBindableProperty<T> { }
}

public sealed class TestElement
{
    public FUI.Binding.BindableProperty<string> Text { get; }
}

public partial class FieldViewModel : FUI.Binding.INotifyPropertyChanged
{
    [FUI.Binding.ObservableProperty]
    [FUI.Binding.Bind(""Root"", nameof(TestElement.Text))]
    string title;
}
";
            var result = RunGenerator(source);
            var propertyCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("FieldViewModel.PropertyChanged.g.cs")).ToString();
            var bindingCode = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("BindingContext.g")).ToString();

            Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(propertyCode, Does.Contain("public string Title"));
            Assert.That(propertyCode, Does.Contain("this.title = value"));
            Assert.That(bindingCode, Does.Contain("this.vm.Title"));
        }

        [Test]
        public void PureModeRejectsBoundAutoProperty()
        {
            const string source = @"
using System;
namespace FUI.Binding
{
    public sealed class BindAttribute : Attribute { public BindAttribute(string path, string property) { } }
    public interface INotifyPropertyChanged { }
}
public partial class InvalidViewModel : FUI.Binding.INotifyPropertyChanged
{
    [FUI.Binding.Bind(""Root"", ""Value"")]
    public int Value { get; set; }
}
";
            var result = RunGenerator(source);

            Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(diagnostic => diagnostic.Id == "FUI0013"));
        }

        static GeneratorDriverRunResult RunGenerator(params string[] sources)
        {
            var syntaxTrees = sources
                .Select((source, index) => CSharpSyntaxTree.ParseText(source, path: $"RouteTest{index}.cs"))
                .ToArray();
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(
                "RouteGeneratorTest",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingSourceGenerator().AsSourceGenerator());
            return driver.RunGenerators(compilation).GetRunResult();
        }
    }
}
