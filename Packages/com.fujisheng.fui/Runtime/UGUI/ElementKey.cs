using System;

namespace FUI.UGUI
{
    internal readonly struct ElementKey : IEquatable<ElementKey>
    {
        public readonly string elementPath;
        public readonly Type elementType;

        public ElementKey(string elementPath, Type elementType)
        {
            this.elementPath = elementPath;
            this.elementType = elementType;
        }

        public bool IsEmpty => elementPath == null && elementType == null;

        public bool Equals(ElementKey other)
        {
            return elementPath == other.elementPath && elementType == other.elementType;
        }

        public override bool Equals(object obj)
        {
            if(obj is ElementKey elementKey)
            {
                return this.Equals(elementKey);
            }
            return false;
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
