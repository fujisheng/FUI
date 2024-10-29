using System.Diagnostics;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

namespace FUI.Editor
{
    /// <summary>
    /// TODO  暂时这样处理，后面完全替代掉unity编译
    /// </summary>
    [InitializeOnLoad]
    public class InjectEditor : UnityEditor.Editor
    {
        static InjectEditor()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinishedCallback;
        }

        static void AssemblyCompilationFinishedCallback(string file, UnityEditor.Compilation.CompilerMessage[] messages)
        {
            if (!file.EndsWith("FUI.Test.dll"))
            {
                return;
            }

            CompileUIProject();
        }

        [MenuItem("FUI/CompileUIProject")]
        public static void CompileUIProject()
        {
            UnityEngine.Debug.Log($"<color=green>FUICompiler Start</color>");
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(Application.dataPath, "../Compiler/Release/net8.0/win-x64/FUICompiler.exe");
            process.StartInfo.ArgumentList.Add("--sln=.\\FUI.sln");
            process.StartInfo.ArgumentList.Add("--project=FUI.Test");
            process.StartInfo.ArgumentList.Add("--output=.\\Library\\ScriptAssemblies\\FUI.Test.dll");
            process.StartInfo.ArgumentList.Add("--binding=.\\Binding\\");
            process.StartInfo.ArgumentList.Add("--generated=.\\Temp\\BindingGenerated\\");
            process.StartInfo.ArgumentList.Add("--ctx_type=Attribute");

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
    }
}
