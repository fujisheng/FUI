using System.IO;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class GeneratedBindingTests
    {
        [Test]
        public void PureGeneratedBindingUpdatesTextAndCommand()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var view = new ProbeView();
                using (var projection = Projection.Create(view, vm))
                {
                    projection.Activate();
                    var initial = view.Label.Text.Value;
                    vm.Message = ""changed"";
                    var changed = view.Label.Text.Value;
                    view.Button.Click.Invoke();
                    return new[] { initial, changed, view.Label.Text.Value, vm.ClickCount.ToString() };
                }");

            Assert.That(result, Is.EqualTo(new[] { "initial", "changed", "clicked", "1" }));
        }

        [Test]
        public void DisposedProjectionStopsPropertyAndCommandDelivery()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var view = new ProbeView();
                var projection = Projection.Create(view, vm);
                projection.Activate();
                projection.Dispose();
                vm.Message = ""after-close"";
                view.Button.Click.Invoke();
                return new[] { view.Label.Text.Value, vm.ClickCount.ToString(), view.Destroyed.ToString() };");

            Assert.That(result, Is.EqualTo(new[] { "initial", "0", "True" }));
        }

        [Test]
        public void SharedViewModelKeepsOtherProjectionBoundAfterOneCloses()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var first = new ProbeView();
                var second = new ProbeView();
                var a = Projection.Create(first, vm);
                using (var b = Projection.Create(second, vm))
                {
                    a.Activate();
                    b.Activate();
                    vm.Message = ""shared"";
                    var before = first.Label.Text.Value + ""/"" + second.Label.Text.Value;
                    a.Dispose();
                    vm.Message = ""remaining"";
                    first.Button.Click.Invoke();
                    var clickCount = vm.ClickCount;
                    var remaining = first.Label.Text.Value + ""/"" + second.Label.Text.Value;
                    second.Button.Click.Invoke();
                    return new[] { before, remaining, clickCount.ToString(), vm.ClickCount.ToString() };
                }");

            Assert.That(result, Is.EqualTo(new[] { "shared/shared", "shared/remaining", "0", "1" }));
        }

        [TestCase("Label")]
        [TestCase("Button")]
        public void MissingRequiredElementFailsActivationAndCanBeDisposed(string missing)
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var view = new ProbeView(""" + missing + @""");
                var projection = Projection.Create(view, vm);
                string error = ""no failure"";
                try { projection.Activate(); }
                catch (InvalidOperationException failure) { error = failure.Message; }
                finally { projection.Dispose(); }
                vm.Message = ""after-failure"";
                view.Button.Click.Invoke();
                return new[] { error, vm.ClickCount.ToString(), view.Destroyed.ToString() };");

            Assert.That(result, Is.EqualTo(new[] { "Missing required element: " + missing, "0", "True" }));
        }

        [Test]
        public void ClosingCancelsGeneratedAsyncCommandAndLateCompletionCannotUpdateView()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var view = new ProbeView();
                var projection = Projection.Create(view, vm);
                projection.Activate();
                view.AsyncButton.Click.Invoke();
                var started = vm.Tokens[0].IsCancellationRequested;
                projection.Dispose();
                vm.PendingCommands[0].SetResult(true);
                if (!vm.EndedCommands[0].Task.Wait(5000)) throw new TimeoutException(""Command did not end"");
                return new[] { started.ToString(), vm.Tokens[0].IsCancellationRequested.ToString(),
                    vm.AsyncCompletions.ToString(), view.Label.Text.Value };");

            Assert.That(result, Is.EqualTo(new[] { "False", "True", "0", "initial" }));
        }

        [Test]
        public void ReopeningSameViewModelGivesNewCommandCancellationBoundary()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var old = new ProbeView();
                var first = Projection.Create(old, vm);
                first.Activate();
                old.AsyncButton.Click.Invoke();
                first.Dispose();
                var current = new ProbeView();
                using (var second = Projection.Create(current, vm))
                {
                    second.Activate();
                    current.AsyncButton.Click.Invoke();
                    vm.PendingCommands[0].SetResult(true);
                    if (!vm.EndedCommands[0].Task.Wait(5000)) throw new TimeoutException(""Old command did not end"");
                    var before = vm.AsyncCompletions;
                    vm.PendingCommands[1].SetResult(true);
                    if (!vm.EndedCommands[1].Task.Wait(5000)) throw new TimeoutException(""Current command did not end"");
                    return new[] { vm.Tokens[0].IsCancellationRequested.ToString(),
                        vm.Tokens[1].IsCancellationRequested.ToString(), before.ToString(),
                        vm.AsyncCompletions.ToString(), old.Label.Text.Value, current.Label.Text.Value };
                }");

            Assert.That(result, Is.EqualTo(new[] { "True", "False", "0", "1", "initial", "async-complete" }));
        }

        [Test]
        public void ParameterizedGeneratedCommandsDeliverOncePerEventAndUnbind()
        {
            var result = Run(@"
                var vm = new ProbeViewModel();
                var view = new ProbeView();
                var projection = Projection.Create(view, vm);
                projection.Activate();
                view.Float.ValueChanged.Invoke(0.75F);
                view.Bool.ValueChanged.Invoke(true);
                var once = vm.FloatCommands + ""/"" + vm.BoolCommands;
                view.Float.ValueChanged.Invoke(0.75F);
                view.Bool.ValueChanged.Invoke(true);
                var twice = vm.FloatCommands + ""/"" + vm.BoolCommands;
                view.Float.Value.Value = 0.3F;
                view.Bool.Value.Value = false;
                var afterWrite = vm.FloatCommands + ""/"" + vm.BoolCommands;
                projection.Dispose();
                view.Float.ValueChanged.Invoke(0.9F);
                view.Bool.ValueChanged.Invoke(false);
                return new[] { once, twice, afterWrite, vm.FloatCommands + ""/"" + vm.BoolCommands,
                    vm.LastFloat.ToString(""0.00"", System.Globalization.CultureInfo.InvariantCulture), vm.LastBool.ToString() };");
            Assert.That(result, Is.EqualTo(new[] { "1/1", "2/2", "2/2", "2/2", "0.75", "True" }));
        }

        static string[] Run(string body)
        {
            var sourcePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Tests", "HeadlessProbe.cs.txt");
            var source = File.ReadAllText(sourcePath).Replace("/* SCENARIO */", body);
            var assembly = GeneratedCompilation.Compile(("HeadlessProbe.cs", source));
            return (string[])assembly.GetType("PortableProbe.Scenario").GetMethod("Run").Invoke(null, null);
        }
    }
}
