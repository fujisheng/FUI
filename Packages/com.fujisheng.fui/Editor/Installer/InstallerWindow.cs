using System.IO;
using System.IO.Compression;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;
using UnityEngine.Networking;

namespace FUI.Editor
{
    /// <summary>
    /// ��������װ����
    /// </summary>
    public class InstallerWindow : EditorWindow
    {
        const string dataPath = "./FUI";
        const string installPath = "./FUI/Compiler";
        const string compilerReleasedUrl = "https://api.github.com/repos/fujisheng/FUICompiler/releases/latest";
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
            GetLatestVersion();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                //û�а�װ������
                if (string.IsNullOrEmpty(installedVersion))
                {
                    EditorGUILayout.LabelField($"Not Find FUICompiler, Please Install First (Lastest:{latestVersion})");
                    if (GUILayout.Button("Install"))
                    {
                        DownloadCompiler(latestVersion);
                    }
                }
                //�Ѱ�װ���������������°汾
                else if (!string.IsNullOrEmpty(installedVersion) && installedVersion != latestVersion)
                {
                    EditorGUILayout.LabelField($"Installed Version:{installedVersion}  (Latest:{latestVersion})");
                    if (GUILayout.Button("Update"))
                    {
                        DownloadCompiler(latestVersion);
                    }
                }
                //�Ѱ�װ�����°汾
                else
                {
                    EditorGUILayout.LabelField($"FUICompiler Installed Is Latest:{latestVersion}");
                }

                //��װ�˱����� ����ʾ����
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

        /// <summary>
        /// ���ض�Ӧ�汾�ı�����
        /// </summary>
        /// <param name="version">Ҫ���صİ汾</param>
        async void DownloadCompiler(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                EditorUtility.DisplayDialog("Download FUICompiler Error", "can not find latest version, please check network connection...", "OK");
                return;
            }

            var downloadUrl = $"https://github.com/fujisheng/FUICompiler/releases/download/{version}/FUICompiler-{version}-{Utility.GetPlatformInfo()}.zip";

            var downloadPath = $"{installPath}/{Utility.GetPlatformInfo()}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(downloadUrl))
            {
                var request = webRequest.SendWebRequest();
                while(true)
                {
                    if(request.isDone || request.webRequest.result != UnityWebRequest.Result.InProgress)
                    {
                        EditorUtility.ClearProgressBar();
                        break;
                    }

                    await System.Threading.Tasks.Task.Delay(1);
                    EditorUtility.DisplayProgressBar("Download FUICompiler", "Downloading...", request.progress);
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("Download FUICompiler Error", webRequest.error, "OK");
                }
                else
                {
                    if (Directory.Exists(downloadPath))
                    {
                        Directory.Delete(downloadPath, true);
                    }
                    
                    TryCreateDirectory(downloadPath);

                    using(var stream = new MemoryStream(webRequest.downloadHandler.data))
                    using(var zipFile = new ZipArchive(stream))
                    {
                        zipFile.ExtractToDirectory(downloadPath);
                    }

                    setting.compilerPath = $"{downloadPath}/FUICompiler.exe";
                    installedVersion = version;
                    SaveVersion(version);
                    EditorUtility.SetDirty(setting);
                    AssetDatabase.SaveAssets();

                    ShowNotification(new GUIContent("Download FUICompiler Sucess"));
                }
            }
        }

        /// <summary>
        /// ���氲װ�İ汾��Ϣ
        /// </summary>
        /// <param name="version"></param>
        void SaveVersion(string version)
        {
            var versionPath = $"{installPath}/{Utility.GetPlatformInfo()}/version.txt";
            File.WriteAllBytes(versionPath, System.Text.Encoding.UTF8.GetBytes(version));
        }

        /// <summary>
        /// ��ȡ�Ѱ�װ�İ汾
        /// </summary>
        /// <returns></returns>
        string GetInstalledVersion()
        {
            var versionPath = $"{installPath}/{Utility.GetPlatformInfo()}/version.txt";
            if (!File.Exists(versionPath))
            {
                return string.Empty;
            }
            return System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(versionPath));
        }

        class ReleaseInfo
        {
            public string tag_name;
        }

        /// <summary>
        /// ��ȡ���°汾
        /// </summary>
        async void GetLatestVersion()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(compilerReleasedUrl))
            {
                await webRequest.SendWebRequest();
                if(webRequest.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.LogError("GetLatestVersion Error" + webRequest.error);
                    this.latestVersion = string.Empty;
                    return;
                }
                
                this.latestVersion = JsonUtility.FromJson<ReleaseInfo>(webRequest.downloadHandler.text).tag_name;
                UnityEngine.Debug.Log($"the latest FUICompiler version is {this.latestVersion}");
            }
        }

        /// <summary>
        /// �������߼���һ������������
        /// </summary>
        /// <returns></returns>
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
                setting.compilerPath = $"{installPath}/{Utility.GetPlatformInfo()}/FUICompiler.exe";
                setting.bindingInfoOutputPath = $"{dataPath}/BindingInfo";
                AssetDatabase.CreateAsset(setting, settingPath);

                AssetDatabase.Refresh();
            }
            return setting;
        }

        /// <summary>
        /// ��ȡĬ�Ͻ������·��
        /// </summary>
        /// <returns></returns>
        string GetDefaultSolutionPath()
        {
            var files = Directory.GetFiles("./", "*.sln", SearchOption.TopDirectoryOnly);
            if(files.Length == 0)
            {
                return string.Empty;
            }

            return files[0];
        }

        /// <summary>
        /// ���Դ���һ��Ŀ¼
        /// </summary>
        /// <param name="path">Ҫ������Ŀ¼·��</param>
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