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
        Attribute,
        /// <summary>
        /// 通过配置文件生成
        /// </summary>
        Config,
        /// <summary>
        /// 混合生成
        /// </summary>
        Mix,
    }

    /// <summary>
    /// 编译器设置
    /// </summary>
    public class CompilerSetting : ScriptableObject
    {
        /// <summary>
        /// 编译器路径
        /// </summary>
        public string compilerPath;
        /// <summary>
        /// 解决方案路径
        /// </summary>
        public string solutionPath;

        /// <summary>
        /// 目标项目
        /// </summary>
        public AssemblyDefinitionAsset targetProject;

        /// <summary>
        /// dll输出路径
        /// </summary>
        public string output;

        /// <summary>
        /// 中间代码生成路径 可为空则不会输出中间代码
        /// </summary>
        public string generatedPath;

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
    }
}