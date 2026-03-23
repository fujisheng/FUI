using System;

namespace FUI.UGUI
{
    /// <summary>
    /// Element唯一标识
    /// </summary>
    internal readonly struct ElementUniqueKey : IEquatable<ElementUniqueKey>
    {
        /// <summary>
        /// 路径
        /// </summary>
        internal readonly string elementPath;

        /// <summary>
        /// 类型
        /// </summary>
        internal readonly Type elementType;

        /// <summary>
        /// 缓存的哈希值
        /// </summary>
        readonly int hashCode;

        /// <summary>
        /// 构造一个ElementUniqueKey
        /// </summary>
        /// <param name="elementPath">元素路径</param>
        /// <param name="elementType">元素类型</param>
        internal ElementUniqueKey(string elementPath, Type elementType)
        {
            this.elementPath = elementPath;
            this.elementType = elementType;
            hashCode = HashCode.Combine(elementPath, elementType);
        }

        public bool Equals(ElementUniqueKey other)
        {
            if(hashCode != other.hashCode)
            {
                return false;
            }
            return elementPath == other.elementPath && elementType == other.elementType;
        }

        public override bool Equals(object obj)
        {
            if(obj is ElementUniqueKey elementKey)
            {
                return this.Equals(elementKey);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            return $"ElementUniqueKey(path:{elementPath} type:{elementType})";
        }
    }
}
