using System;using System.IO;using System.Linq;using System.Reflection;using System.Runtime.ExceptionServices;using NUnit.Framework;
namespace FUI.Portable.Tests{public sealed class Batch3NativeTests{
[TestCase(0,new[]{"True","True","True","True"},TestName="Batch3NativeImageInterfacesReuseOriginalSourceAndGraphicalObjects")]
[TestCase(1,new[]{"1","0","b","b"},TestName="Batch3ActualSpriteSlotReleasesPreviousLeaseOnSourceReplacement")]
[TestCase(2,new[]{"0","1","a","a"},TestName="Batch3ActualSpriteSlotRollsBackFailedSourceAndReleasesFailedCandidate")]
[TestCase(3,new[]{"1","null","direct"},TestName="Batch3ActualSpriteSlotDirectAssignmentClearsSourceAndReleasesLease")]
[TestCase(4,new[]{"0","1","a","null"},TestName="Batch3MaterialLeaseRemainsIndependentFromSpriteLease")]
[TestCase(5,new[]{"1","1","null"},TestName="Batch3ActualTextureSlotReplacementAndClearReleaseEachLeaseOnce")]
[TestCase(6,new[]{"2","2","0.2","0.8","1"},TestName="Batch3NativeScrollConvertsEveryOriginalEventOnce")]
[TestCase(7,new[]{"0","0.3","40"},TestName="Batch3OriginalScrollSynchronizationSuppressesProgrammaticEvents")]
[TestCase(8,new[]{"1","1","0"},TestName="Batch3ActualSlotsDisposeAndRejectPostDisposeSourceWrites")]
[TestCase(9,new[]{"0","0","0.6","0.7","0.6","70"},TestName="Batch3ScrollPointBridgeRoundTripsThroughOriginalVectorState")]
[TestCase(10,new[]{"0","0"},TestName="Batch3ScrollBridgeAndBackendSubscriptionsReleaseTogether")]
public void ActualNativeSourcesWithManagedExternalTypes(int n,string[]expected){var root=GeneratedCompilation.RepositoryRoot();var p="Packages/com.fujisheng.fui.ugui/Runtime/Rendering/UGUI/";var files=new[]{"Elements/ImageElement.cs","Elements/RawImageElement.cs","Elements/ScrollViewElement.cs","Base/AssetSourceSlot.cs","IAssetLoader.cs"};var src=files.Select(f=>(p+f,File.ReadAllText(Path.Combine(root,p,f)))).Concat(new[]{("ManagedExternalTypes.cs",File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Tests","Batch3NativeShell.cs.txt")))}).ToArray();var a=GeneratedCompilation.Compile(src);try{Assert.That((string[])a.GetType("NativeBatch3.Scenario").GetMethod("Run").Invoke(null,new object[]{n}),Is.EqualTo(expected));}catch(TargetInvocationException e)when(e.InnerException!=null){ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
}}
