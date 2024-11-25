using System.IO;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;

namespace FUI.Editor
{
    public class InstallerWindow : EditorWindow
    {
        const string dataPath = "./FUI";
        const string installPath = "./FUI/Compiler";
        const string compilerUrl = "";
        const string settingPath = "Assets/Editor Default Resources/FUI/Settings/CompilerSetting.asset";

        string installedVersion;
        string latestVersion;

        CompilerSetting setting;

        [MenuItem("FUI/Installer")]
        public static void ShowWindow()
        {
            var window = GetWindowWithRect<InstallerWindow>(new Rect(Vector2.zero, new Vector2(1080, 720)));
            window.titleContent = new GUIContent("FUI Installer");

            window.installedVersion = window.GetInstalledVersion();
            window.setting = window.LoadOrCreateSetting();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                var versionDes = string.IsNullOrEmpty(latestVersion) 
                    ? $"InstalledVersion:{installedVersion}"
                    : $"InstalledVersion:{installedVersion}(LatestVersion:{latestVersion})";
                EditorGUILayout.LabelField(versionDes);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("CheckUpdate"))
                    {
                        latestVersion = GetLatestVersion();
                    }

                    var installOrUpdate = string.IsNullOrEmpty(installedVersion) ? "Install" : "Update";
                    if (GUILayout.Button(installOrUpdate))
                    {
                        //TODO: Install
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(installedVersion))
                {
                    setting.solutionPath = EditorGUILayout.TextField("SolutionPath", setting.solutionPath);
                    setting.targetProject = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField("TargetProject", setting.targetProject, typeof(AssemblyDefinitionAsset), false);
                    setting.generatedPath = EditorGUILayout.TextField("GeneratedPath", setting.generatedPath);
                    setting.output = EditorGUILayout.TextField("Output", setting.output);
                    setting.generateType =(BindingContextGenerateType) EditorGUILayout.EnumPopup("GenerateType", setting.generateType);
                }
            }
            EditorGUILayout.EndVertical();

        }

        string GetInstalledVersion()
        {
            return "0.0.1";
        }

        string GetLatestVersion()
        {
            return "0.0.2";
        }

        void Install()
        {
        }

        CompilerSetting LoadOrCreateSetting() 
        {
            var directory = Path.GetDirectoryName(settingPath);
            TryCreateDirectory(directory);
            var setting = AssetDatabase.LoadAssetAtPath<CompilerSetting>(settingPath);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<CompilerSetting>();
                setting.solutionPath = GetDefaultSolutionPath();
                setting.generatedPath = $"{dataPath}/Generated";
                setting.output = "./Library/ScriptAssemblies";
                AssetDatabase.CreateAsset(setting, settingPath);

                AssetDatabase.Refresh();
            }
            return setting;
        }

        string GetDefaultSolutionPath()
        {
            var files = Directory.GetFiles("./", "*.sln", SearchOption.TopDirectoryOnly);
            if(files.Length == 0)
            {
                return string.Empty;
            }

            return files[0];
        }

        void TryCreateDirectory(string path)
        {
            if(Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }
    }
}