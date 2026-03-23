using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;

using UnityEngine;

namespace FUI.Editor
{
	[InitializeOnLoad]
	public class ILPostProcessesEditor : UnityEditor.Editor, IPostBuildPlayerScriptDLLs
	{
		static ILPostProcessesSetting setting;

		/// <summary>
		/// 当目标项目编译完成时 的所有回调
		/// </summary>
		static List<Action<string, List<object>>> onCompilationComplate;

		static ILPostProcessesEditor()
		{
			Initialize();
			CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
		}

		static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
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

			ObservableObjectILPostProcessor.ProcessAssemblyFile(assemblyPath);
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
		/// 手动注入目标 DLL
		/// </summary>
		[MenuItem("FUI/ILPostProcessos")]
		public static void ManualInject()
		{
			if (!TryLoadSetting(out var setting) || setting == null)
			{
				UnityEngine.Debug.LogError("未找到 CompilerSetting，无法注入。");
				return;
			}

			var dllPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
			if (!File.Exists(dllPath))
			{
				UnityEngine.Debug.LogError($"目标 DLL 不存在: {dllPath}");
				return;
			}

			ObservableObjectILPostProcessor.ProcessAssemblyFile(dllPath);
			UnityEngine.Debug.Log($"手动注入完成: {dllPath}");
		}

		/// <summary>
		/// 执行所有的编译完成回调
		/// </summary>
		/// <param name="file">编译完成的dll</param>
		/// <param name="message">消息</param>
		static void InvokeCompilationFinishedCallback(string file, List<object> message)
		{
			for (var i =0; i < onCompilationComplate.Count; i++)
			{
				var action = onCompilationComplate[i];
				action?.Invoke(file, message);
			}
		}

		/// <summary>
		/// 尝试加载编译器设置
		/// </summary>
		/// <param name="setting"></param>
		/// <returns></returns>
		static bool TryLoadSetting(out ILPostProcessesSetting setting)
		{
			setting = AssetDatabase.LoadAssetAtPath<ILPostProcessesSetting>("Assets/Editor Default Resources/FUI/Settings/CompilerSetting.asset");
			return setting != null;
		}

		public int callbackOrder { get { return 0; } }

		/// <summary>
		/// 打包时为unity编译的dll注入Set调用（通过 BuildReport 定位 Managed目录，避免硬编码临时路径）。
		/// </summary>
		/// <param name="report">构建报告</param>
		public void OnPostBuildPlayerScriptDLLs(BuildReport report)
		{
			TryLoadSetting(out setting);

			var customDllPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
			var dllName = Path.GetFileName(customDllPath);

			string managedDir;
			if (!TryGetManagedDirectoryFromReport(report, dllName, out managedDir))
			{
				UnityEngine.Debug.LogError("未能从 BuildReport 定位到 Managed目录，请检查构建报告。");
				return;
			}

			var targetPath = Path.Combine(managedDir, dllName);
			if (File.Exists(targetPath))
			{
				ObservableObjectILPostProcessor.ProcessAssemblyFile(targetPath);
				UnityEngine.Debug.Log($"DLL注入完成，目标路径: {targetPath}");
			}
			else
			{
				UnityEngine.Debug.LogError($"在定位的 Managed目录下未找到目标 DLL: {targetPath}");
			}
		}

		/// <summary>
		/// 从 BuildReport 中查找 Player 的 Managed目录（优先匹配目标 dll，再退化到 Assembly-CSharp.dll，再到任意 Managed）。
		/// </summary>
		/// <param name="report">构建报告</param>
		/// <param name="dllName">目标 dll 名称</param>
		/// <param name="managedDir">输出的 Managed目录</param>
		/// <returns>是否成功定位</returns>
		static bool TryGetManagedDirectoryFromReport(
			BuildReport report,
			string dllName,
			out string managedDir)
		{
			managedDir = null;
			if (report == null)
			{
				return false;
			}

			IList<BuildFile> files = null;
			try
			{
				files = report.GetFiles();
			}
			catch
			{
				files = null;
			}

			if (files == null || files.Count ==0)
			{
				return false;
			}

			//1) 精确匹配目标 dll 所在目录
			for (var i =0; i < files.Count; i++)
			{
				var path = files[i].path;
				if (string.IsNullOrEmpty(path))
				{
					continue;
				}
				if (string.Equals(Path.GetFileName(path), dllName, StringComparison.OrdinalIgnoreCase))
				{
					managedDir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(managedDir))
					{
						return true;
					}
				}
			}

			//2)退化：寻找 Assembly-CSharp.dll 所在目录
			for (var i =0; i < files.Count; i++)
			{
				var path = files[i].path;
				if (string.IsNullOrEmpty(path))
				{
					continue;
				}
				if (string.Equals(Path.GetFileName(path), "Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase))
				{
					managedDir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(managedDir))
					{
						return true;
					}
				}
			}

			//3) 再退化：任意包含 Managed目录的文件路径
			for (var i =0; i < files.Count; i++)
			{
				var path = files[i].path;
				if (string.IsNullOrEmpty(path))
				{
					continue;
				}
				var dir = Path.GetDirectoryName(path);
				if (string.IsNullOrEmpty(dir))
				{
					continue;
				}
				var dirNorm = dir.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
				var token = Path.DirectorySeparatorChar + "Managed" + Path.DirectorySeparatorChar;
				if (dirNorm.IndexOf(token, StringComparison.OrdinalIgnoreCase) >=0 || dirNorm.EndsWith(Path.DirectorySeparatorChar + "Managed", StringComparison.OrdinalIgnoreCase))
				{
					managedDir = dir;
					return true;
				}
			}

			return false;
		}
	}
}

