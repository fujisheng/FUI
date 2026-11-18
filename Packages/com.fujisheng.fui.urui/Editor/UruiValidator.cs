using System;
using System.Collections.Generic;
using global::URUI.Editor.Authoring;

namespace FUI.Rendering.URUI.Editor
{
    public sealed class UruiValidationIssue
    {
        public UruiValidationIssue(string code, string message, string uri = "", int line = 0, string node = "")
        { Code = code; Message = message; Uri = uri; Line = line; Node = node; }
        public string Code { get; }
        public string Message { get; }
        public string Uri { get; }
        public int Line { get; }
        public string Node { get; }
    }
    public sealed class UruiValidationResult
    {
        public UruiValidationResult(IReadOnlyList<UruiValidationIssue> issues) { Issues = issues; }
        public IReadOnlyList<UruiValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;
    }
    /// <summary>作者结构与局部选择器检查；CSS 语法交给当前 native 解析器。</summary>
    public static class UruiValidator
    {
        internal sealed class Node
        {
            public string Tag, Id, Classes, Source;
            public int Line;
        }
        internal static List<Node> ReadNodes(string html, string uri, List<UruiValidationIssue> issues, bool isFragment = true)
        {
            var nodes = new List<Node>();
            if (string.IsNullOrWhiteSpace(html)) {
                issues.Add(new UruiValidationIssue("empty-html", "作者内容不能为空。", uri)); return nodes;
            }
            var result = HtmlCssValidator.Validate(html, string.IsNullOrEmpty(uri) ? "urui:authoring" : uri, isFragment);
            foreach (var diagnostic in result.Diagnostics)
                issues.Add(new UruiValidationIssue(diagnostic.Code, diagnostic.Message, diagnostic.Uri, diagnostic.Line));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in result.Nodes) {
                if (value.IsInert) continue;
                nodes.Add(new Node { Tag = value.Tag, Id = value.Id, Classes = value.Classes, Source = value.Source, Line = value.Line });
                if (!string.IsNullOrEmpty(value.Id) && !ids.Add(value.Id))
                    issues.Add(new UruiValidationIssue("duplicate-id", "同一文档的 id 必须唯一：" + value.Id, uri, value.Line, "#" + value.Id));
            }
            return nodes;
        }
        public static UruiValidationResult Validate(string html, string uri = "")
        {
            var issues = new List<UruiValidationIssue>();
            ReadNodes(html, uri, issues);
            return new UruiValidationResult(issues.AsReadOnly());
        }
    }
}
