using System;

namespace FUI.Manager
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SettingsAttribute : Attribute
    {
        public readonly UISettings settings;

        /// <summary>
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="attributes">特性</param>
        /// <param name="preDependency">前置依赖 在打开这个界面之前需要打开的界面</param>
        /// <param name="postDependency">后置依赖 在打开这个界面之后需要打开的界面</param>
        public SettingsAttribute(Layer layer = Layer.Common, Attributes attributes = Attributes.None, string[] preDependency = null, string[] postDependency = null) : this((int)layer, attributes, preDependency, postDependency) { }

        /// <summary>
        /// 定义一个默认的界面配置
        /// </summary>
        /// <param name="layer">层级</param>
        /// <param name="attributes">特性</param>
        /// <param name="preDependency">前置依赖 在打开这个界面之前需要打开的界面</param>
        /// <param name="postDependency">后置依赖 在打开这个界面之后需要打开的界面</param>
        public SettingsAttribute(int layer, Attributes attributes = Attributes.None, string[] preDependency = null, string[] postDependency = null)
        {
            this.settings = new UISettings(layer, attributes, preDependency, postDependency);
        }
    }
}