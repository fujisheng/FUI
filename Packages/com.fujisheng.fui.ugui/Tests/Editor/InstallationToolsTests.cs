using FUI.Editor.Installation;
using FUI.Rendering.UGUI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FUI.Tests
{
    public sealed class InstallationToolsTests
    {
        const string AssetFolder = "Assets/__FUIInstallationTests__";
        string fileSystemFolder;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "__FUIInstallationTests__");
            }

            fileSystemFolder = Path.Combine(Path.GetTempPath(), "FUIInstallationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fileSystemFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(AssetFolder);
            if (!string.IsNullOrEmpty(fileSystemFolder) && Directory.Exists(fileSystemFolder))
            {
                Directory.Delete(fileSystemFolder, true);
            }
        }

        [Test]
        public void EditorToolsUseTopLevelFuiMenu()
        {
            // PageWizard moved to FUI.Rendering.UGUI.Editor with the uGUI split; scan both editor assemblies.
            var menuPaths = new[] { typeof(PageWizard).Assembly, typeof(SetupWindow).Assembly }
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .SelectMany(method => method.GetCustomAttributesData())
                .Where(attribute => attribute.AttributeType == typeof(MenuItem))
                .Select(attribute => (string)attribute.ConstructorArguments[0].Value)
                .ToArray();

            Assert.That(menuPaths, Does.Contain("FUI/Setup"));
            Assert.That(menuPaths, Does.Contain("FUI/Page Wizard"));
            Assert.That(menuPaths, Does.Contain("FUI/Validate Views"));
            Assert.That(
                menuPaths.Where(path => path.StartsWith("Tools/FUI/", StringComparison.Ordinal)),
                Is.Empty);
        }

        [Test]
        public void PageWizardValidatesAssetFoldersAndNormalizesViewNames()
        {
            Assert.That(PageWizard.CanCreate("Inventory", "Assets"), Is.True);
            Assert.That(PageWizard.CanCreate("Inventory", "Assets/UI"), Is.True);
            Assert.That(PageWizard.CanCreate("Inventory", "AssetsExtra"), Is.False);
            Assert.That(PageWizard.CanCreate(string.Empty, "Assets/UI"), Is.False);
            Assert.That(PageWizard.CanCreate("Inventory", string.Empty), Is.False);
            Assert.That(PageWizard.NormalizePageName("Inventory"), Is.EqualTo("InventoryView"));
            Assert.That(PageWizard.NormalizePageName("InventoryView"), Is.EqualTo("InventoryView"));
        }

        [Test]
        public void PageWizardProducesOrthogonalPolicyPresets()
        {
            Assert.That(
                PageWizard.PolicySnippet(PageWizard.PagePreset.FullScreen),
                Is.EqualTo("[RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide)]"));
            Assert.That(
                PageWizard.PolicySnippet(PageWizard.PagePreset.Popup),
                Is.EqualTo("[RoutePolicy(Layer.Popup, CoverageMode = CoverageMode.KeepVisible)]"));
            Assert.That(
                PageWizard.PolicySnippet(PageWizard.PagePreset.Tips),
                Is.EqualTo("[RoutePolicy(Layer.Tips, HistoryMode = HistoryMode.Transient, CoverageMode = CoverageMode.KeepVisible, AllowMultiple = true)]"));
        }

        [Test]
        public void PageWizardCreatesAValidIndependentCanvasPrefab()
        {
            var prefab = PageWizard.CreatePrefabAsset("CoverageProbePage", AssetFolder);
            var assetPath = AssetDatabase.GetAssetPath(prefab);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo("CoverageProbePageView"));
            Assert.That(assetPath, Does.StartWith(AssetFolder + "/CoverageProbePageView"));
            Assert.That(prefab.GetComponent<View>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(prefab.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SettingsReadTargetAssemblyNameAndAssetPath()
        {
            var asmdefPath = AssetFolder + "/FUI.Test.Target.asmdef";
            File.WriteAllText(asmdefPath, "{\"name\":\"FUI.Test.Target\",\"rootNamespace\":\"FUI.Tests\"}");
            AssetDatabase.ImportAsset(asmdefPath, ImportAssetOptions.ForceSynchronousImport);
            var target = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmdefPath);
            var settings = ScriptableObject.CreateInstance<ILPostProcessingSettings>();
            try
            {
                settings.targetProject = target;

                Assert.That(settings.GetTargetProjectName(), Is.EqualTo("FUI.Test.Target"));
                Assert.That(settings.GetTargetAssemblyDefinitionPath(), Is.EqualTo(asmdefPath));

                settings.targetProject = null;
                Assert.Throws<Exception>(() => settings.GetTargetProjectName());
                Assert.Throws<Exception>(() => settings.GetTargetAssemblyDefinitionPath());
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void SetupWritesAnalyzerOnlyMetaAndDeletesLegacyGenerator()
        {
            var nestedDirectory = Path.Combine(fileSystemFolder, "Generated", "UI");
            SetupWindow.TryCreateDirectory(nestedDirectory);
            Assert.That(Directory.Exists(nestedDirectory), Is.True);

            var metaPath = Path.Combine(nestedDirectory, "FUI.Generators.dll.meta");
            SetupWindow.WriteGeneratorMeta(metaPath);
            var meta = File.ReadAllText(metaPath);
            Assert.That(meta, Does.Contain("- RoslynAnalyzer"));
            Assert.That(meta, Does.Contain("- SourceGenerator"));
            Assert.That(meta, Does.Contain("Any:\n      enabled: 0"));
            Assert.That(meta, Does.Contain("isExplicitlyReferenced: 1"));

            var legacyDll = Path.Combine(nestedDirectory, "FUI.SourceGenerator.dll");
            File.WriteAllText(legacyDll, "legacy");
            File.WriteAllText(legacyDll + ".meta", "legacy meta");
            SetupWindow.DeleteLegacyGenerator(nestedDirectory);

            Assert.That(File.Exists(legacyDll), Is.False);
            Assert.That(File.Exists(legacyDll + ".meta"), Is.False);
            Assert.That(File.Exists(metaPath), Is.True);
        }

        [Test]
        public void OptionalPackageCatalogKeepsToolsExternalAndVersioned()
        {
            Assert.That(OptionalPackageCatalog.PublicPackages, Has.Length.EqualTo(2));
            Assert.That(OptionalPackageCatalog.UnityCli.Identifier, Is.EqualTo("com.fujisheng.unitycli@0.4.0"));
            Assert.That(OptionalPackageCatalog.FuiCli.Identifier, Is.EqualTo("com.fujisheng.fui.cli@1.0.0"));
            Assert.That(OptionalPackageCatalog.UnityCli.IsAvailableForInstallation, Is.False);
            Assert.That(OptionalPackageCatalog.FuiCli.IsAvailableForInstallation, Is.False);
            Assert.That(
                OptionalPackageCatalog.FuiCli.Dependencies,
                Is.EqualTo(new[] { "com.fujisheng.unitycli" }));
        }

        [Test]
        public void OptionalPackageInstallerSerializesRequestsAndCommitsSuccess()
        {
            var client = new FakeOptionalPackageClient();
            var installer = new OptionalPackageInstaller(client);

            Assert.That(installer.TryInstall(OptionalPackageCatalog.UnityCli), Is.True);
            Assert.That(installer.IsBusy, Is.True);
            Assert.That(
                installer.GetStatus(OptionalPackageCatalog.UnityCli).State,
                Is.EqualTo(OptionalPackageState.Installing));
            Assert.That(installer.TryInstall(OptionalPackageCatalog.FuiCli), Is.False);
            Assert.That(client.LastIdentifier, Is.EqualTo("com.fujisheng.unitycli@0.4.0"));

            client.Complete(true);
            installer.Update();

            var status = installer.GetStatus(OptionalPackageCatalog.UnityCli);
            Assert.That(installer.IsBusy, Is.False);
            Assert.That(status.State, Is.EqualTo(OptionalPackageState.Installed));
            Assert.That(status.InstalledVersion, Is.EqualTo("0.4.0"));
        }

        [Test]
        public void OptionalPackageInstallerReportsFailureAndAllowsRetry()
        {
            var client = new FakeOptionalPackageClient();
            var installer = new OptionalPackageInstaller(client);

            Assert.That(installer.TryInstall(OptionalPackageCatalog.UnityCli), Is.True);
            client.Complete(false, "Registry package was not found.");
            installer.Update();

            var failed = installer.GetStatus(OptionalPackageCatalog.UnityCli);
            Assert.That(failed.State, Is.EqualTo(OptionalPackageState.Failed));
            Assert.That(failed.ErrorMessage, Is.EqualTo("Registry package was not found."));
            Assert.That(installer.TryInstall(OptionalPackageCatalog.UnityCli), Is.True);
            Assert.That(installer.GetStatus(OptionalPackageCatalog.UnityCli).State, Is.EqualTo(OptionalPackageState.Installing));
        }

        [Test]
        public void OptionalPackageInstallerResolvesFuiCliDependencySequentially()
        {
            var client = new FakeOptionalPackageClient();
            var installer = new OptionalPackageInstaller(client);

            Assert.That(installer.TryInstall(OptionalPackageCatalog.FuiCli), Is.True);
            Assert.That(client.LastIdentifier, Is.EqualTo(OptionalPackageCatalog.UnityCli.Identifier));
            Assert.That(installer.GetStatus(OptionalPackageCatalog.FuiCli).State, Is.EqualTo(OptionalPackageState.Installing));

            client.Complete(true);
            installer.Update();

            Assert.That(client.LastIdentifier, Is.EqualTo(OptionalPackageCatalog.FuiCli.Identifier));
            Assert.That(installer.IsBusy, Is.True);

            client.Complete(true);
            installer.Update();

            Assert.That(installer.IsBusy, Is.False);
            Assert.That(installer.GetStatus(OptionalPackageCatalog.FuiCli).State, Is.EqualTo(OptionalPackageState.Installed));
        }

        [Test]
        public void OptionalPackageInstallerDoesNotReinstallExistingPackage()
        {
            var client = new FakeOptionalPackageClient();
            client.Installed[OptionalPackageCatalog.UnityCli.PackageId] = "0.4.0";
            var installer = new OptionalPackageInstaller(client);

            Assert.That(installer.TryInstall(OptionalPackageCatalog.UnityCli), Is.False);
            Assert.That(client.LastIdentifier, Is.Null);
        }

        sealed class FakeOptionalPackageClient : IOptionalPackageClient
        {
            readonly FakeOptionalPackageRequest request = new FakeOptionalPackageRequest();

            public readonly Dictionary<string, string> Installed = new Dictionary<string, string>();
            public string LastIdentifier { get; private set; }

            public IReadOnlyDictionary<string, string> GetInstalledPackages() => Installed;

            public IOptionalPackageRequest Add(string identifier)
            {
                LastIdentifier = identifier;
                request.Reset();
                return request;
            }

            public void Complete(bool succeeded, string error = null)
            {
                request.Complete(succeeded, error);
                if (!succeeded)
                {
                    return;
                }

                var separator = LastIdentifier.LastIndexOf('@');
                Installed[LastIdentifier.Substring(0, separator)] = LastIdentifier.Substring(separator + 1);
            }
        }

        sealed class FakeOptionalPackageRequest : IOptionalPackageRequest
        {
            public bool IsCompleted { get; private set; }
            public bool Succeeded { get; private set; }
            public string ErrorMessage { get; private set; }

            public void Reset()
            {
                IsCompleted = false;
                Succeeded = false;
                ErrorMessage = null;
            }

            public void Complete(bool succeeded, string error)
            {
                IsCompleted = true;
                Succeeded = succeeded;
                ErrorMessage = error;
            }
        }
    }
}
