using UnityEditor;

using UnityEngine;

namespace FUI.Editor
{
    /// <summary>
    /// һ��ͨ�õ����鴰��
    /// </summary>
    public class DetailsWindow : EditorWindow
    {
        object[] contents;

        public static void ShowDetails(Vector2 position, string title, params object[] contents)
        {
            var window = GetWindow<DetailsWindow>(true, title, true);
            window.contents = contents;
        }

        private void OnGUI()
        {
            if(contents == null)
            {
                return;
            }

            foreach(var content in contents)
            {
                GUILayout.Box(content.ToString());
            }
        }
    }
}