using UnityEngine;

namespace FUI.Editor.Validation
{
    /// <summary>
    /// 表示 View 资源验证问题的严重级别。
    /// </summary>
    public enum ValidationSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// 描述一个可定位到资源或 Unity 对象的 View 验证问题。
    /// </summary>
    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string AssetPath { get; }
        public string Message { get; }
        public Object Context { get; }

        public ValidationIssue(
            ValidationSeverity severity,
            string assetPath,
            string message,
            Object context = null)
        {
            Severity = severity;
            AssetPath = assetPath;
            Message = message;
            Context = context;
        }

        public override string ToString()
        {
            return $"[{Severity}] {AssetPath}: {Message}";
        }
    }
}
