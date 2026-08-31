using System.IO;
using FUI.Rendering.UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FUI.Editor.Installation
{
    /// <summary>
    /// 创建符合 FUI 结构约定的 View Prefab，并生成常用 RoutePolicy 配置片段。
    /// </summary>
    internal sealed class PageWizard : EditorWindow
    {
        /// <summary>
        /// 定义新页面可选的导航策略预设。
        /// </summary>
        internal enum PagePreset
        {
            FullScreen,
            Popup,
            Tips,
        }

        string pageName = "NewView";
        string destination = "Assets";
        PagePreset preset;

        [MenuItem("FUI/Page Wizard")]
        static void Open()
        {
            var window = GetWindow<PageWizard>(true, "FUI Page Wizard");
            window.minSize = new Vector2(460f, 280f);
            window.destination = SelectedFolder();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Create View Prefab", EditorStyles.boldLabel);
            pageName = EditorGUILayout.TextField("Name", pageName);
            destination = EditorGUILayout.TextField("Folder", destination);
            preset = (PagePreset)EditorGUILayout.EnumPopup("Policy Preset", preset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RoutePolicy", EditorStyles.boldLabel);
            var policy = PolicySnippet(preset);
            EditorGUILayout.SelectableLabel(policy, EditorStyles.textArea, GUILayout.Height(70f));
            if (GUILayout.Button("Copy Policy"))
            {
                EditorGUIUtility.systemCopyBuffer = policy;
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!CanCreate()))
            {
                if (GUILayout.Button("Create Prefab", GUILayout.Height(32f)))
                {
                    CreatePrefab();
                }
            }
        }

        bool CanCreate()
        {
            return CanCreate(pageName, destination);
        }

        internal static bool CanCreate(string value, string folder)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrWhiteSpace(folder)
                && (folder == "Assets" || folder.StartsWith("Assets/"));
        }

        void CreatePrefab()
        {
            if (!AssetDatabase.IsValidFolder(destination))
            {
                EditorUtility.DisplayDialog("FUI", $"Folder does not exist: {destination}", "OK");
                return;
            }

            var prefab = CreatePrefabAsset(pageName, destination);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        internal static GameObject CreatePrefabAsset(string value, string folder)
        {
            var normalizedName = NormalizePageName(value);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folder, $"{normalizedName}.prefab").Replace('\\', '/'));
            var root = new GameObject(
                normalizedName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(View));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                root.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                return PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                DestroyImmediate(root);
            }
        }

        internal static string NormalizePageName(string value)
        {
            return value.EndsWith("View") ? value : $"{value}View";
        }

        internal static string PolicySnippet(PagePreset value)
        {
            switch (value)
            {
                case PagePreset.Popup:
                    return "[RoutePolicy(Layer.Popup, CoverageMode = CoverageMode.KeepVisible)]";
                case PagePreset.Tips:
                    return "[RoutePolicy(Layer.Tips, HistoryMode = HistoryMode.Transient, CoverageMode = CoverageMode.KeepVisible, AllowMultiple = true)]";
                default:
                    return "[RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide)]";
            }
        }

        static string SelectedFolder()
        {
            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                return selectedPath;
            }
            if (!string.IsNullOrEmpty(selectedPath))
            {
                return Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? "Assets";
            }
            return "Assets";
        }
    }
}
