
using Mono.Cecil;
using Mono.Cecil.Cil;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor.Compilation;

namespace FUI.Editor
{
	/// <summary>
	/// 内部程序集解析器：自动收集依赖并提供基础解析与特殊映射支持。
	/// </summary>
	internal class CustomAssemblyResolver : BaseAssemblyResolver
	{
		/// <summary>
		/// 已解析的程序集缓存
		/// </summary>
		Dictionary<string, AssemblyDefinition> cache;

		/// <summary>
		///解析使用的引用文件路径集合
		/// </summary>
		List<string> references;

		/// <summary>
		/// 构造函数，基于目标程序集路径收集引用并注册搜索目录。
		/// </summary>
		public CustomAssemblyResolver(string targetAssemblyPath)
		{
			references = BuildReferences(targetAssemblyPath);
			cache = new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);

			for (var i =0; i < references.Count; i++)
			{
				var dir = Path.GetDirectoryName(references[i]);
				if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
				{
                    AddSearchDirectory(dir);
				}
			}
		}

		/// <summary>
		/// 构建引用列表（编译管线优先，最小兜底，HashSet 去重）。
		/// </summary>
		static List<string> BuildReferences(string targetAssemblyPath)
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetPath = Path.GetFullPath(targetAssemblyPath);
            var asm = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).FirstOrDefault(a => string.Equals(Path.GetFullPath(a.outputPath), targetPath, StringComparison.OrdinalIgnoreCase));
			if(asm == null || asm.allReferences == null)
			{
				return new List<string>();
			}

			foreach(var refPath in asm.allReferences)
			{
				if (string.IsNullOrEmpty(refPath) || !File.Exists(refPath))
				{
					continue;
                }
                set.Add(refPath);
            }

            return set.ToList();
		}

		/// <summary>
		///解析目标程序集（带 ReaderParameters）。
		/// </summary>
		public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			if (cache.TryGetValue(name.FullName, out var cached))
			{
				return cached;
			}

			try
			{
                var asm = TryResolveFromReferences(name, parameters);
                if (asm == null)
                {
                    asm = base.Resolve(name, parameters);
                }

                cache[name.FullName] = asm;
                return asm;
            }
			catch
			{
				return null;
			}
		}

		/// <summary>
		///解析目标程序集（不带参数）。
		/// </summary>
		public override AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return Resolve(name, null);
		}

		/// <summary>
		/// 尝试在引用文件中直接匹配并加载。
		/// </summary>
		AssemblyDefinition TryResolveFromReferences(AssemblyNameReference name, ReaderParameters p)
		{
            var fileName = name.Name + ".dll";
            var file = GetReferencePath(fileName);
			if (file == null)
			{
				return null;
			}
			return AssemblyDefinition.ReadAssembly(file, p ?? new ReaderParameters());
		}

		/// <summary>
		/// 获取指定文件名的引用路径。
		/// </summary>
		string GetReferencePath(string fileName)
		{
			for (var i =0; i < references.Count; i++)
			{
				var current = references[i];
				if (string.Equals(Path.GetFileName(current), fileName, StringComparison.OrdinalIgnoreCase))
				{
					return current;
				}
			}
			return null;
		}

		/// <summary>
		///释放缓存中的程序集资源。
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var kv in cache)
				{
					try
					{
						kv.Value?.Dispose();
					}
					catch { }
				}
				cache.Clear();
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// IL 后处理器：为标记 [FUI.Binding] 的自动属性注入统一 setter 调用。
	/// </summary>
	public class ObservableObjectILPostProcessor
	{
		/// <summary>
		///处理并写回指定程序集。
		/// </summary>
		public static void ProcessAssemblyFile(string assemblyPath)
		{
			var readerParameters = new ReaderParameters
			{
				ReadSymbols = false,
				ReadingMode = ReadingMode.Immediate,
				InMemory = true,
				AssemblyResolver = new CustomAssemblyResolver(assemblyPath)
			};

			var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
			if (File.Exists(pdbPath))
			{
				readerParameters.ReadSymbols = true;
				readerParameters.SymbolReaderProvider = new PortablePdbReaderProvider();
			}

            var asmDef = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
            var modified = ProcessAssembly(asmDef);

            if (asmDef == null || !modified)
			{
				asmDef?.Dispose();
				return;
			}

            var writerParameters = new WriterParameters
            {
                WriteSymbols = File.Exists(pdbPath),
                SymbolWriterProvider = new PortablePdbWriterProvider()

            };
            asmDef.Write(assemblyPath, writerParameters);
            asmDef.Dispose();
        }

		/// <summary>
		/// 遍历程序集处理所有目标类型。
		/// </summary>
		static bool ProcessAssembly(AssemblyDefinition assembly)
		{
			var modified = false;
			var count =0;
			foreach (var module in assembly.Modules)
			{
				for (var i =0; i < module.Types.Count; i++)
				{
					var type = module.Types[i];
					if (!IsObservableObjectDerived(type))
					{
						continue;
					}
					if (ProcessType(type))
					{
						modified = true;
					}
					count++;
				}
			}
			UnityEngine.Debug.Log("[ObservableObjectILPostProcessor]处理类型数量: " + count);
			return modified;
		}

		/// <summary>
		///处理单个类型的属性注入。
		/// </summary>
		static bool ProcessType(TypeDefinition type)
		{
			return InstrumentPropertySetters(type);
		}

		/// <summary>
		/// 判断类型是否继承自 ObservableObject 或 ViewModel。
		/// </summary>
		static bool IsObservableObjectDerived(TypeDefinition type)
		{
			var current = (TypeReference)type;
			while (current != null)
			{
				TypeReference baseRef;
				if (current is TypeDefinition td)
				{
					baseRef = td.BaseType;
				}
				else
				{
					baseRef = current.Resolve() != null ? current.Resolve().BaseType : null;
				}
				if (baseRef == null)
				{
					break;
				}
				if (baseRef.Name == "ObservableObject" || baseRef.Name == "ViewModel")
				{
					return true;
				}
				TypeDefinition resolved = null;
				try
				{
					resolved = baseRef.Resolve();
				}
				catch
				{
					break;
				}
				current = resolved;
			}
			return false;
		}

		/// <summary>
		/// 为带 Binding 标记的属性注入 IL。
		/// </summary>
		static bool InstrumentPropertySetters(TypeDefinition type)
		{
			var modified = false;
			for (var i =0; i < type.Properties.Count; i++)
			{
				var prop = type.Properties[i];
				if (prop.SetMethod == null)
				{
					continue;
				}
				if (!HasBindingAttribute(prop))
				{
					continue;
				}
				var backingName = "<" + prop.Name + ">k__BackingField";
				var backing = type.Fields.FirstOrDefault(f => f.Name == backingName);
				if (backing == null)
				{
					continue;
				}
				if (IsSetterAlreadyInstrumented(prop.SetMethod))
				{
					continue;
				}
				try
				{
					RewriteSetterBody(type, prop, backing, prop.SetMethod);
					modified = true;
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError("ObservableObject IL PostProcessor: 属性注入失败: " + type.FullName + "." + prop.Name + "\n" + ex.Message);
				}
			}
			return modified;
		}

		/// <summary>
		/// 判断属性是否标记 FUI.Binding。
		/// </summary>
		static bool HasBindingAttribute(PropertyDefinition prop)
		{
			for (var i =0; i < prop.CustomAttributes.Count; i++)
			{
				var t = prop.CustomAttributes[i].AttributeType;
				if (t == null)
				{
					continue;
				}
				if (t.FullName == "FUI.BindingAttribute" || (t.Name == "BindingAttribute" && t.Namespace == "FUI"))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 判断属性 setter 是否已被注入。
		/// </summary>
		static bool IsSetterAlreadyInstrumented(MethodDefinition setter)
		{
			try
			{
				var list = setter.Body != null ? setter.Body.Instructions : null;
				if (list == null || list.Count ==0)
				{
					return false;
				}
				for (var i =0; i < list.Count; i++)
				{
					var ins = list[i];
					if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference mr)
					{
						if (mr.Name.StartsWith("__Set_", StringComparison.Ordinal))
						{
							return true;
						}
					}
				}
			}
			catch
			{
				// 忽略判断异常，返回未注入
			}
			return false;
		}

		/// <summary>
		/// 重写属性 setter 并保留调试序列点。
		/// </summary>
		static void RewriteSetterBody(TypeDefinition type, PropertyDefinition property, FieldDefinition backingField, MethodDefinition setter)
		{
			var debugInfo = setter.DebugInformation;
			SequencePoint firstSp = null;
			List<SequencePoint> original = null;
			if (debugInfo != null && debugInfo.HasSequencePoints)
			{
				firstSp = debugInfo.SequencePoints[0];
				original = new List<SequencePoint>(debugInfo.SequencePoints);
			}

			setter.Body.Variables.Clear();
			setter.Body.Instructions.Clear();
			setter.Body.ExceptionHandlers.Clear();
			setter.Body.InitLocals = true;
			if (setter.Parameters == null || setter.Parameters.Count ==0)
			{
				setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, property.PropertyType));
			}

			var methodName = "__Set_" + property.Name;
			var setHelper = FindSetHelper(type, property, methodName);
			var il = setter.Body.GetILProcessor();
			var propType = type.Module.ImportReference(property.PropertyType);
			var oldValueVar = new VariableDefinition(propType);
			setter.Body.Variables.Add(oldValueVar);

			Instruction firstInstruction;
			if (backingField != null)
			{
				firstInstruction = il.Create(OpCodes.Ldarg_0);
				il.Append(firstInstruction);
				il.Append(il.Create(OpCodes.Ldfld, backingField));
				il.Append(il.Create(OpCodes.Stloc, oldValueVar));
			}
			else if (property.GetMethod != null)
			{
				firstInstruction = il.Create(OpCodes.Ldarg_0);
				il.Append(firstInstruction);
				il.Append(il.Create(OpCodes.Callvirt, type.Module.ImportReference(property.GetMethod)));
				il.Append(il.Create(OpCodes.Stloc, oldValueVar));
			}
			else
			{
				firstInstruction = il.Create(OpCodes.Nop);
				il.Append(firstInstruction);
				il.Append(il.Create(OpCodes.Ldloca_S, oldValueVar));
				il.Append(il.Create(OpCodes.Initobj, propType));
			}

			if (backingField != null)
			{
				il.Append(il.Create(OpCodes.Ldarg_0));
				il.Append(il.Create(OpCodes.Ldarg_1));
				il.Append(il.Create(OpCodes.Stfld, backingField));
			}
			if (setHelper != null)
			{
				il.Append(il.Create(OpCodes.Ldarg_0));
				il.Append(il.Create(OpCodes.Ldloc, oldValueVar));
				il.Append(il.Create(OpCodes.Ldarg_1));
				il.Append(il.Create(OpCodes.Callvirt, type.Module.ImportReference(setHelper)));
			}

			var ret = il.Create(OpCodes.Ret);
			il.Append(ret);
			MapSequencePoints(debugInfo, firstSp, original, firstInstruction, ret);
		}

		/// <summary>
		/// 在类型及其基类链中查找统一 Set 方法。
		/// </summary>
		static MethodDefinition FindSetHelper(TypeDefinition type, PropertyDefinition property, string methodName)
		{
			MethodDefinition helper = null;
			var current = type;
			var visited = new HashSet<string>(StringComparer.Ordinal);
			while (current != null && helper == null)
			{
				if (!visited.Add(current.FullName))
				{
					break;
				}
				for (var i =0; i < current.Methods.Count; i++)
				{
					var m = current.Methods[i];
					if (m.Name == methodName && m.Parameters.Count ==2 && m.Parameters[0].ParameterType.FullName == property.PropertyType.FullName && m.Parameters[1].ParameterType.FullName == property.PropertyType.FullName)
					{
						helper = m;
						break;
					}
				}
				if (helper != null)
				{
					break;
				}
				TypeDefinition next = null;
				try
				{
					next = current.BaseType != null ? current.BaseType.Resolve() : null;
				}
				catch
				{
					break;
				}
				current = next;
			}
			return helper;
		}

		/// <summary>
		/// 映射调试序列点，尽量保留源代码行信息。
		/// </summary>
		static void MapSequencePoints(MethodDebugInformation debugInfo, SequencePoint firstSp, List<SequencePoint> original, Instruction firstInstruction, Instruction ret)
		{
			if (debugInfo == null || firstSp == null || firstSp.Document == null)
			{
				return;
			}
			debugInfo.SequencePoints.Clear();
			debugInfo.SequencePoints.Add(new SequencePoint(firstInstruction, firstSp.Document)
			{
				StartLine = firstSp.StartLine,
				StartColumn = firstSp.StartColumn,
				EndLine = firstSp.EndLine,
				EndColumn = firstSp.EndColumn
			});
			if (original != null && original.Count >1)
			{
				for (var i =1; i < original.Count; i++)
				{
					var osp = original[i];
					if (osp == null || osp.Document == null)
					{
						continue;
					}
					debugInfo.SequencePoints.Add(new SequencePoint(ret, osp.Document)
					{
						StartLine = osp.StartLine,
						StartColumn = osp.StartColumn,
						EndLine = osp.EndLine,
						EndColumn = osp.EndColumn
					});
				}
			}
			debugInfo.SequencePoints.Add(new SequencePoint(ret, firstSp.Document)
			{
				StartLine = firstSp.EndLine,
				StartColumn = firstSp.EndColumn,
				EndLine = firstSp.EndLine,
				EndColumn = firstSp.EndColumn
			});
		}
	}
}