using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace FUI.Editor
{
	/// <summary>
	/// 在 Hierarchy 中高亮含 IElement 的对象并追加标识图标。
	/// </summary>
	[InitializeOnLoad]
	public class HighlightElementEditor
	{
		/// <summary>
		/// 高亮背景颜色（未选中时）。
		/// </summary>
		static readonly Color HighlightColor = new Color(0.25f, 0.55f, 0.85f, 0.20f);

		/// <summary>
		/// 自定义图标大小。
		/// </summary>
		const int IconSize = 16;

		/// <summary>
		/// 名字与图标之间的间距。
		/// </summary>
		const int TextPadding = 2;

		/// <summary>
		/// Unity 默认折叠箭头与原内置图标区域宽度，用于让高亮覆盖它们但避免自绘干扰。
		/// </summary>
		const int PrefixReserveWidth = 32;

		/// <summary>
		/// 是否启用高亮功能。
		/// </summary>
		static bool enabled = true;

		/// <summary>
		/// 自定义追加图标纹理。
		/// </summary>
		static Texture2D iconTexture;

		static MethodInfo setExpandedRecursiveMethod;
		static MethodInfo setExpandedMethod;

		static HighlightElementEditor()
		{
			LoadIcon();
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
		}

		/// <summary>
		/// 加载使用的内置图标，失败时生成兜底纯色纹理。
		/// </summary>
		static void LoadIcon()
		{
			if (iconTexture != null)
			{
				return;
			}
			var content = EditorGUIUtility.IconContent("d_Linked");
			iconTexture = content != null ? content.image as Texture2D : null;
			if (iconTexture == null)
			{
				iconTexture = new Texture2D(1, 1);
				iconTexture.SetPixel(0, 0, new Color(0.2f, 0.6f, 1f));
				iconTexture.Apply();
			}
		}

		/// <summary>
		/// 在 Hierarchy 行内绘制高亮与后置图标。
		/// </summary>
		static void OnHierarchyGUI(int instanceID, Rect selectionRect)
		{
			if (!enabled)
			{
				return;
			}
			var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (obj == null)
			{
				return;
			}
			if (!HasElementComponent(obj))
			{
				return;
			}

			var nameWidth = GUI.skin.label.CalcSize(new GUIContent(obj.name)).x;
			var highlightWidth = PrefixReserveWidth + nameWidth + TextPadding + IconSize;
			var highlightRect = new Rect(selectionRect.x, selectionRect.y, highlightWidth, selectionRect.height);
			if (Selection.instanceIDs == null || Array.IndexOf(Selection.instanceIDs, instanceID) < 0)
			{
				EditorGUI.DrawRect(highlightRect, HighlightColor);
			}
			var iconX = selectionRect.x + PrefixReserveWidth + nameWidth + TextPadding;
			var iconRect = new Rect(iconX, selectionRect.y, IconSize, IconSize);
			if (iconTexture != null)
			{
				GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit, true);
			}
		}

		/// <summary>
		/// 判断是否包含 IElement 组件。
		/// </summary>
		static bool HasElementComponent(GameObject gameObject)
		{
			var behaviours = gameObject.GetComponents<MonoBehaviour>();
			for (var i = 0; i < behaviours.Length; i++)
			{
				var b = behaviours[i];
				if (b is FUI.IElement)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 设置高亮启用状态。
		/// </summary>
		public static void SetEnabled(bool active)
		{
			enabled = active;
			EditorApplication.RepaintHierarchyWindow();
		}

		/// <summary>
		/// 展开当前选中对象下所有含 IElement 的节点路径。
		/// </summary>
		[MenuItem("GameObject/FUI/ExpandElements", false, 0)]
		static void ExpandHighlightedChildrenMenu()
		{
			var root = Selection.activeGameObject;
			if (root == null)
			{
				return;
			}
			var window = GetHierarchyWindow();
			if (window == null)
			{
				return;
			}
			CacheHierarchyMethods(window);

			var highlighted = new System.Collections.Generic.List<Transform>(64);
			CollectHighlighted(root.transform, highlighted);
			if (highlighted.Count == 0)
			{
				return;
			}

			var expandSet = new System.Collections.Generic.HashSet<Transform>();
			for (var i = 0; i < highlighted.Count; i++)
			{
				var t = highlighted[i];
				while (t != null)
				{
					if (!expandSet.Add(t))
					{
						break;
					}
					if (t == root.transform)
					{
						break;
					}
					t = t.parent;
				}
			}

			foreach (var t in expandSet)
			{
				if (setExpandedMethod != null)
				{
					setExpandedMethod.Invoke(
						window,
						new object[]
						{
							t.gameObject.GetInstanceID(),
							true
						}
					);
				}
				else if (setExpandedRecursiveMethod != null)
				{
					setExpandedRecursiveMethod.Invoke(
						window,
						new object[]
						{
							t.gameObject.GetInstanceID(),
							true
						}
					);
				}
			}
			window.Repaint();
		}

		/// <summary>
		/// 递归收集所有含 IElement 的子节点。
		/// </summary>
		static void CollectHighlighted(Transform current, System.Collections.Generic.List<Transform> list)
		{
			if (HasElementComponent(current.gameObject))
			{
				list.Add(current);
			}
			for (var i = 0; i < current.childCount; i++)
			{
				CollectHighlighted(current.GetChild(i), list);
			}
		}

		[MenuItem("GameObject/FUI/ExpandElements", true)]
		static bool ValidateExpandHighlightedChildrenMenu()
		{
			return Selection.activeGameObject != null;
		}

		static EditorWindow GetHierarchyWindow()
		{
			var type = Type.GetType("UnityEditor.SceneHierarchyWindow, UnityEditor");
			if (type == null)
			{
				return null;
			}
			return EditorWindow.GetWindow(type);
		}

		/// <summary>
		/// 缓存用于展开的反射方法。
		/// </summary>
		static void CacheHierarchyMethods(EditorWindow window)
		{
			if (setExpandedMethod == null)
			{
				setExpandedMethod = window.GetType().GetMethod(
					"SetExpanded",
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					null,
					new Type[] { typeof(int), typeof(bool) },
					null
				);
			}
			if (setExpandedRecursiveMethod == null)
			{
				setExpandedRecursiveMethod = window.GetType().GetMethod(
					"SetExpandedRecursive",
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				);
			}
		}
	}
}