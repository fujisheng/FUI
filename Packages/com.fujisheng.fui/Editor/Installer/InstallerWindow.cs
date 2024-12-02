using System.IO;
using System.IO.Compression;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;
using UnityEngine.Networking;

namespace FUI.Editor
{
    public class InstallerWindow : EditorWindow
    {
        const string dataPath = "./FUI";
        const string installPath = "./FUI/Compiler";
        const string compilerUrl = "https://github.com/fujisheng/FUICompiler/releases/download/0.0.2.a/FUICompiler.zip";
        const string settingPath = "Assets/Editor Default Resources/FUI/Settings/CompilerSetting.asset";

        string installedVersion;
        string latestVersion;

        CompilerSetting setting;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UIEntitites.Instance.GetEntity(null);
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
            installedVersion = GetInstalledVersion();
            setting = LoadOrCreateSetting();
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
                        DownloadCompiler(installPath);
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
                    setting.bindingInfoOutputPath = EditorGUILayout.TextField("BindingInfoOutputPath", setting.bindingInfoOutputPath);
                    if(GUILayout.Button("Save Settings"))
                    {
                        EditorUtility.SetDirty(setting);
                        AssetDatabase.SaveAssets();
                        UnityEngine.Debug.Log("Save FUI Compiler Setting Success");
                    }
                }
            }
            EditorGUILayout.EndVertical();

        }

        async void DownloadCompiler(string downloadPath)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(compilerUrl))
            {
                UnityEngine.Debug.Log($"start download compiler...");
                await webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.LogError("download compiler error" + webRequest.error);
                }
                else
                {
                    Directory.Delete(downloadPath, true );
                    TryCreateDirectory(downloadPath);

                    using(var stream = new MemoryStream(webRequest.downloadHandler.data))
                    using(var zipFile = new ZipArchive(stream))
                    {
                        zipFile.ExtractToDirectory(downloadPath);
                    }
                    UnityEngine.Debug.Log("download sucess");
                    setting.compilerPath = $"{downloadPath}/FUICompiler/FUICompiler.exe";
                }
            }
        }

        string GetInstalledVersion()
        {
            return "0.0.1";
        }

        string GetLatestVersion()
        {
            return "0.0.2";
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
                setting.compilerPath = $"{installPath}/FUICompiler/FUICompiler.exe";
                setting.bindingInfoOutputPath = $"{dataPath}/BindingInfo";
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

        /// <summary>
        /// Inject to UnityEditor.Scripting.ScriptCompilation 671 hook start event
        /// </summary>
        void InjectCompilationStarted()
        {

        }
    }
}