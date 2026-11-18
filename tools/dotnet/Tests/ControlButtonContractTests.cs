using System;using System.IO;using System.Linq;using System.Reflection;using System.Runtime.ExceptionServices;using FUI.Binding;using Microsoft.CodeAnalysis;using Microsoft.CodeAnalysis.CSharp;using NUnit.Framework;
namespace FUI.Portable.Tests {public sealed class ControlButtonContractTests {
const string Probe=@"using System;using FUI.Binding;using FUI.Rendering;using FUI.Testing;
[assembly:ViewModule(""ControlButtonProbe.Generated"",PropertyMode=PropertyGenerationMode.Pure)]
namespace ControlButtonProbe {
public sealed class LegacyClickOnly:IClickElement{public string Name=>""Button"";public IElement Parent=>null;public Command Click{get;}=new Command();}
[ViewContract(""Button"")]public partial class ButtonVm:FUI.Presentation.ViewModel {
 [ObservableProperty][Bind(""Button"",nameof(IButtonElement.Interactable))]bool available=true;
 [ObservableProperty][Bind(""Button"",nameof(IButtonElement.Visible))]bool shown=true;
 public int Calls;
 [Command(""Button"",nameof(IButtonElement.Click))]void Activate(){Calls++;}
}
public static class Scenario {public static string[] Run(int which){Generated.Routes.Initialize();if(which==8){var failed=false;var invalid=new HeadlessView(""Button"",new LegacyClickOnly());using(var bad=Projection.Create(invalid,new ButtonVm())){try{bad.Activate();}catch(InvalidOperationException){failed=true;}return new[]{failed.ToString()};}}var vm=new ButtonVm();var button=new HeadlessButtonElement(""Button"");var view=new HeadlessView(""Button"",button);using(var projection=Projection.Create(view,vm)){projection.Activate();
 if(which==0){IButtonElement control=button;IClickElement legacy=button;ISelectableElement selectable=button;return new[]{ReferenceEquals(control.Click,legacy.Click).ToString(),ReferenceEquals(control.Interactable,selectable.Interactable).ToString(),ReferenceEquals(control.Visible,((IVisibilityElement)button).Visible).ToString()};}
 if(which==1){button.Input();return new[]{vm.Calls.ToString()};}
 if(which==2){vm.Available=false;button.Input();view.Interactable=false;view.Interactable=true;button.Input();var disabled=vm.Calls;vm.Available=true;button.Input();return new[]{disabled.ToString(),vm.Calls.ToString()};}
 if(which==3){vm.Shown=false;button.Input();vm.Shown=true;button.Input();return new[]{vm.Calls.ToString(),button.Visible.Value.ToString()};}
 if(which==4){projection.Deactivate();button.Input();button.Click.Invoke();var closed=vm.Calls;projection.Activate();button.Input();return new[]{closed.ToString(),vm.Calls.ToString()};}
 if(which==5){projection.Dispose();button.Input();button.Click.Invoke();return new[]{vm.Calls.ToString()};}
 if(which==6){var other=new HeadlessButtonElement(""Button"");var secondView=new HeadlessView(""Button"",other);using(var second=Projection.Create(secondView,vm)){second.Activate();button.Input();other.Input();projection.Deactivate();button.Input();other.Input();return new[]{vm.Calls.ToString()};}}
 view.Interactable=false;button.Input();var covered=vm.Calls;view.Interactable=true;button.Input();return new[]{covered.ToString(),vm.Calls.ToString()};
}}}}
";
[TestCase(8,new[]{"True"})][TestCase(0,new[]{"True","True","True"})][TestCase(1,new[]{"1"})][TestCase(2,new[]{"0","1"})][TestCase(3,new[]{"1","True"})][TestCase(4,new[]{"0","1"})][TestCase(5,new[]{"0"})][TestCase(6,new[]{"3"})][TestCase(7,new[]{"0","1"})]
public void ActualGeneratedButtonExposesInheritedStateAndOneSharedClick(int which,string[]expected){var assembly=GeneratedCompilation.Compile(("ControlButtonProbe.cs",Probe));try{Assert.That((string[])assembly.GetType("ControlButtonProbe.Scenario").GetMethod("Run").Invoke(null,new object[]{which}),Is.EqualTo(expected));}catch(TargetInvocationException error)when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}}
[Test]public void ActualNativeButtonDeclarationImplementsControlWithoutExtraEventChain(){var root=GeneratedCompilation.RepositoryRoot();var original=File.ReadAllText(Path.Combine(root,"Packages/com.fujisheng.fui.ugui/Runtime/Rendering/UGUI/Elements/ButtonElement.cs"));var stub=@"
namespace UnityEngine{public sealed class DisallowMultipleComponent:System.Attribute{}public sealed class RequireComponent:System.Attribute{public RequireComponent(System.Type type){}}public sealed class AddComponentMenu:System.Attribute{public AddComponentMenu(string path){}}}
namespace UnityEngine.UI{public class ClickEvent{public void AddListener(System.Action action){}public void RemoveListener(System.Action action){}}public class Button{public ClickEvent onClick;}}
namespace FUI.Rendering.UGUI{public abstract class SelectableElement<T>:FUI.Rendering.ISelectableElement{protected T Component;public string Name=>throw new System.NotSupportedException();public FUI.Rendering.IElement Parent=>throw new System.NotSupportedException();public FUI.Binding.BindableProperty<bool>Visible=>throw new System.NotSupportedException();public FUI.Binding.BindableProperty<bool>Interactable=>throw new System.NotSupportedException();protected virtual void OnInitialize(){}protected U Track<U>(U value)=>value;protected void TrackCleanup(System.Action cleanup){}}}
public static class DeclarationOnly{public static FUI.Rendering.IButtonElement AsControl(FUI.Rendering.UGUI.Elements.ButtonElement button)=>button;}
";var platform=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(x=>!Path.GetFileName(x).StartsWith("FUI."));var refs=platform.Concat(new[]{typeof(ObservableObject).Assembly.Location}).Distinct().Select(x=>MetadataReference.CreateFromFile(x));var compilation=CSharpCompilation.Create("NativeButtonDeclarationOnly",new[]{CSharpSyntaxTree.ParseText(original),CSharpSyntaxTree.ParseText(stub)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));Assert.That(compilation.GetDiagnostics().Where(x=>x.Severity==DiagnosticSeverity.Error),Is.Empty,"仅实际Button源声明检查，不Emit/不执行Unity");Assert.That(original,Does.Contain("Command IClickElement.Click => OnClick;"));Assert.That(original.Split(new[]{"Component.onClick.AddListener("},StringSplitOptions.None).Length-1,Is.EqualTo(1));}
}}
