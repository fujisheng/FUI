using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FUI.RedDot.Editor
{
    /// <summary>将打包的 Catalog Source Generator 安装到项目目标 asmdef 旁。</summary>
    public sealed class RedDotGeneratorSetupWindow : EditorWindow
    {
        AssemblyDefinitionAsset targetAssembly;

        [MenuItem("FUI/Red Dot/Setup Generator")]
        public static void Open()
        {
            var window = GetWindow<RedDotGeneratorSetupWindow>();
            window.titleContent = new GUIContent("Red Dot Generator Setup");
            window.minSize = new Vector2(480F, 150F);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Select the project assembly that declares [RedDotCatalog]. The generator DLL is installed beside its asmdef.", MessageType.Info);
            targetAssembly = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField("Target Assembly", targetAssembly, typeof(AssemblyDefinitionAsset), false);
            using (new EditorGUI.DisabledScope(targetAssembly == null))
            {
                if (GUILayout.Button("Install Generator"))
                {
                    Install();
                }
            }
        }

        void Install()
        {
            try
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RedDotGeneratorSetupWindow).Assembly);
                if (package == null)
                {
                    throw new InvalidOperationException("Cannot resolve com.fujisheng.fui.reddot package path.");
                }
                var source = Path.Combine(package.resolvedPath, "Roslyn~", "FUI.RedDot.Generators.dll");
                if (!File.Exists(source))
                {
                    throw new FileNotFoundException("Cannot find the packaged red dot generator.", source);
                }

                var asmdefPath = AssetDatabase.GetAssetPath(targetAssembly);
                var target = Path.Combine(Path.GetDirectoryName(asmdefPath), "FUI.RedDot.Generators.dll");
                File.Copy(source, target, true);
                if (!File.Exists(target + ".meta"))
                {
                    File.WriteAllText(target + ".meta", BuildGeneratorMeta());
                }
                AssetDatabase.Refresh();
                Debug.Log("FUI Red Dot Generator installed successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogError("FUI Red Dot Generator installation failed: " + exception.Message);
            }
        }

        static string BuildGeneratorMeta()
        {
            return @"fileFormatVersion: 2
guid: " + Guid.NewGuid().ToString("N") + @"
labels:
- RoslynAnalyzer
- SourceGenerator
PluginImporter:
  externalObjects: {}
  serializedVersion: 3
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 1
  validateReferences: 0
  platformData:
    Any:
      enabled: 0
      settings: {}
    Editor:
      enabled: 0
      settings:
        DefaultValueInitialized: true
    WindowsStoreApps:
      enabled: 0
      settings:
        CPU: AnyCPU
  userData:
  assetBundleName:
  assetBundleVariant:
";
        }
    }
}
