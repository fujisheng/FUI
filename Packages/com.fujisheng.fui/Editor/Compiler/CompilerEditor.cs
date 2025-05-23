﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;

using UnityEngine;

namespace FUI.Editor
{
    [InitializeOnLoad]
    public class CompilerEditor : UnityEditor.Editor, IPostBuildPlayerScriptDLLs
    {
        static CompilerSetting setting;

        /// <summary>
        /// 当目标项目编译完成时 的所有回调
        /// </summary>
        static List<Action<string, List<object>>> onCompilationComplate;

        static CompilerEditor()
        {
            Initialize();
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;
        }

        static void OnAssemblyCompilationStarted(string assemblyPath)
        {
            if (setting == null && !TryLoadSetting(out setting))
            {
                UnityEngine.Debug.LogError("InjectSetting not found");
                return;
            }

            if (!assemblyPath.EndsWith($"{setting.targetProject.name}.dll"))
            {
                return;
            }

            // 终止Unity默认编译流程
            CompilationPipeline.RequestScriptCompilation();
            // 触发自定义编译器
            Compile();
        }

        static void Initialize()
        {
            //找到所有的编译成功回调
            onCompilationComplate = new List<Action<string, List<object>>>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var method in methods)
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
            foreach (var action in onCompilationComplate)
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

        [MenuItem("FUI/Compile")]
        public static void Compile()
        {
            if (setting == null && !TryLoadSetting(out setting))
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
            process.StartInfo.ArgumentList.Add($"--ctx_type={(int)setting.generateType}");
            process.StartInfo.ArgumentList.Add($"--binding_output={setting.bindingInfoOutputPath}");
            process.StartInfo.ArgumentList.Add($"--modified_output={setting.modifiedObservableObjectPath}");

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("GBK");

            process.Start();

            var messages = new List<object>();
            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if (line == null)
                {
                    break;
                }

                messages.Add(line);
                ProcessMessage(line);
            }

            var file = $"{setting.output}/{setting.GetTargetProjectName()}.dll";

            UnityEngine.Debug.Log($"{setting.targetProject.name}.dll compilation completed at {file}");
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
            
            InvokeCompilationFinishedCallback(file, messages);
        }

        static void ProcessMessage(string message)
        {
            var msg = Message.ReadMessage(message);
            if (msg == null)
            {
                UnityEngine.Debug.Log(message);
                return;
            }

            if (msg is LogMessage log)
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

            if (msg is CompilerMessage compiler)
            {
                UnityEngine.Debug.LogError(compiler.Error);
            }
        }

        public int callbackOrder => 0;

        /// <summary>
        /// 打包时替换unity编译的dll为自定义编译的dll
        /// </summary>
        /// <param name="report"></param>
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            TryLoadSetting(out setting);
            Compile();

            var customDllPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
            var dllName = Path.GetFileName(customDllPath);

            var buildTarget = report.summary.platform;

            var projectPath = Application.dataPath.Replace("/Assets", "");
            var stagingManagedDir = Path.Combine(projectPath, "Temp", "StagingArea", "Managed");

            if (report.summary.platform == BuildTarget.Android)
            {
                stagingManagedDir = Path.Combine(projectPath, "Temp", "StagingArea", "Data", "Managed");
            }

            if (!Directory.Exists(stagingManagedDir))
            {
                UnityEngine.Debug.LogError("目录不存在，请检查路径！");
                return;
            }

            var targetPath = Path.Combine(stagingManagedDir, dllName);

            if (File.Exists(customDllPath))
            {
                File.Copy(customDllPath, targetPath, overwrite: true);
                UnityEngine.Debug.Log($"DLL替换成功！目标路径: {targetPath}");
            }
            else
            {
                UnityEngine.Debug.LogError($"DLL不存在于: {customDllPath}");
            }
        }
    }
}

