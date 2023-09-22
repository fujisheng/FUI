using System;

namespace FUI
{
    readonly struct ElementKey : IEquatable<ElementKey>
    {
        public readonly string elementPath;
        public readonly string elementType;

        public ElementKey(string elementPath, string elementType)
        {
            this.elementPath = elementPath;
            this.elementType = elementType;
        }

        public ElementKey(string elementPath, Type elementType)
        {
            this.elementPath = elementPath;
            this.elementType = elementType.FullName;
        }

        public bool IsEmpty => elementPath == null && elementType == null;

        public bool Equals(ElementKey other)
        {
            return elementPath == other.elementPath && elementType == other.elementType;
        }

        public override int GetHashCode()
        {
            return elementPath.GetHashCode() ^ elementType.GetHashCode();
        }

        public override string ToString()
        {
            return $"name:{elementPath} type:{elementType}";
        }
    }
}
