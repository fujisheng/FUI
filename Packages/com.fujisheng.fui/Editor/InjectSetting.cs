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

    public class InjectSetting : ScriptableObject
    {
        public string[] dlls;
        public string solutionPath;
        public string projectName;
        public string output;
        public string bindingPath;
        public string generatedPath;
        public BindingContextGenerateType generateType;
    }
}