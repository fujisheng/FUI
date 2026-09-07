using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 将 RectTransform 保持在设备安全区内，并按设计宽度与最小可用高度缩放内容。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [MovedFrom(true, "FUI.Cli.Layout", "FUI.Cli.Layout", "WebSafeArea")]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [FormerlySerializedAs("referenceWidth")]
        [Min(1f)]
        public float ReferenceWidth = 941f;

        [FormerlySerializedAs("minimumHeight")]
        [Min(1f)]
        public float MinimumHeight = 1672f;

        Rect lastSafeArea;
        Vector2Int lastScreenSize;
        Vector2 lastParentSize;

        void OnEnable()
        {
            Refresh(true);
        }

        void OnRectTransformDimensionsChange()
        {
            Refresh(true);
        }

        void LateUpdate()
        {
            Refresh(false);
        }

        /// <summary>
        /// 使用当前屏幕和父节点尺寸刷新安全区布局。
        /// </summary>
        public void Refresh()
        {
            Refresh(true);
        }

        void Refresh(bool force)
        {
            var parent = transform.parent as RectTransform;
            if (parent == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var parentSize = parent.rect.size;
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize && parentSize == lastParentSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastParentSize = parentSize;
            Apply(safeArea, screenSize, parentSize);
        }

        /// <summary>
        /// 使用显式输入计算布局，便于编辑器工具和测试稳定复现安全区结果。
        /// </summary>
        public void Apply(Rect safeAreaPixels, Vector2 screenSizePixels, Vector2 parentSize)
        {
            if (screenSizePixels.x <= 0f || screenSizePixels.y <= 0f || parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            var availableSize = Vector2.Scale(safeAreaPixels.size / screenSizePixels, parentSize);
            var referenceWidth = Mathf.Max(1f, ReferenceWidth);
            var minimumHeight = Mathf.Max(1f, MinimumHeight);
            var scale = Mathf.Min(1f, availableSize.x / referenceWidth, availableSize.y / minimumHeight);
            if (scale <= 0f)
            {
                return;
            }

            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = Vector2.one * 0.5f;
            rectTransform.anchorMax = Vector2.one * 0.5f;
            rectTransform.pivot = Vector2.one * 0.5f;
            rectTransform.anchoredPosition = Vector2.Scale(
                safeAreaPixels.center / screenSizePixels - Vector2.one * 0.5f,
                parentSize);
            rectTransform.sizeDelta = new Vector2(referenceWidth, availableSize.y / scale);
            rectTransform.localScale = Vector3.one * scale;
        }
    }
}
