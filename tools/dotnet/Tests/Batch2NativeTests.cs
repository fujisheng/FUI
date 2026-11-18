using System;using System.IO;using System.Linq;using System.Reflection;using System.Runtime.ExceptionServices;using NUnit.Framework;
namespace FUI.Portable.Tests{public sealed class Batch2NativeTests{
[TestCase(0,new[]{"True","True","True","True","True","True"},TestName="Batch2ActualNativeInterfaceGettersReuseOriginalObjects")]
[TestCase(1,new[]{"0","9","2",""},TestName="Batch2ActualLegacyDropdownProgramWritesAndUserEventsStayDistinct")]
[TestCase(2,new[]{"0","True","10","10","1","False"},TestName="Batch2ActualTmpDropdownForwardsMaskWithoutChangingLegacyCapability")]
[TestCase(3,new[]{"0","","","edit:|end:finish|submit:send"},TestName="Batch2ActualInputBridgeRetainsNoNotifyAndThreeEventChannels")]
[TestCase(4,new[]{"0","Custom","MultiLineNewline","initial"},TestName="Batch2ActualInputConfigurationSuppressesBackendChangeCommands")]
[TestCase(5,new[]{"0","0","0","0","0","0","0","0","0"},TestName="Batch2ActualNativeCleanupUnsubscribesEveryBackendEvent")]
[TestCase(6,new[]{"0","0.6","0.3","4","2"},TestName="Batch2ActualScrollbarKeepsNoNotifyAndRawBackendEvents")]
[TestCase(7,new[]{"True","True","True"},TestName="Batch2ActualTmpControlContractSharesInheritedState")]
[TestCase(8,new[]{"True","True","True","True"},TestName="Batch2ActualLegacyInputContractSharesThreeOriginalCommands")]
[TestCase(9,new[]{"True","True","True","True"},TestName="Batch2ActualScrollbarContractSharesFloatAndConfigurationState")]
[TestCase(10,new[]{"True","True","True","True"},TestName="Batch2ActualTmpInputContractSharesThreeOriginalCommands")]
public void ActualFuiBridgeWithManagedExternalTypeShells(int n,string[] expected){var root=GeneratedCompilation.RepositoryRoot();var prefix="Packages/com.fujisheng.fui.ugui/Runtime/Rendering/UGUI/Elements/";var files=new[]{"DropdownElement.cs","LegacyDropdownElement.cs","TMPDropdownElement.cs","InputFieldElement.cs","LegacyInputFieldElement.cs","TMPInputFieldElement.cs","ScrollbarElement.cs"};var sources=files.Select(f=>(prefix+f,File.ReadAllText(Path.Combine(root,prefix,f)))).Concat(new[]{("ExternalTypeShells.cs",File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Tests","Batch2NativeShell.cs.txt")))}).ToArray();var a=GeneratedCompilation.Compile(sources);try{Assert.That((string[])a.GetType("NativeBatch2.Scenario").GetMethod("Run").Invoke(null,new object[]{n}),Is.EqualTo(expected));}catch(TargetInvocationException e)when(e.InnerException!=null){ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
}}
