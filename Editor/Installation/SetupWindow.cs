using System.IO;
using FUI.Editor;
using FUI.Editor.Inspection;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;

namespace FUI.Editor.Installation
{
    /// <summary>
    /// 编译器安装窗口
    /// </summary>
    internal class SetupWindow : EditorWindow
    {
        const string dataPath = "./FUI";
        const string settingPath = "Assets/Editor Default Resources/FUI/Settings/ILPostProcessingSettings.asset";

        ILPostProcessingSettings setting;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            ViewInstanceRegistry.GetInstance(null);
            BindingCatalog.Instance.GetContextInfo(null);
        }

        [MenuItem("Tools/FUI/Setup")]
        public static void ShowWindow()
        {
            var window = GetWindowWithRect<SetupWindow>(new Rect(Vector2.zero, new Vector2(1080, 720)));
            window.titleContent = new GUIContent("FUI Setup");
        }

        void OnEnable()
        {
            setting = LoadOrCreateSetting();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                var targetProjectContent = new GUIContent("TargetProject", "[Required] target project assembly definition asset.");
                setting.targetProject = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField(targetProjectContent, setting.targetProject, typeof(AssemblyDefinitionAsset), false);
                var outputContent = new GUIContent("Output", "[Required] target project dll output path, if there are no special reasons, please do not make any changes.");
                setting.output = EditorGUILayout.TextField(outputContent, setting.output);
                var generateTypeContent = new GUIContent("GenerateType", "[Required] binding context generate type.");
                setting.generateType = (BindingContextGenerateType)EditorGUILayout.EnumFlagsField(generateTypeContent, setting.generateType);
                var bindingInfoOutputPathContent = new GUIContent("BindingInfoOutputPath", "[Optional] binding info output path, will use it in view inspector.");
                setting.bindingInfoOutputPath = EditorGUILayout.TextField(bindingInfoOutputPathContent, setting.bindingInfoOutputPath);

                if (GUILayout.Button("Save Settings"))
                {
                    EditorUtility.SetDirty(setting);
                    AssetDatabase.SaveAssets();
                    UnityEngine.Debug.Log("Save FUI ILPostProcessing Settings Success");
                }

                if (GUILayout.Button("Install Generator"))
                {
                    InstallGeneratorToTargetProject();
                }

                EditorGUILayout.HelpBox("The assembly-level ViewModule.PropertyMode selects the property mode. Mixed mode requires Unity's official Cecil package.", MessageType.Info);
                if (GUILayout.Button("Install Cecil For Mixed Mode"))
                {
                    UnityEditor.PackageManager.Client.Add("com.unity.nuget.mono-cecil@1.11.6");
                }
            }
            EditorGUILayout.EndVertical();

        }

        /// <summary>
        /// 创建或者加载一个编译器设置
        /// </summary>
        /// <returns></returns>
        ILPostProcessingSettings LoadOrCreateSetting() 
        {
            var directory = Path.GetDirectoryName(settingPath);
            TryCreateDirectory(directory);
            var setting = AssetDatabase.LoadAssetAtPath<ILPostProcessingSettings>(settingPath);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<ILPostProcessingSettings>();
                setting.output = "./Library/ScriptAssemblies";
                setting.bindingInfoOutputPath = $"{dataPath}/BindingInfo";
                AssetDatabase.CreateAsset(setting, settingPath);

                AssetDatabase.Refresh();
            }
            return setting;
        }

        /// <summary>
        /// 尝试创建一个目录
        /// </summary>
        /// <param name="path">要创建的目录路径</param>
        internal static void TryCreateDirectory(string path)
        {
            if(Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        void InstallGeneratorToTargetProject()
        {
            try
            {
                var packageRoot = GetFuiPackagePath();
                const string dllName = "FUI.Generators.dll";
                var dllSourcePath = Path.Combine(packageRoot, "Roslyn~", dllName);
                if (!File.Exists(dllSourcePath))
                {
                    throw new FileNotFoundException("can not find precompiled FUI Generator: " + dllSourcePath);
                }
                    
                //复制 DLL 到目标程序集定义文件夹
                string asmdefPath = setting.GetTargetAssemblyDefinitionPath();
                string targetDir = Path.GetDirectoryName(asmdefPath);
                string dllTargetPath = Path.Combine(targetDir, dllName);

                // 3.0 不保留旧生成器；同时加载两个 Analyzer 会生成重复源码。
                DeleteLegacyGenerator(targetDir);
                File.Copy(dllSourcePath, dllTargetPath, true);

                // 仅在首次安装时创建 meta，更新生成器不能改变已有 GUID。
                string metaPath = dllTargetPath + ".meta";
                if (!File.Exists(metaPath))
                {
                    WriteGeneratorMeta(metaPath);
                }

                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("FUI Generator install success！");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("FUI Generator install failed: " + ex.Message);
            }
        }

        internal static void DeleteLegacyGenerator(string targetDirectory)
        {
            var legacyDll = Path.Combine(targetDirectory, "FUI.SourceGenerator.dll");
            var legacyMeta = legacyDll + ".meta";
            if (File.Exists(legacyDll))
            {
                File.Delete(legacyDll);
            }
            if (File.Exists(legacyMeta))
            {
                File.Delete(legacyMeta);
            }
        }

        string GetFuiPackagePath()
        {
            // 获取所有已加载的包
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in packages)
            {
                if (package.name == "com.fujisheng.fui")
                {
                    return package.resolvedPath;
                }
            }
            throw new System.Exception("can not find package com.fujisheng.fui");
        }

        //生成符合 Unity Generator 要求的 .meta 文件
        internal static void WriteGeneratorMeta(string metaPath)
        {
            string metaContent =
        @"fileFormatVersion: 2
guid: " + System.Guid.NewGuid().ToString("N") + @"
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
            File.WriteAllText(metaPath, metaContent);
        }
    }
}
