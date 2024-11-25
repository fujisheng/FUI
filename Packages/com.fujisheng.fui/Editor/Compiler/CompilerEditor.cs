using System.Diagnostics;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

namespace FUI.Editor
{
    [InitializeOnLoad]
    public class CompilerEditor : UnityEditor.Editor
    {
        static CompilerSetting setting;

        static CompilerEditor()
        {
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinishedCallback;
        }

        /// <summary>
        /// 尝试加载编译器设置
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        static bool TryLoadSetting(out CompilerSetting setting)
        {
            setting = AssetDatabase.LoadAssetAtPath<CompilerSetting>("Assets/Editor Default Resources/FUI/Settings/CompilerSetting.asset");
            return setting != null;
        }

        static void AssemblyCompilationFinishedCallback(string file, UnityEditor.Compilation.CompilerMessage[] messages)
        {
            if(setting == null && !TryLoadSetting(out setting))
            {
                UnityEngine.Debug.LogError("InjectSetting not found");
                return;
            }

            if (!file.EndsWith($"{setting.targetProject.name}.dll"))
            {
                return;
            }

            Compile();
        }

        [MenuItem("FUI/Compile")]
        public static void Compile()
        {
            if(setting == null && !TryLoadSetting(out setting))
            {
                UnityEngine.Debug.LogError("InjectSetting not found");
                return;
            }

            UnityEngine.Debug.Log($"<color=green>FUICompiler Start</color>");
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(Application.dataPath, $"../{setting.compilerPath}");
            process.StartInfo.ArgumentList.Add($"--sln={setting.solutionPath}");
            process.StartInfo.ArgumentList.Add($"--project={setting.GetTargetProjectName()}");
            process.StartInfo.ArgumentList.Add($"--output={setting.output}");
            process.StartInfo.ArgumentList.Add("--binding=.\\Binding\\");
            process.StartInfo.ArgumentList.Add($"--generated={setting.generatedPath}");
            process.StartInfo.ArgumentList.Add($"--ctx_type={setting.generateType.ToString()}");

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("GBK");

            process.Start();

            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if(line == null)
                {
                    break;
                }
                ProcessMessage(line);
            }
        }

        static void ProcessMessage(string message)
        {
            var msg = Message.ReadMessage(message);
            if(msg is LogMessage log)
            {
                switch (log.Level)
                {
                    case LogLevel.Info:
                        UnityEngine.Debug.Log($"<color=green>{log.Message}</color>");
                        break;
                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(log.Message);
                        break;
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(log.Message);
                        break;
                }
            }

            if(msg is CompilerMessage compiler)
            {
                UnityEngine.Debug.LogError(compiler.Error);
            }
        }

        static void OnCompilationStarted(object o)
        {
            UnityEngine.Debug.Log(o.GetType().FullName);
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        static bool aaa(int instance, int line)
        {
            UnityEngine.Debug.Log($"OnOpenAsset   instance:{instance}  line:{line}");
            return false;
        }
    }
}
