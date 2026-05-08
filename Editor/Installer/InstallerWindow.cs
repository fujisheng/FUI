using System.IO;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;

namespace FUI.Editor
{
    /// <summary>
    /// 编译器安装窗口
    /// </summary>
    public class InstallerWindow : EditorWindow
    {
        const string dataPath = "./FUI";
        const string settingPath = "Assets/Editor Default Resources/FUI/Settings/ILPostProcessesSetting.asset";

        ILPostProcessesSetting setting;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UIEntityRegistry.GetEntity(null);
            BindingContexts.Instance.GetContextInfo(null);
        }

        [MenuItem("FUI/Installer")]
        public static void ShowWindow()
        {
            var window = GetWindowWithRect<InstallerWindow>(new Rect(Vector2.zero, new Vector2(1080, 720)));
            window.titleContent = new GUIContent("FUI Installer");
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
                    UnityEngine.Debug.Log("Save FUI ILPostProcesses Setting Success");
                }

                if (GUILayout.Button("Install"))
                {
                    InstallSourceGeneratorToTargetProject();
                }
            }
            EditorGUILayout.EndVertical();

        }

        /// <summary>
        /// 创建或者加载一个编译器设置
        /// </summary>
        /// <returns></returns>
        ILPostProcessesSetting LoadOrCreateSetting() 
        {
            var directory = Path.GetDirectoryName(settingPath);
            TryCreateDirectory(directory);
            var setting = AssetDatabase.LoadAssetAtPath<ILPostProcessesSetting>(settingPath);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<ILPostProcessesSetting>();
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
        void TryCreateDirectory(string path)
        {
            if(Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        void InstallSourceGeneratorToTargetProject()
        {
            try
            {
                var packageRoot = GetFuiPackagePath();
                var sourceGeneratorDir = Path.Combine(packageRoot, "FUI.SourceGenerator~");
                var solutionPath = Path.Combine(sourceGeneratorDir, "FUI.SourceGenerator.sln");

                if (!File.Exists(solutionPath))
                {
                    throw new FileNotFoundException("can not find FUI.SourceGenerator.sln: " + solutionPath);
                }
                    
                //dotnet build
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"build \"{solutionPath}\" -c Release";
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(solutionPath);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new System.Exception("dotnet build failed: " + error);
                }
                    
                //查找输出 DLL
                string dllName = "FUI.SourceGenerator.dll";
                string dllSourcePath = Path.Combine(
                    Path.GetDirectoryName(solutionPath),
                    "bin",
                    "Release",
                    "netstandard2.0",
                    dllName
                );
                if (!File.Exists(dllSourcePath))
                {
                    throw new FileNotFoundException("can not find DLL: " + dllSourcePath);
                }
                    
                //复制 DLL 到目标程序集定义文件夹
                string asmdefPath = setting.GetTargetAssemblyDefinitionPath();
                string targetDir = Path.GetDirectoryName(asmdefPath);
                string dllTargetPath = Path.Combine(targetDir, dllName);
                File.Copy(dllSourcePath, dllTargetPath, true);

                // 5. 生成/修正 .meta 文件
                string metaPath = dllTargetPath + ".meta";
                WriteSourceGeneratorMeta(metaPath);

                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("FUI SourceGenerator install success！");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("FUI SourceGenerator install failed: " + ex.Message);
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

        //生成符合 Unity SourceGenerator 要求的 .meta 文件
        void WriteSourceGeneratorMeta(string metaPath)
        {
            string metaContent =
        @"fileFormatVersion: 2
guid: " + System.Guid.NewGuid().ToString("N") + @"
labels:
- RoslynAnalyzer
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isExplicitlyReferenced: 1
  validateReferences: 0
  platformData:
  - first:
      '': Any
    second:
      enabled: 1
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
  sourceGenerator: 1
";
            File.WriteAllText(metaPath, metaContent);
        }
    }
}
