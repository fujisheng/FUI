using FUI.Analyzer.AttributeBinding;
using FUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FUI.Generators.Tests
{
    public sealed class GeneratorCoverageTests
    {
        [Test]
        public void MissingViewModuleUsesAssemblyScopedDefaultNamespace()
        {
            const string source = @"
using System;
namespace FUI.Binding
{
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}
[FUI.Binding.ViewContract(""SettingsView"")]
public partial class SettingsViewModel : FUI.Binding.INotifyPropertyChanged { }
";

            var result = RunGenerator(source, "Feature.Account");
            var routes = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(routes, Does.Contain("namespace Feature.Account.UI"));
            Assert.That(routes, Does.Not.Contain("GameUI"));
        }

        [Test]
        public void StaticRouteDependencyCycleReportsDiagnostic()
        {
            const string source = @"
using System;
[assembly: FUI.Binding.ViewModule(""Tests.UI"")]
namespace FUI.Binding
{
    public sealed class ViewModuleAttribute : Attribute { public ViewModuleAttribute(string name) { } }
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}
namespace FUI.Navigation
{
    public enum Layer { Panel = 0 }
    public sealed class RoutePolicyAttribute : Attribute
    {
        public Type[] RequiredBefore { get; set; }
        public RoutePolicyAttribute(Layer layer = Layer.Panel) { }
    }
}
[FUI.Binding.ViewContract(""FirstView"")]
[FUI.Navigation.RoutePolicy(RequiredBefore = new[] { typeof(SecondViewModel) })]
public partial class FirstViewModel : FUI.Binding.INotifyPropertyChanged { }
[FUI.Binding.ViewContract(""SecondView"")]
[FUI.Navigation.RoutePolicy(RequiredBefore = new[] { typeof(FirstViewModel) })]
public partial class SecondViewModel : FUI.Binding.INotifyPropertyChanged { }
";

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(diagnostic => diagnostic.Id == "FUI0012"));
            Assert.That(result.GeneratedTrees.Select(tree => tree.FilePath), Has.None.Contains("Routes.g.cs"));
        }

        [Test]
        public void BusinessAsyncMethodDoesNotChangeRouteContract()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
[assembly: FUI.Binding.ViewModule(""Tests.UI"")]
namespace FUI.Binding
{
    public sealed class ViewModuleAttribute : Attribute { public ViewModuleAttribute(string name) { } }
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public sealed class PresenterForAttribute : Attribute { public PresenterForAttribute(Type type) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}
namespace FUI.Presentation
{
    public abstract class PresenterBase
    {
        protected virtual ValueTask LoadBusinessDataAsync(object parameter) => default;
    }
}
[FUI.Binding.ViewContract(""AsyncView"")]
public partial class AsyncViewModel : FUI.Binding.INotifyPropertyChanged { }
[FUI.Binding.PresenterFor(typeof(AsyncViewModel))]
public sealed class AsyncPresenter : FUI.Presentation.PresenterBase
{
    protected override ValueTask LoadBusinessDataAsync(object parameter) => default;
}
";

            var result = RunGenerator(source);
            var routes = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(routes, Does.Contain("typeof(global::AsyncPresenter)"));
            Assert.That(routes, Does.Not.Contain("supportsSynchronousOpen"));
        }

        [Test]
        public void BusinessAsyncDependencyDoesNotChangeParentRouteContract()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
[assembly: FUI.Binding.ViewModule(""Tests.UI"")]
namespace FUI.Binding
{
    public sealed class ViewModuleAttribute : Attribute { public ViewModuleAttribute(string name) { } }
    public sealed class ViewContractAttribute : Attribute { public ViewContractAttribute(string name) { } }
    public sealed class PresenterForAttribute : Attribute { public PresenterForAttribute(Type type) { } }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}
namespace FUI.Navigation
{
    public sealed class RoutePolicyAttribute : Attribute
    {
        public Type[] RequiredBefore { get; set; }
    }
}
namespace FUI.Presentation
{
    public abstract class PresenterBase
    {
        protected virtual ValueTask LoadBusinessDataAsync(object parameter) => default;
    }
}
[FUI.Binding.ViewContract(""ChildView"")]
public partial class ChildViewModel : FUI.Binding.INotifyPropertyChanged { }
[FUI.Binding.PresenterFor(typeof(ChildViewModel))]
public sealed class ChildPresenter : FUI.Presentation.PresenterBase
{
    protected override ValueTask LoadBusinessDataAsync(object parameter) => default;
}
[FUI.Binding.ViewContract(""ParentView"")]
[FUI.Navigation.RoutePolicy(RequiredBefore = new[] { typeof(ChildViewModel) })]
public partial class ParentViewModel : FUI.Binding.INotifyPropertyChanged { }
";

            var result = RunGenerator(source);
            var routes = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(routes, Does.Not.Contain("supportsSynchronousOpen"));
            Assert.That(routes, Does.Contain("DependencyTiming.RequiredBefore"));
        }

        [Test]
        public void RoutePolicySerializesAllOrthogonalOptions()
        {
            const string source = @"
using System;
[assembly: FUI.Binding.ViewModule(""Tests.UI"")]
namespace FUI.Binding
{
    public sealed class ViewModuleAttribute : Attribute { public ViewModuleAttribute(string name) { } }
    public sealed class ViewContractAttribute : Attribute
    {
        public ViewContractAttribute(string name, string assetKey = null) { }
    }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public interface IBindableProperty<T> { }
}
namespace FUI.Navigation
{
    public enum Layer { Panel = 0, Popup = 8000 }
    public enum HistoryMode { Stack = 0, Transient = 1 }
    public enum CoverageMode { KeepVisible = 0, Hide = 1 }
    public enum CacheMode { None = 0, Timed = 2 }
    public sealed class RoutePolicyAttribute : Attribute
    {
        public HistoryMode HistoryMode { get; set; }
        public CoverageMode CoverageMode { get; set; }
        public CacheMode CacheMode { get; set; }
        public bool AllowMultiple { get; set; }
        public int CacheCapacity { get; set; }
        public float CacheDuration { get; set; }
        public Type EnterTransitionProvider { get; set; }
        public Type ExitTransitionProvider { get; set; }
        public RoutePolicyAttribute(Layer layer = Layer.Panel) { }
    }
}
public sealed class EnterTransition { }
public sealed class ExitTransition { }
[FUI.Binding.ViewContract(""PopupView"", ""UI/Popup.prefab"")]
[FUI.Navigation.RoutePolicy(
    FUI.Navigation.Layer.Popup,
    HistoryMode = FUI.Navigation.HistoryMode.Transient,
    CoverageMode = FUI.Navigation.CoverageMode.Hide,
    CacheMode = FUI.Navigation.CacheMode.Timed,
    AllowMultiple = true,
    CacheCapacity = 4,
    CacheDuration = 2.5f,
    EnterTransitionProvider = typeof(EnterTransition),
    ExitTransitionProvider = typeof(ExitTransition))]
public partial class PopupViewModel : FUI.Binding.INotifyPropertyChanged { }
";

            var result = RunGenerator(source);
            var routes = result.GeneratedTrees.Single(tree => tree.FilePath.Contains("Routes.g.cs")).ToString();

            Assert.That(routes, Does.Contain("@\"UI/Popup.prefab\""));
            Assert.That(routes, Does.Contain("layer: FUI.Navigation.Layer.Popup"));
            Assert.That(routes, Does.Contain("historyMode: (FUI.Navigation.HistoryMode)1"));
            Assert.That(routes, Does.Contain("coverageMode: (FUI.Navigation.CoverageMode)1"));
            Assert.That(routes, Does.Contain("cacheMode: (FUI.Navigation.CacheMode)2"));
            Assert.That(routes, Does.Contain("allowMultiple: true"));
            Assert.That(routes, Does.Contain("cacheCapacity: 4"));
            Assert.That(routes, Does.Contain("cacheDuration: 2.5f"));
            Assert.That(routes, Does.Contain("new global::EnterTransition()"));
            Assert.That(routes, Does.Contain("new global::ExitTransition()"));
        }

        [Test]
        public void UtilityRecognizesBindingSyntaxAndBuildsStableNames()
        {
            const string source = @"
[FUI.Binding.ViewContract(""StatusView"")]
public partial class StatusViewModel
{
    [FUI.Binding.Bind(""Title"", nameof(Element.Text))]
    public string Title { get; set; }

    [FUI.Binding.ObservableProperty]
    string count;

    [FUI.Binding.Command(""Save"", nameof(Element.Click))]
    public void Save() { }
}
";
            var root = CSharpSyntaxTree.ParseText(source, path: "StatusViewModel.cs").GetRoot();
            var declaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            var property = declaration.Members.OfType<PropertyDeclarationSyntax>().Single();
            var method = declaration.Members.OfType<MethodDeclarationSyntax>().Single();

            Assert.That(declaration.IsObservableObject(), Is.True);
            Assert.That(Utility.TryGetViewBindingAttribute(declaration.AttributeLists, out var viewAttribute), Is.True);
            Assert.That(viewAttribute.Name.ToString(), Does.Contain("ViewContract"));
            Assert.That(Utility.TryGetPropertyBindingAttributes(property.AttributeLists, out var bindings), Is.True);
            Assert.That(bindings, Has.Count.EqualTo(1));
            Assert.That(Utility.TryGetCommandAttributes(method.AttributeLists, out var commands), Is.True);
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That("Global.Type<T>/Nested".ToCSharpIdentifier(), Is.EqualTo("Global_Type_T__Nested"));
            Assert.That("Tests.UI.StatusViewModel".GetTypeName("Tests.UI"), Is.EqualTo("StatusViewModel"));
            Assert.That(Utility.GetPropertyChangedDelegateName("Title"), Is.EqualTo("_Title_Changed"));
            Assert.That(Utility.GetPropertyBackingFieldName("Title"), Is.EqualTo("_Title__BackingField"));
            Assert.That(Utility.GetEventMethodName("Save"), Is.EqualTo("__EventMethod_Save"));
            Assert.That(Utility.GenerateCodeWithHeader("class C { }"), Does.EndWith("class C { }"));
        }

        [Test]
        public async Task AnalyzerExposesStableDiagnosticCatalogWithoutReportingDisabledRules()
        {
            const string source = "public sealed class PlainType { }";
            var compilation = CreateCompilation(new[] { source }, "AnalyzerTest");
            var analyzer = new AttributeBindingAnalyzer();
            var diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
                .GetAnalyzerDiagnosticsAsync();

            Assert.That(analyzer.SupportedDiagnostics.Select(item => item.Id), Is.EqualTo(new[]
            {
                "FUI0001", "FUI0002", "FUI0003", "FUI0004", "FUI0005",
                "FUI0006", "FUI0007", "FUI0010", "FUI0008", "FUI0009",
            }));
            Assert.That(diagnostics, Is.Empty);
        }

        static GeneratorDriverRunResult RunGenerator(string source, string assemblyName = "GeneratorCoverageTest")
        {
            var compilation = CreateCompilation(new[] { source }, assemblyName);
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingSourceGenerator().AsSourceGenerator());
            return driver.RunGenerators(compilation).GetRunResult();
        }

        static CSharpCompilation CreateCompilation(string[] sources, string assemblyName)
        {
            var syntaxTrees = sources
                .Select((source, index) => CSharpSyntaxTree.ParseText(source, path: $"Coverage{index}.cs"))
                .ToArray();
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
