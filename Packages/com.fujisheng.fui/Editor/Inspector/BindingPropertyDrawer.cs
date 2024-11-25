using FUI.UGUI;

using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace FUI.Editor
{
    [CustomEditor(typeof(View))]
    public class ViewInspector : UnityEditor.Editor
    {
        public static IReadOnlyList<UIEntity> entities;

        public override void OnInspectorGUI()
        {
            //if (entities == null) 
            //{
            //    return;
            //}

            var view = target as View;

            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LinkButton("TestViewModel");
                    GUILayout.Label("->");
                    GUILayout.Label("TestView");
                }
                EditorGUILayout.EndHorizontal();

                for(int i = 0; i < 10; i++)
                {
                    ShowBindingItem();
                }
            }
            EditorGUILayout.EndVertical();
        }

        void ShowBindingItem()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LinkButton("PropertyName");
                GUILayout.Label("<->");
                EditorGUILayout.LinkButton("ViewPropertyName");
                GUILayout.Button("@");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}