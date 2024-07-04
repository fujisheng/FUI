using System.IO;

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
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinishedCallback;
        }

        static void AssemblyCompilationFinishedCallback(string file, CompilerMessage[] messages)
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
            var batPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../CompileUIProject.bat"));
            BatHelper.RunBat(batPath, (hasError) => { UnityEngine.Debug.Log($"注入绑定{(hasError ? "失败" : "成功")}"); });
        }
    }
}
