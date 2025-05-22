using System;

namespace FUI.Manager
{
    /// <summary>
    /// 界面设置
    /// </summary>
    public struct UISettings : IEquatable<UISettings>
    {
        ///<summary>
        /// 层级
        /// </summary>
        public readonly int layer;

        ///<summary>
        /// 标记
        /// </summary>
        public readonly Attributes flag;

        /// <summary>
        /// 前置依赖
        /// </summary>
        public readonly string[] preDependency;

        /// <summary>
        /// 后置依赖
        /// </summary>
        public readonly string[] postDependency;

        public UISettings(int layer, Attributes flag, string[] preDependency, string[] postDependency)
        {
            this.layer = layer;
            this.flag = flag;
            this.preDependency = preDependency;
            this.postDependency = postDependency;
        }

        public static UISettings Default => new UISettings((int)Layer.Common, Attributes.None, null, null);

        public bool Equals(UISettings other)
        {
            return layer == other.layer && flag == other.flag;
        }

        public override bool Equals(object obj)
        {
            return obj is UISettings other && Equals(other);
        }
    }
}