using System;
using FUI.Portable.Tests;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class HeadlessTestingTests
    {
        // 删除严格名称/类型检查会让下面的用户可观察错误断言失败。
        [Test]
        public void NamedLookupRejectsMissingWrongTypeAndDuplicateNames()
        {
            Assert.That(Run(@"
                var label = new HeadlessTextElement(""Title"");
                var view = new HeadlessView(""Example"", label);
                var errors = new List<string>();
                errors.Add(ReferenceEquals(label, view.GetElement<ITextElement>(""Title"")).ToString());
                try { view.GetElement<ITextElement>(""Missing""); } catch (InvalidOperationException e) { errors.Add(e.Message); }
                try { view.GetElement<IClickElement>(""Title""); } catch (InvalidOperationException e) { errors.Add(e.Message); }
                try { new HeadlessView(""Duplicate"", label, new HeadlessClickElement(""Title"")); } catch (ArgumentException) { errors.Add(""duplicate rejected""); }
                return errors.ToArray();"), Is.EqualTo(new[] { "True", "Missing required element: Missing", "Element type mismatch: Title", "duplicate rejected" }));
        }

        // 额外派发程序赋值或重复输入会产生重复业务调用。
        [Test]
        public void SemanticInputEmitsOnceAndProgrammaticValueWritesEmitNone()
        {
            Assert.That(Run(@"
                var volume = new HeadlessFloatElement(""Volume"");
                var toggle = new HeadlessBoolElement(""Toggle"");
                var button = new HeadlessClickElement(""Button"");
                int floats=0, bools=0, clicks=0;
                volume.ValueChanged.AddListener(v => floats++);
                toggle.ValueChanged.AddListener(v => bools++);
                button.Click.AddListener(() => clicks++);
                volume.Input(0.75F); volume.Input(0.75F); volume.Value.Value=0.2F;
                toggle.Input(true); toggle.Input(true); toggle.Value.Value=false;
                button.Input();
                return new[] { floats.ToString(), bools.ToString(), clicks.ToString(), volume.Value.Value.ToString(CultureInfo.InvariantCulture), toggle.Value.Value.ToString() };"), Is.EqualTo(new[] { "1", "1", "1", "0.2", "False" }));
        }

        // Destroy后仍保留Command/属性监听会使已关闭投影重新产生效果。
        [Test]
        public void DestroyIsIdempotentAndStopsElementInput()
        {
            Assert.That(Run(@"
                var value = new HeadlessFloatElement(""Value"");
                var button = new HeadlessClickElement(""Button"");
                var view = new HeadlessView(""Example"", value, button);
                int calls=0;
                button.Click.AddListener(() => calls++);
                value.ValueChanged.AddListener(v => calls++);
                view.Destroy(); view.Destroy();
                button.Input(); value.Input(1F); button.Click.Invoke();
                return new[] { view.Destroyed.ToString(), calls.ToString(), value.Value.Value.ToString(CultureInfo.InvariantCulture) };"), Is.EqualTo(new[] { "True", "0", "0" }));
        }

        [Test]
        public void TestingAssemblyHasNoUnityReferences()
        {
            Assert.That(Run(@"return typeof(HeadlessView).Assembly.GetReferencedAssemblies().Where(a => a.Name.StartsWith(""Unity"")).Select(a => a.Name).ToArray();"), Is.Empty);
        }

        [Test]
        public void DestroyReleasesOtherElementsWhenOneCleanupThrows()
        {
            Assert.That(Run(@"
                var bad=new FailingElement(); var button=new HeadlessClickElement(""Button"");
                var view=new HeadlessView(""Example"",bad,button); int clicks=0;
                button.Click.AddListener(()=>clicks++); string error=""none"";
                try { view.Destroy(); } catch(Exception e) { error=e.Message; }
                view.Destroy(); button.Click.Invoke();
                return new[]{error,button.IsDisposed.ToString(),clicks.ToString(),bad.Calls.ToString()};"),Is.EqualTo(new[]{"cleanup failure","True","0","1"}));
        }

        internal static string[] Run(string body)
        {
            var source = @"using System; using System.Collections.Generic; using System.Linq; using System.Globalization;
                using FUI.Rendering; using FUI.Testing;
                public sealed class FailingElement : IElement, IDisposable { public string Name => ""Bad""; public IElement Parent => null; public int Calls; public void Dispose() { Calls++; throw new InvalidOperationException(""cleanup failure""); } }
                public static class Scenario { public static string[] Run() { " + body + @" } }";
            var assembly = GeneratedCompilation.Compile(("HeadlessScenario.cs", source));
            return (string[])assembly.GetType("Scenario").GetMethod("Run").Invoke(null, null);
        }
    }
}
