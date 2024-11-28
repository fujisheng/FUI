using FUI.Test;
using FUI.UGUI;

using System.Collections.Generic;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;

namespace FUI.Editor
{
    [CustomEditor(typeof(View))]
    public class ViewInspector : UnityEditor.Editor
    {
        public static IReadOnlyList<UIEntity> entities;
        Vector2 scrollPosition;
        ContextBindingInfo contextInfo;
        IView view;
        UIEntity entity;

        void OnEnable()
        {
            entities = TestLauncher.Instance.Entities;
            view = target as IView;
            entity = GetEntity(view);
            if (entity == null)
            {
                return;
            }
            contextInfo = BindingInfoManager.Instance.GetContextInfo(entity);

            CompilerEditor.OnCompilationComplete += OnCompilationComplate;
        }

        void OnDisable()
        {
            CompilerEditor.OnCompilationComplete -= OnCompilationComplate;
            scrollPosition = Vector2.zero;
            contextInfo = null;
            view = null;
            entity = null;
        }

        void OnCompilationComplate()
        {
            contextInfo = BindingInfoManager.Instance.GetContextInfo(entity);

            this.Repaint();
        }

        public override void OnInspectorGUI()
        {
            if (contextInfo == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Button(contextInfo.viewModelType);
                    GUILayout.Label("->", GUILayout.Width(20));
                    GUILayout.Button(view.Name);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                //属性绑定列表
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(300));
                {
                    foreach (var property in contextInfo.properties)
                    {
                        DrawBindingItem(contextInfo, property);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一个绑定信息
        /// </summary>
        /// <param name="contextInfo"></param>
        /// <param name="info"></param>
        void DrawBindingItem(ContextBindingInfo contextInfo, PropertyBindingInfo info)
        {
            EditorGUILayout.BeginHorizontal();
            {
                //属性信息
                var propertyInfoContent = new GUIContent($"{info.propertyInfo.name}({info.propertyInfo.type.GetTypeName()})", $"{contextInfo.viewModelType}.{info.propertyInfo.name}({info.propertyInfo.type})");
                if (GUILayout.Button(propertyInfoContent, GUILayout.Width(200)))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(info.propertyInfo.location.path, info.propertyInfo.location.line, info.propertyInfo.location.column);
                }

                //绑定模式
                var bindingModeContent = new GUIContent();
                bindingModeContent.tooltip = info.bindingMode.ToString();
                switch (info.bindingMode)
                {
                    case BindingMode.OneWay:
                        bindingModeContent.text = ">>";
                        break;
                    case BindingMode.TwoWay:
                        bindingModeContent.text = "<>";
                        break;
                    case BindingMode.OneWayToSource:
                        bindingModeContent.text = "<<";
                        break;
                }
                GUILayout.Label(bindingModeContent, GUILayout.Width(20));

                //目标信息
                var targetInfoContent = new GUIContent($"{info.targetInfo.type.GetTypeName()}.{info.targetInfo.propertyName}({info.targetInfo.propertyValueType.GetTypeName()})", $"{info.targetInfo.type}.{info.targetInfo.propertyName}({info.targetInfo.propertyType})");
                UnityEngine.Debug.Log($"propertyType:{info.targetInfo.propertyType.GetTypeName()}");
                if (GUILayout.Button(targetInfoContent, GUILayout.Width(300)))
                {
                    var targetType = info.targetInfo.type.GetNamedType();
                    var target = (this.target as IView).GetChild(info.targetInfo.path, targetType);
                    EditorGUIUtility.PingObject((target as View).gameObject);
                }

                GUILayout.Label("@", GUILayout.Width(20));

                //转换器信息
                var converterContent = new GUIContent();
                if(info.converterInfo == null)
                {
                    converterContent.text = "None";
                }
                else
                {
                    converterContent.text = $"{info.converterInfo.type.GetTypeName()}({info.converterInfo.sourceType.GetTypeName()}->{info.converterInfo.targetType.GetTypeName()})";
                    converterContent.tooltip = $"{info.converterInfo.type}({info.converterInfo.sourceType}->{info.converterInfo.targetType})";
                }

                GUILayout.Button(converterContent, GUILayout.Width(200));
            }
            EditorGUILayout.EndHorizontal();
        }

        UIEntity GetEntity(IView view)
        {
            if(entities == null)
            {
                return null;
            }

            foreach(var entity in entities)
            {
                if (entity.View == view)
                {
                    return entity;
                }
            }

            return null;
        }
    }
}