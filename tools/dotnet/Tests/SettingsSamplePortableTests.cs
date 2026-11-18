using System.IO;
using System;
using System.Linq;
using System.Reflection;
using FUI.Infrastructure.Binding;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class SettingsSamplePortableTests
    {
        [Test]
        public void ActualSettingsSourceSupportsInitialStateTwoWayInputAndClose()
        {
            var result = Run(@"
                var vm = new SettingsViewModel();
                var view = new HeadlessSettingsView();
                int closes = 0;
                vm.CloseRequested += () => closes++;
                using (var projection = Projection.Create(view, vm))
                {
                    projection.Activate();
                    var initial = view.Title.Text.Value + ""|"" + view.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture) +
                        ""|"" + view.Vibration.Value.Value + ""|"" + view.Status.Text.Value;
                    view.Volume.Input(0.75F);
                    var volume = vm.Volume.ToString(""0.00"", CultureInfo.InvariantCulture) + ""|"" + view.Status.Text.Value;
                    view.Vibration.Input(false);
                    var vibration = vm.VibrationEnabled + ""|"" + view.Status.Text.Value;
                    vm.Volume = 0.25F;
                    var reverse = view.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture);
                    view.Close.Click.Invoke();
                    return new[] { initial, volume, vibration, reverse, closes.ToString() };
                }");

            Assert.That(result, Is.EqualTo(new[] {
                "FUI Settings|0.50|True|FUI binding is active.",
                "0.75|Volume: 0.75", "False|Vibration disabled", "0.25", "1" }));
        }

        [Test]
        public void RepeatedHeadlessInputEmitsOneValueEventAndVmWriteEmitsNone()
        {
            var result = Run(@"
                var vm = new SettingsViewModel();
                var view = new HeadlessSettingsView();
                using (var projection = Projection.Create(view, vm))
                {
                    projection.Activate();
                    int volumeEvents = 0, vibrationEvents = 0;
                    view.Volume.ValueChanged.AddListener(value => volumeEvents++);
                    view.Vibration.ValueChanged.AddListener(value => vibrationEvents++);
                    view.Volume.Input(0.75F);
                    view.Volume.Input(0.75F);
                    var userCount = volumeEvents;
                    vm.Volume = 0.3F;
                    view.Vibration.Input(false);
                    view.Vibration.Input(false);
                    return new[] { userCount.ToString(), volumeEvents.ToString(), vibrationEvents.ToString(), vm.Status };
                }");

            Assert.That(result, Is.EqualTo(new[] { "1", "1", "1", "Vibration disabled" }));
        }

        [Test]
        public void ClosedSettingsControlsCannotChangeVmOrRequestClose()
        {
            var result = Run(@"
                var vm = new SettingsViewModel();
                var view = new HeadlessSettingsView();
                int closes = 0;
                vm.CloseRequested += () => closes++;
                var projection = Projection.Create(view, vm);
                projection.Activate();
                projection.Dispose();
                view.Volume.Input(0.9F);
                view.Vibration.Input(false);
                view.Close.Click.Invoke();
                return new[] { vm.Volume.ToString(""0.00"", CultureInfo.InvariantCulture),
                    vm.VibrationEnabled.ToString(), closes.ToString(), view.Destroyed.ToString() };");

            Assert.That(result, Is.EqualTo(new[] { "0.50", "True", "0", "True" }));
        }

        [TestCase("Volume")]
        [TestCase("Close")]
        public void MissingSettingsControlFailsInsteadOfSilentlySkippingBinding(string missing)
        {
            var result = Run(@"
                var vm = new SettingsViewModel();
                var view = new HeadlessSettingsView(""" + missing + @""");
                var projection = Projection.Create(view, vm);
                string error = ""no failure"";
                try { projection.Activate(); }
                catch (InvalidOperationException failure) { error = failure.Message; }
                finally { projection.Dispose(); }
                return new[] { error, view.Destroyed.ToString() };");

            Assert.That(result, Is.EqualTo(new[] { "Missing required element: " + missing, "True" }));
        }

        [Test]
        public void SameSettingsVmProjectsIntoTwoViewsAndOnlyLiveViewStaysBound()
        {
            var result = Run(@"
                var vm = new SettingsViewModel();
                var first = new HeadlessSettingsView();
                var second = new HeadlessSettingsView();
                var a = Projection.Create(first, vm);
                using (var b = Projection.Create(second, vm))
                {
                    a.Activate(); b.Activate();
                    first.Volume.Input(0.75F);
                    var shared = second.Volume.Value.Value;
                    a.Dispose();
                    second.Volume.Input(0.25F);
                    return new[] { shared.ToString(""0.00"", CultureInfo.InvariantCulture),
                        first.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture),
                        second.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture) };
                }");

            Assert.That(result, Is.EqualTo(new[] { "0.75", "0.75", "0.25" }));
        }

        [Test]
        public void ActualSettingsManifestTargetTypesResolveWithCurrentEditorContract()
        {
            var assembly = Compile("return new string[0];");
            var manifests = assembly.GetTypes().Select(type => type.GetProperty("Info", BindingFlags.Public | BindingFlags.Static))
                .Where(property => property?.PropertyType == typeof(BindingManifest))
                .Select(property => (BindingManifest)property.GetValue(null)).ToArray();
            Assert.That(manifests, Has.Length.EqualTo(1));
            var targets = manifests[0].properties.Select(property => property.targetInfo.type)
                .Concat(manifests[0].commands.Select(command => command.targetInfo.type)).Distinct().ToArray();
            Assert.That(targets, Has.Length.EqualTo(4));
            var missing = targets.Where(name => !AppDomain.CurrentDomain.GetAssemblies()
                .Any(loaded => loaded.GetType(name, throwOnError: false) != null)).ToArray();
            Assert.That(missing, Is.Empty, "当前 Editor 的 Assembly.GetType 无法加载生成目标：" + string.Join(", ", missing));
        }

        [Test]
        public void GeneratedSettingsRouteClosesThroughHostIntentAndReleasesLeaseOnce()
        {
            var result = Run(@"
                var provider = new HeadlessSettingsProvider();
                var navigator = new Navigator(provider, new OwnerThreadDispatcher());
                navigator.Initialize();
                try
                {
                    var vm = new SettingsViewModel();
                    ViewHandle handle = default;
                    int closes = 0;
                    vm.CloseRequested += () => { closes++; navigator.Close(handle); };
                    handle = navigator.OpenWithViewModel(Routes.SettingsView, vm);
                    var lease = provider.Leases[0];
                    lease.Settings.Volume.Input(0.75F);
                    lease.Settings.Close.Click.Invoke();
                    lease.Settings.Close.Click.Invoke();
                    return new[] { navigator.IsAlive(handle).ToString(), closes.ToString(),
                        vm.Volume.ToString(""0.00"", CultureInfo.InvariantCulture), lease.DisposeCount.ToString(),
                        ((HeadlessAssetScope)lease.Assets).DisposeCount.ToString(), lease.Settings.Destroyed.ToString() };
                }
                finally { navigator.Shutdown(); }");

            Assert.That(result, Is.EqualTo(new[] { "False", "1", "0.75", "1", "1", "True" }));
        }

        [Test]
        public void CachedGeneratedSettingsRebindsNewVmAndInvalidatesOldHandle()
        {
            var result = Run(@"
                var provider = new HeadlessSettingsProvider();
                var navigator = new Navigator(provider, new OwnerThreadDispatcher());
                navigator.Initialize();
                try
                {
                    var route = Routes.SettingsView.CreateViewVariant(""CachedSettings"", new RoutePolicy(cacheMode: CacheMode.KeepAlive));
                    var oldVm = new SettingsViewModel();
                    var oldHandle = navigator.OpenWithViewModel(route, oldVm);
                    var view = provider.Leases[0].Settings;
                    view.Volume.Input(0.75F);
                    navigator.Close(oldHandle);
                    var newVm = new SettingsViewModel();
                    newVm.Volume = 0.25F;
                    var newHandle = navigator.OpenWithViewModel(route, newVm);
                    var rebound = view.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture);
                    oldVm.Volume = 0.9F;
                    var afterOldWrite = view.Volume.Value.Value.ToString(""0.00"", CultureInfo.InvariantCulture);
                    view.Volume.Input(0.3F);
                    var count = provider.Leases.Count;
                    var same = ReferenceEquals(view, provider.Leases[0].Settings);
                    var state = navigator.IsAlive(oldHandle) + ""/"" + navigator.IsAlive(newHandle) + ""/"" + (oldHandle != newHandle);
                    navigator.Shutdown();
                    return new[] { count.ToString(), same.ToString(), state, rebound, afterOldWrite,
                        oldVm.Volume.ToString(""0.00"", CultureInfo.InvariantCulture), newVm.Volume.ToString(""0.00"", CultureInfo.InvariantCulture),
                        provider.Leases[0].DisposeCount.ToString(), ((HeadlessAssetScope)provider.Leases[0].Assets).DisposeCount.ToString() };
                }
                finally { navigator.Shutdown(); }");

            Assert.That(result, Is.EqualTo(new[] { "1", "True", "False/True/True", "0.25", "0.25", "0.90", "0.30", "1", "1" }));
        }

        static string[] Run(string body)
        {
            var assembly = Compile(body);
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                return (string[])assembly.GetType("FUI.Samples.Settings.PortableSettingsScenario").GetMethod("Run").Invoke(null, null);
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        }

        static Assembly Compile(string body)
        {
            var root = GeneratedCompilation.RepositoryRoot();
            var sample = Path.Combine(root, "Packages", "com.fujisheng.fui.ugui", "Samples~", "Settings Sample");
            var host = @"
                using System;
                using System.Globalization;
                using FUI.Rendering;
                using FUI.Navigation;
                namespace FUI.Samples.Settings
                {
                    public static class PortableSettingsScenario
                    {
                        public static string[] Run()
                        {
                            Routes.Initialize();
                            " + body + @"
                        }
                    }
                }";
            var controls = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Tests", "HeadlessSettings.cs.txt"));
            return GeneratedCompilation.Compile(
                ("SettingsViewModel.cs", File.ReadAllText(Path.Combine(sample, "Scripts", "SettingsViewModel.cs"))),
                ("AssemblyInfo.cs", File.ReadAllText(Path.Combine(sample, "AssemblyInfo.cs"))),
                ("HeadlessSettings.cs", controls), ("PortableSettingsScenario.cs", host));
        }
    }
}
