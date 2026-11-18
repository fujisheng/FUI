using System;using System.Reflection;using System.Runtime.ExceptionServices;using NUnit.Framework;
namespace FUI.Portable.Tests{public sealed class ControlSettingsContractTests{
const string Probe=@"using System;using FUI.Binding;using FUI.Rendering;using FUI.Testing;
[assembly:ViewModule(""ControlSettingsProbe.Generated"",PropertyMode=PropertyGenerationMode.Pure)]namespace ControlSettingsProbe{
[ViewContract(""Styles"")]public partial class Vm:FUI.Presentation.ViewModel{
[ObservableProperty][Bind(""Text"",nameof(ITextElement.FontSize))]float fontSize=22;
[ObservableProperty][Bind(""Text"",nameof(ITextElement.RichText))]bool richText=true;
[ObservableProperty][Bind(""Text"",nameof(ITextElement.Visible))]bool shown=true;
[ObservableProperty][Bind(""Text"",nameof(ITextElement.Tint))]Rgba tint=new Rgba(1,0,0,1);
[ObservableProperty][Bind(""Slider"",nameof(ISliderElement.MinValue))]float minimum=-1;
[ObservableProperty][Bind(""Slider"",nameof(ISliderElement.MaxValue))]float maximum=2;
[ObservableProperty][Bind(""Slider"",nameof(ISliderElement.WholeNumbers))]bool whole=true;
}
public static class Scenario{public static string[]Run(int which){Generated.Routes.Initialize();var text=new HeadlessTextElement(""Text"");var slider=new HeadlessFloatElement(""Slider"");var toggle=new HeadlessBoolElement(""Toggle"");
if(which==0){ITextElement label=text;IGraphicElement graphic=label;return new[]{ReferenceEquals(label.Tint,graphic.Tint).ToString(),ReferenceEquals(label.Visible,graphic.Visible).ToString(),typeof(ITextElement).GetInterfaces().Contains(typeof(ITextAlignmentElement)).ToString(),typeof(ITextElement).GetInterfaces().Contains(typeof(IFormattedTextElement)).ToString()};}
if(which==1){ISliderElement s=slider;IToggleElement t=toggle;return new[]{ReferenceEquals(s.Value,((IFloatValueElement)slider).Value).ToString(),ReferenceEquals(s.ValueChanged,((IFloatValueElement)slider).ValueChanged).ToString(),ReferenceEquals(t.IsOn,((IBoolValueElement)toggle).Value).ToString(),ReferenceEquals(t.ValueChanged,((IBoolValueElement)toggle).ValueChanged).ToString()};}
if(which==2){var vm=new Vm();var view=new HeadlessView(""Styles"",text,slider);using(var p=Projection.Create(view,vm)){p.Activate();var first=text.FontSize.Value;vm.FontSize=30;vm.Shown=false;vm.RichText=false;vm.Maximum=3;return new[]{first.ToString(),text.FontSize.Value.ToString(),text.Visible.Value.ToString(),text.RichText.Value.ToString(),slider.MinValue.Value.ToString(),slider.MaxValue.Value.ToString(),slider.WholeNumbers.Value.ToString(),(text.Tint.Value.r==1).ToString()};}}
if(which==3){var a=0;var b=0;slider.ValueChanged.AddListener(v=>a++);toggle.ValueChanged.AddListener(v=>b++);slider.Interactable.Value=false;toggle.Visible.Value=false;slider.Input(.8f);toggle.Input(true);var blocked=a+b;slider.Interactable.Value=true;toggle.Visible.Value=true;slider.Input(.8f);toggle.Input(true);slider.Input(.8f);toggle.Input(true);return new[]{blocked.ToString(),a.ToString(),b.ToString()};}
var changed=0;text.FontSize.OnValueChanged+=(oldValue,newValue)=>changed++;text.Tint.OnValueChanged+=(oldValue,newValue)=>changed++;text.Dispose();text.FontSize.Value=17;text.Tint.Value=new Rgba(0,1,0,1);return new[]{changed.ToString(),text.IsDisposed.ToString()};}}
}";
[TestCase(0,new[]{"True","True","True","True"})][TestCase(1,new[]{"True","True","True","True"})][TestCase(2,new[]{"22","30","False","False","-1","3","True","True"})][TestCase(3,new[]{"0","1","1"})][TestCase(4,new[]{"0","True"})]
public void ControlSurfaceIncludesActualTextStyleAndSharedValueState(int which,string[]expected){var source="using System.Linq;"+Probe;var assembly=GeneratedCompilation.Compile(("ControlSettingsProbe.cs",source));try{Assert.That((string[])assembly.GetType("ControlSettingsProbe.Scenario").GetMethod("Run").Invoke(null,new object[]{which}),Is.EqualTo(expected));}catch(TargetInvocationException error)when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}}
}}
