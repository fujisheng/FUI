using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Rendering;
using FUI.Rendering.UGUI;

using UnityEditor;

using UnityEditorInternal;

using UnityEngine;
using System.Linq;
using System.Reflection;

namespace FUI.Editor.Inspection
{
    /// <summary>
    /// 在 View Inspector 中展示生成后的属性与命令绑定关系，并提供对象和源码定位入口。
    /// </summary>
    [CustomEditor(typeof(View), true)]
    internal class BindingInspector : UnityEditor.Editor
    {
        BindingManifest contextInfo;
        IView view;
        ViewInstance instance;

        bool showPropertites = true;
        bool showCommands = true;
        Vector2 propertitesScrollPosition;
        Vector2 commandsScrollPosition;

        void OnEnable()
        {
            view = target as IView;
            instance = ViewInstanceRegistry.GetInstance(view);
        }

        void OnDisable()
        {
            propertitesScrollPosition = Vector2.zero;
            commandsScrollPosition = Vector2.zero;

            contextInfo = null;
            view = null;
            instance = null;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var manifests = instance == null
                ? BindingCatalog.Instance.GetManifests(view.Name)
                : new[] { BindingCatalog.Instance.GetContextInfo(instance) };
            manifests = manifests.Where(manifest => manifest != null).ToArray();
            if (manifests.Count == 0)
            {
                EditorGUILayout.HelpBox("No BindingManifest is available for this View.", MessageType.Info);
                return;
            }

            foreach (var manifest in manifests)
            {
                DrawManifest(manifest);
            }
        }

        void DrawManifest(BindingManifest manifest)
        {
            contextInfo = manifest;
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
                this.showPropertites = EditorGUILayout.BeginFoldoutHeaderGroup(this.showPropertites, "Properties");
                {
                    if (this.showPropertites)
                    {
                        propertitesScrollPosition = EditorGUILayout.BeginScrollView(propertitesScrollPosition);
                        {

                            foreach (var property in contextInfo.properties)
                            {
                                DrawPropertyItem(contextInfo, property);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                //命令绑定列表
                this.showCommands = EditorGUILayout.BeginFoldoutHeaderGroup(this.showCommands, "Commands");
                {
                    if (this.showCommands)
                    {
                        commandsScrollPosition = EditorGUILayout.BeginScrollView(commandsScrollPosition);
                        {
                            foreach (var command in contextInfo.commands)
                            {
                                DrawCommandItem(contextInfo, command);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一个属性绑定信息
        /// </summary>
        /// <param name="contextInfo"></param>
        /// <param name="info"></param>
        void DrawPropertyItem(BindingManifest contextInfo, PropertyBindingDescriptor info)
        {
            EditorGUILayout.BeginHorizontal();
            {
                //属性信息
                var propertyInfoContent = new GUIContent($"{info.propertyInfo.name}({info.propertyInfo.type.GetTypeName()})");
                if (GUILayout.Button(propertyInfoContent, GUILayout.Width(200)))
                {
                    OpenFileAtLine(info.propertyInfo.location);
                }
                using (new EditorGUI.DisabledScope(instance == null))
                {
                    if(GUILayout.Button("?", GUILayout.Width(20)))
                    {
                        var viewModel = instance.ViewModel;
                        var property = viewModel.GetType().GetProperty(info.propertyInfo.name);
                        var propertyValue = property.GetValue(viewModel);
                        DetailsWindow.ShowDetails(Vector2.zero, "PropertyValue", propertyValue, $"{contextInfo.viewModelType}.{info.propertyInfo.name}({info.propertyInfo.type})");
                    }
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
                var targetInfoContent = new GUIContent($"{info.targetInfo.type.GetTypeName()}.{info.targetInfo.propertyName}({info.targetInfo.propertyValueType.GetTypeName()})");

                if (GUILayout.Button(targetInfoContent, GUILayout.Width(300)))
                {
                    var targetType = info.targetInfo.type.GetNamedType();
                    PingTarget(info.targetInfo.path, targetType);
                }
                using (new EditorGUI.DisabledScope(instance == null))
                {
                    if(GUILayout.Button("?", GUILayout.Width(20)))
                    {
                        var targetType = info.targetInfo.type.GetNamedType();
                        var target = (this.target as IView).GetElement(info.targetInfo.path, targetType);
                        var targetMember = ElementMemberResolver.Resolve(targetType, info.targetInfo.propertyName);
                        var property = targetMember is PropertyInfo targetProperty
                            ? targetProperty.GetValue(target)
                            : ((FieldInfo)targetMember).GetValue(target);
                        DetailsWindow.ShowDetails(Vector2.zero, "TargetValue", property, $"{info.targetInfo.type}.{info.targetInfo.propertyName}({info.targetInfo.propertyType})");
                    }
                }

                //转换器信息
                if (info.converterInfo != null)
                {
                    GUILayout.Label("@", GUILayout.Width(20));

                    var converterContent = new GUIContent();
                    if (info.converterInfo == null)
                    {
                        converterContent.text = "None";
                    }
                    else
                    {
                        converterContent.text = $"{info.converterInfo.type.GetTypeName()}({info.converterInfo.sourceType.GetTypeName()}->{info.converterInfo.targetType.GetTypeName()})";
                        converterContent.tooltip = $"{info.converterInfo.type}({info.converterInfo.sourceType}->{info.converterInfo.targetType})";
                    }

                    GUILayout.Button(converterContent, GUILayout.MinWidth(200));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void OpenFileAtLine(SourceLocation location)
        {
            InternalEditorUtility.OpenFileAtLineExternal(location.path, location.line, location.column);
        }

        /// <summary>
        /// 绘制一个命令绑定信息
        /// </summary>
        void DrawCommandItem(BindingManifest contextInfo, CommandBindingDescriptor info)
        {
            EditorGUILayout.BeginHorizontal();
            {
                //属性信息
                var commandParameter = string.Join(",", info.commandInfo.parameters);
                var propertyInfoContent = new GUIContent($"{info.commandInfo.name}({commandParameter})");
                if (GUILayout.Button(propertyInfoContent, GUILayout.Width(200)))
                {
                    OpenFileAtLine(info.commandInfo.location);
                }

                GUILayout.Label("<<", GUILayout.Width(20));

                //目标信息
                var targetParameter = string.Join(",", info.targetInfo.parameters);
                var targetInfoContent = new GUIContent($"{info.targetInfo.type.GetTypeName()}.{info.targetInfo.propertyName}({targetParameter})");

                if (GUILayout.Button(targetInfoContent, GUILayout.Width(300)))
                {
                    var targetType = info.targetInfo.type.GetNamedType();
                    PingTarget(info.targetInfo.path, targetType);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void PingTarget(string elementName, System.Type elementType)
        {
            if (instance != null)
            {
                var runtimeTarget = view.GetElement(elementName, elementType) as Component;
                if (runtimeTarget != null)
                {
                    EditorGUIUtility.PingObject(runtimeTarget.gameObject);
                }
                return;
            }

            var viewComponent = target as View;
            var editModeTarget = viewComponent
                ?.GetComponentsInChildren<MonoBehaviour>(true)
                .FirstOrDefault(component =>
                    component.gameObject.name == elementName
                    && elementType.IsAssignableFrom(component.GetType()));
            if (editModeTarget != null)
            {
                EditorGUIUtility.PingObject(editModeTarget.gameObject);
            }
        }
    }
}
