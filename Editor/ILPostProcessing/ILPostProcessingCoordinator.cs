using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FUI.Editor.Installation;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;

using UnityEngine;

namespace FUI.Editor.ILPostProcessing
{
	/// <summary>
	/// 协调程序集编译与玩家构建后的可观察属性 IL 注入，并负责触发框架编译回调。
	/// </summary>
	[InitializeOnLoad]
	internal class ILPostProcessingCoordinator : UnityEditor.Editor, IPostBuildPlayerScriptDLLs
	{
		static ILPostProcessingSettings setting;

		/// <summary>
		/// 当目标项目编译完成时 的所有回调
		/// </summary>
		static List<Action<string, List<object>>> onCompilationComplate;

		static ILPostProcessingCoordinator()
		{
			Initialize();
			CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
			EditorApplication.delayCall += EnsureCurrentTargetProcessed;
		}

		static void EnsureCurrentTargetProcessed()
		{
			if (!TryLoadSetting(out setting) || !HasValidTarget(setting))
			{
				return;
			}

			var assemblyPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
			if (!File.Exists(assemblyPath))
			{
				return;
			}

			try
			{
				if (ObservableObjectILPostProcessor.ProcessAssemblyFile(assemblyPath))
				{
					// 包或 ILPP 自身升级时，目标程序集可能已由旧处理器编译；补写后重新加载一次。
					EditorUtility.RequestScriptReload();
				}
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogError($"FUI ILPP automatic check failed: {exception}");
			}
		}

		static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
		{
			if ((setting == null && !TryLoadSetting(out setting)) || !HasValidTarget(setting))
			{
				return;
			}

			if (!assemblyPath.EndsWith($"{setting.GetTargetProjectName()}.dll", StringComparison.OrdinalIgnoreCase))
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
				var assemblyName = assembly.GetName().Name;
				if (assemblyName.EndsWith(".Generators") || assemblyName.Contains("SourceGenerator") || assemblyName.Contains("Analyzer"))
				{
					continue;
				}

				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException exception)
				{
					types = Array.FindAll(exception.Types, type => type != null);
				}

				foreach (var type in types)
				{
					MethodInfo[] methods;
					try
					{
						methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					}
					catch (Exception)
					{
						continue;
					}

					foreach (var method in methods)
					{
						try
						{
							if (method.GetCustomAttribute<AssemblyCompilationFinishedAttribute>() == null)
							{
								continue;
							}

							var callback = (Action<string, List<object>>)method.CreateDelegate(typeof(Action<string, List<object>>));
							onCompilationComplate.Add(callback);
						}
						catch (Exception)
						{
							// 跳过依赖未加载的编辑器扩展方法。
							continue;
						}
					}
				}
			}
		}

		/// <summary>
		/// 手动注入目标 DLL
		/// </summary>
		[MenuItem("Tools/FUI/Run IL Post Processing")]
		public static void ManualInject()
		{
			if (!TryLoadSetting(out var setting) || !HasValidTarget(setting))
			{
				UnityEngine.Debug.LogError("FUI ILPP settings are missing or the target assembly is invalid.");
				return;
			}

			var dllPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
			if (!File.Exists(dllPath))
			{
				UnityEngine.Debug.LogError($"Target DLL not found: {dllPath}");
				return;
			}

			ObservableObjectILPostProcessor.ProcessAssemblyFile(dllPath);
			UnityEngine.Debug.Log($"Manual IL post-processing completed: {dllPath}");
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
		static bool TryLoadSetting(out ILPostProcessingSettings setting)
		{
			setting = AssetDatabase.LoadAssetAtPath<ILPostProcessingSettings>("Assets/Editor Default Resources/FUI/Settings/ILPostProcessingSettings.asset");
			return setting != null;
		}

		static bool HasValidTarget(ILPostProcessingSettings value)
		{
			return value != null
				&& value.targetProject != null
				&& !string.IsNullOrWhiteSpace(value.output);
		}

		public int callbackOrder { get { return 0; } }

		/// <summary>
		/// 打包时为unity编译的dll注入Set调用（通过 BuildReport 定位 Managed目录，避免硬编码临时路径）。
		/// </summary>
		/// <param name="report">构建报告</param>
		public void OnPostBuildPlayerScriptDLLs(BuildReport report)
		{
			if (!TryLoadSetting(out setting) || !HasValidTarget(setting))
			{
				return;
			}

			var customDllPath = Path.Combine(setting.output, setting.GetTargetProjectName() + ".dll");
			var dllName = Path.GetFileName(customDllPath);

			string managedDir;
			if (!TryGetManagedDirectoryFromReport(report, dllName, out managedDir))
			{
				UnityEngine.Debug.LogError("Unable to locate the Managed directory from the BuildReport.");
				return;
			}

			var targetPath = Path.Combine(managedDir, dllName);
			if (File.Exists(targetPath))
			{
				ObservableObjectILPostProcessor.ProcessAssemblyFile(targetPath);
				UnityEngine.Debug.Log($"IL post-processing completed: {targetPath}");
			}
			else
			{
				UnityEngine.Debug.LogError($"Target DLL was not found in the Managed directory: {targetPath}");
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
