using FUI.Editor.Installation;
using FUI.Rendering.UGUI;
using NUnit.Framework;
using System;
using System.IO;
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
    }
}
