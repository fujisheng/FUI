using UnityEditorInternal;

using UnityEngine;

namespace FUI.Editor
{
    /// <summary>
    /// 绑定上下文生成类型
    /// </summary>
    public enum BindingContextGenerateType
    {
        /// <summary>
        /// 通过特性生成
        /// </summary>
        Attribute = 1 << 0,
        /// <summary>
        /// 通过配置文件生成
        /// </summary>
        Config = 1 << 1,
        /// <summary>
        /// 通过描述文件生成
        /// </summary>
        Descriptor = 1 << 2,
    }

    /// <summary>
    ///IL后处理设置
    /// </summary>
    public class ILPostProcessesSetting : ScriptableObject
    {
        /// <summary>
        /// 目标项目
        /// </summary>
        public AssemblyDefinitionAsset targetProject;

        /// <summary>
        /// dll输出路径
        /// </summary>
        public string output;

        /// <summary>
        /// 生成类型
        /// </summary>
        public BindingContextGenerateType generateType;

        /// <summary>
        /// 绑定信息输出路径
        /// </summary>
        public string bindingInfoOutputPath;

        #region AssemblyDefinitionData
        [System.Serializable]
        class CustomScriptAssemblyData
        {
            public string name;
            public string rootNamespace;
            public string[] references;

            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
        }
        #endregion

        /// <summary>
        /// 获取目标项目名称
        /// </summary>
        public string GetTargetProjectName()
        {
            if(targetProject == null)
            {
                throw new System.Exception("targetProject is null");
            }

            var assemblyData = JsonUtility.FromJson<CustomScriptAssemblyData>(targetProject.text);
            return assemblyData.name;
        }

        /// <summary>
        /// 获取目标程序集定义文件路径
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public string GetTargetAssemblyDefinitionPath()
        {
            if (targetProject == null)
            {
                throw new System.Exception("targetProject is null");
            }
            return UnityEditor.AssetDatabase.GetAssetPath(targetProject);
        }
    }
}