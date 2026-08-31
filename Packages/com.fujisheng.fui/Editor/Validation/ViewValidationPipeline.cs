using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FUI.Editor.Validation
{
    /// <summary>
    /// 在 Prefab 导入和玩家构建前运行 ViewValidator，并将验证问题写入 Console。
    /// </summary>
    internal sealed class ViewValidationPipeline : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var issues = ViewValidator.ValidateAll();
            Report(issues);
            var errorCount = issues.Count(issue => issue.Severity == ValidationSeverity.Error);
            if (errorCount > 0)
            {
                throw new BuildFailedException($"FUI View validation found {errorCount} error(s). The build has been stopped.");
            }
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            foreach (var assetPath in importedAssets.Where(path => path.EndsWith(".prefab")))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null || prefab.GetComponent<FUI.Rendering.UGUI.View>() == null)
                {
                    continue;
                }

                Report(ViewValidator.ValidatePrefab(assetPath));
            }
        }

        [MenuItem("FUI/Validate Views")]
        static void ValidateViews()
        {
            var issues = ViewValidator.ValidateAll();
            Report(issues);
            var errorCount = issues.Count(issue => issue.Severity == ValidationSeverity.Error);
            if (errorCount == 0)
            {
                Debug.Log($"FUI View validation passed with {issues.Count} issue(s).");
            }
        }

        static void Report(System.Collections.Generic.IEnumerable<ValidationIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    Debug.LogError(issue.ToString(), issue.Context);
                }
                else
                {
                    Debug.LogWarning(issue.ToString(), issue.Context);
                }
            }
        }
    }
}
