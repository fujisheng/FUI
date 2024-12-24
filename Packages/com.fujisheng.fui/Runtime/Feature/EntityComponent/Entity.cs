using System;

namespace EntityComponent
{
    /// <summary>
    /// 实体
    /// </summary>
    public struct Entity : IEquatable<Entity>
    {
        internal int Index { get; private set; }
        string Name { get; set; }

        internal Entity(int index, string name = null)
        {
            this.Index = index;
            this.Name = name;
        }

        public static bool operator==(Entity l, Entity r)
        {
            return l.Equals(r);
        }

        public static bool operator!=(Entity l, Entity r)
        {
            return !l.Equals(r);
        }

        public bool Equals(Entity other)
        {
            return this.Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Entity other))
            {
                return false;
            }

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? $"Entity({Index})" : Name;
        }
    }
}