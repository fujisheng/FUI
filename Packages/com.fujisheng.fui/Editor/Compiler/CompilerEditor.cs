using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// 当目标项目编译完成时 的所有回调
        /// </summary>
        static List<Action<string, List<object>>> onCompilationComplate;

        static CompilerEditor()
        {
            Initialize();
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinishedCallback;
        }

        static void Initialize()
        {
            //找到所有的编译成功回调
            onCompilationComplate = new List<Action<string, List<object>>>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach(var method in methods)
                    {
                        if (method.GetCustomAttribute<AssemblyCompilationFinishedAttribute>() != null)
                        {
                            var callback = (Action<string, List<object>>)method.CreateDelegate(typeof(Action<string, List<object>>));
                            onCompilationComplate.Add(callback);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 执行所有的编译完成回调
        /// </summary>
        /// <param name="file">编译完成的dll</param>
        /// <param name="message">消息</param>
        static void InvokeCompilationFinishedCallback(string file, List<object> message)
        {
            foreach(var action in onCompilationComplate)
            {
                action?.Invoke(file, message);
            }
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
            process.StartInfo.ArgumentList.Add($"--binding_output={setting.bindingInfoOutputPath}");

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("GBK");

            process.Start();

            var messages = new List<object>();
            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if(line == null)
                {
                    break;
                }

                messages.Add(line);
                ProcessMessage(line);
            }

            var file = $"{setting.output}/{setting.GetTargetProjectName()}.dll";
            InvokeCompilationFinishedCallback(file, messages);
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
