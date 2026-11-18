using System;
using FUI.RedDot.UGUI;
using UnityEditor;
using UnityEngine;

namespace FUI.RedDot.Editor
{
    /// <summary>
    /// 红点运行态只读诊断窗口，只展示中心快照，不提供手动篡改入口。
    /// </summary>
    public sealed class RedDotDebuggerWindow : EditorWindow
    {
        const int PageSize = 200;

        string search = string.Empty;
        Vector2 scroll;
        RedDotNodeSnapshot[] snapshot = Array.Empty<RedDotNodeSnapshot>();
        RedDotScope[] scopes = Array.Empty<RedDotScope>();
        string[] contextNames = { "Default Runtime" };
        IRedDotDiagnostics snapshotSource;
        ulong snapshotVersion;
        int contextIndex;
        int pageIndex;
        double nextRefresh;
        double nextContextRefresh;

        [MenuItem("FUI/Red Dot/Debugger")]
        public static void Open()
        {
            var window = GetWindow<RedDotDebuggerWindow>();
            window.titleContent = new GUIContent("Red Dot Debugger");
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.update += Refresh;
        }

        void OnDisable()
        {
            EditorApplication.update -= Refresh;
        }

        void Refresh()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh)
            {
                return;
            }
            nextRefresh = EditorApplication.timeSinceStartup + 0.25d;
            var needsRepaint = false;
            if (EditorApplication.timeSinceStartup >= nextContextRefresh)
            {
                RefreshContexts();
                nextContextRefresh = EditorApplication.timeSinceStartup + 1d;
                needsRepaint = true;
            }
            var diagnostics = ResolveDiagnostics();
            var diagnosticsChanged = !ReferenceEquals(snapshotSource, diagnostics);
            var version = diagnostics == null ? 0 : diagnostics.DiagnosticsVersion;
            if (diagnosticsChanged || snapshotVersion != version)
            {
                snapshotSource = diagnostics;
                snapshotVersion = version;
                snapshot = diagnostics == null ? Array.Empty<RedDotNodeSnapshot>() : diagnostics.GetDiagnosticsSnapshot();
                if (diagnosticsChanged) pageIndex = 0;
                needsRepaint = true;
            }
            if (needsRepaint) Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var selectedContext = EditorGUILayout.Popup(contextIndex, contextNames, GUILayout.Width(220F));
                if (selectedContext != contextIndex)
                {
                    contextIndex = selectedContext;
                    snapshotSource = null;
                    pageIndex = 0;
                    nextRefresh = 0;
                }
                var previousSearch = search;
                search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSearchTextField"));
                if (!string.Equals(previousSearch, search, StringComparison.Ordinal)) pageIndex = 0;
                GUILayout.FlexibleSpace();
                var matchCount = CountMatches();
                var pageCount = Math.Max(1, (matchCount + PageSize - 1) / PageSize);
                pageIndex = Math.Min(pageIndex, pageCount - 1);
                using (new EditorGUI.DisabledScope(pageIndex == 0))
                {
                    if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(24F))) pageIndex--;
                }
                GUILayout.Label($"{pageIndex + 1}/{pageCount}", EditorStyles.miniLabel, GUILayout.Width(52F));
                using (new EditorGUI.DisabledScope(pageIndex >= pageCount - 1))
                {
                    if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(24F))) pageIndex++;
                }
                GUILayout.Label(matchCount + " / " + snapshot.Length + " nodes", EditorStyles.miniLabel);
            }

            if (ResolveDiagnostics() == null)
            {
                EditorGUILayout.HelpBox("The selected red dot context has no diagnostics provider.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var firstMatch = pageIndex * PageSize;
            var lastMatch = firstMatch + PageSize;
            var matchIndex = 0;
            for (var index = 0; index < snapshot.Length; index++)
            {
                var node = snapshot[index];
                if (!Matches(node)) continue;
                if (matchIndex < firstMatch)
                {
                    matchIndex++;
                    continue;
                }
                if (matchIndex >= lastMatch) break;
                matchIndex++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(node.DebugName, EditorStyles.boldLabel);
                    EditorGUILayout.SelectableLabel(node.Key.Value, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.LabelField("State", $"Count={node.State.Count}, Visible={node.State.IsVisible}, Batch={node.State.BatchVersion}");
                    EditorGUILayout.LabelField("Aggregator", node.Aggregator);
                    EditorGUILayout.LabelField("Flags", $"Dynamic={node.IsDynamic}, Dirty={node.IsDirty}, Subscribers={node.SubscriberCount}, Rules={node.RuleCount}");
                    EditorGUILayout.LabelField("Last calculation", node.LastCalculationMilliseconds.ToString("0.###") + " ms");
                    DrawKeys("Parents", node.Parents);
                    DrawKeys("Children", node.Children);
                    for (var contributionIndex = 0; contributionIndex < node.Contributions.Length; contributionIndex++)
                    {
                        var contribution = node.Contributions[contributionIndex];
                        EditorGUILayout.LabelField("Contribution", $"{contribution.Owner}: {contribution.Count} ({contribution.Kind})");
                    }
                    if (node.LastDiagnostic.HasValue)
                    {
                        var diagnostic = node.LastDiagnostic.Value;
                        EditorGUILayout.HelpBox($"[{diagnostic.Kind}] {diagnostic.Owner} (batch {diagnostic.BatchVersion})\n{diagnostic.Exception}", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void RefreshContexts()
        {
            scopes = FindObjectsByType<RedDotScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            contextNames = new string[scopes.Length + 1];
            contextNames[0] = "Default Runtime";
            for (var index = 0; index < scopes.Length; index++) contextNames[index + 1] = "Scope: " + scopes[index].name;
            if (contextIndex >= contextNames.Length) contextIndex = 0;
        }

        IRedDotDiagnostics ResolveDiagnostics()
        {
            if (contextIndex == 0)
            {
                RedDotRuntime.TryGetDiagnostics(out var diagnostics);
                return diagnostics;
            }
            var scopeIndex = contextIndex - 1;
            return scopeIndex >= 0 && scopeIndex < scopes.Length ? scopes[scopeIndex].Reader as IRedDotDiagnostics : null;
        }

        bool Matches(RedDotNodeSnapshot node)
        {
            return string.IsNullOrWhiteSpace(search) ||
                   node.Key.Value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   node.DebugName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int CountMatches()
        {
            if (string.IsNullOrWhiteSpace(search)) return snapshot.Length;
            var count = 0;
            for (var index = 0; index < snapshot.Length; index++)
            {
                if (Matches(snapshot[index])) count++;
            }
            return count;
        }

        static void DrawKeys(string label, RedDotKey[] keys)
        {
            if (keys.Length == 0)
            {
                return;
            }
            var text = keys[0].Value;
            for (var index = 1; index < keys.Length; index++) text += ", " + keys[index].Value;
            EditorGUILayout.LabelField(label, text, EditorStyles.wordWrappedLabel);
        }
    }
}
