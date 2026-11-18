using System;using System.IO;using System.Reflection;using System.Runtime.ExceptionServices;using NUnit.Framework;
namespace FUI.Portable.Tests{public sealed class Batch3ControlTests{
[TestCase(0,new[]{"True","True","True","True"},TestName="Batch3ImageContractsReuseExistingGraphicalAndSourceProperties")]
[TestCase(1,new[]{"b","texture-b","0.8","0"},TestName="Batch3SourceAndFillBindingsExecuteAgainstActualGenerator")]
[TestCase(2,new[]{"True","False","2","True","True","False","True"},TestName="Batch3DisplayAndScrollContractsBindExistingPublicScalars")]
[TestCase(3,new[]{"0.2","0.3","0.3","0"},TestName="Batch3ProgrammaticScrollDoesNotEmitUserCommand")]
[TestCase(4,new[]{"0.4","0.6","2","True"},TestName="Batch3ScrollEventsSynchronizeVmAndReuseActualListRows")]
[TestCase(5,new[]{"0","1"},TestName="Batch3ScrollInputRespectsOwnerAndOwnVisibility")]
[TestCase(6,new[]{"a","texture-a","0"},TestName="Batch3DisplayAndScrollBindingsUnbindOnDeactivate")]
[TestCase(7,new[]{"0","True","True"},TestName="Batch3DisposeClearsDisplayAndScrollListeners")]
[TestCase(8,new[]{"a","b","texture-a","texture-b","1","0.8","1"},TestName="Batch3SharedVmDisplayAndScrollProjectionsUnbindIndependently")]
[TestCase(9,new[]{"rejected"},TestName="Batch3ImageBackendCannotSilentlySatisfyRawTextureContract")]
public void ActualGeneratedDisplayBindings(int n,string[] expected){var a=GeneratedCompilation.Compile(("Batch3Probe.cs",File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Tests","Batch3Probe.cs.txt"))));try{Assert.That((string[])a.GetType("Batch3.Scenario").GetMethod("Run").Invoke(null,new object[]{n}),Is.EqualTo(expected));}catch(TargetInvocationException e)when(e.InnerException!=null){ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
}}
