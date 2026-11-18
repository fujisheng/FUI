using System;using System.IO;using System.Reflection;using System.Runtime.ExceptionServices;using NUnit.Framework;
namespace FUI.Portable.Tests{public sealed class Batch2ControlTests{
[TestCase(0,new[]{"True","True","True","True","True","True","False"},TestName="Batch2OriginalStateAndEventAliasesShareObjects")]
[TestCase(1,new[]{"10","code","0.8","0"},TestName="Batch2ProgrammaticAssignmentsDoNotInvokeUserCommands")]
[TestCase(2,new[]{"10","10","True","drop:10"},TestName="Batch2TmpMultiSelectMaskIsNotClampedToOptionIndex")]
[TestCase(3,new[]{"1","drop:10|drop:10"},TestName="Batch2DropdownBackendEventsRemainDistinctFromPropertyDeduplication")]
[TestCase(4,new[]{"","","edit:|end:finish|submit:send"},TestName="Batch2InputEventsNormalizeAndRemainThreeDistinctChannels")]
[TestCase(5,new[]{"seed","seed","end:end|submit:"},TestName="Batch2ReadOnlyBlocksEditingButPreservesFinishAndSubmit")]
[TestCase(6,new[]{"0","9","go","0.9","3"},TestName="Batch2OwnVisibilityAndInteractabilityGateUserInput")]
[TestCase(7,new[]{"-0.75","scroll:-0.75|scroll:-0.75"},TestName="Batch2ScrollbarPreservesEventPayloadAndNotificationCount")]
[TestCase(8,new[]{"second","seed","second","10","5","3"},TestName="Batch2SharedVmProjectionsUnbindIndependently")]
[TestCase(9,new[]{"False","5","open","1"},TestName="Batch2ParentCoverDoesNotOverwriteOwnDisabledState")]
[TestCase(10,new[]{"0","True","True","True"},TestName="Batch2DisposedElementsClearAllUserAndPropertyListeners")]
[TestCase(11,new[]{"True","null","0.7","0"},TestName="Batch2OptionsAndFadeWritesDoNotGenerateUserSelection")]
[TestCase(12,new[]{"7","2","0.5","0.3","4"},TestName="Batch2ActualGeneratorBindsInheritedAndConfigurationMembers")]
[TestCase(13,new[]{"rejected"},TestName="Batch2LegacyDropdownCannotSilentlySatisfyMultiSelectTarget")]
[TestCase(14,new[]{"rejected"},TestName="Batch2DisplayTextCannotSilentlySatisfyEditableInputTarget")]
[TestCase(15,new[]{"False","False","False","0"},TestName="Batch2InheritedSelectableBindingsUseRequestedControlContracts")]
public void RealGeneratedControlBindings(int n,string[] expected){var assembly=GeneratedCompilation.Compile(("Batch2Probe.cs",File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Tests","Batch2ControlsProbe.cs.txt"))));try{Assert.That((string[])assembly.GetType("Batch2Probe.Scenario").GetMethod("Run").Invoke(null,new object[]{n}),Is.EqualTo(expected));}catch(TargetInvocationException e)when(e.InnerException!=null){ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
}}
