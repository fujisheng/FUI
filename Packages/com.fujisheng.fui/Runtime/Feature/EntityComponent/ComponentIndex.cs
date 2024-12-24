using System.Collections.Generic;
using System.Linq;

namespace EntityComponent
{
    static class ComponentType
    {
        static int currentType = 0;
        public static int GetNewType()
        {
            currentType++;
            return currentType;
        }
    }
    class ComponentIndex<T> where T : struct, IComponent
    {
        List<T> components;
        HashSet<int> removedIndices;
        internal int TypeId { get; private set; }

        internal ComponentIndex()
        {
            components = new List<T>();
            removedIndices = new HashSet<int>();
            TypeId = ComponentType.GetNewType();
        }

        /// <summary>
        /// 加入一个组件
        /// </summary>
        /// <param name="component">组件</param>
        /// <returns>返回组件在数组中的索引</returns>
        internal int AddComponent(T component)
        {
            if(removedIndices.Count == 0)
            {
                var addIndex = components.Count;
                components.Add(component);
                return addIndex;
            }

            var index = removedIndices.First();
            components[index] = component;
            removedIndices.Remove(index);
            return index;
        }

        /// <summary>
        /// 删除一个组件
        /// </summary>
        /// <param name="index"></param>
        internal void RemoveComponent(int index)
        {
            if(index >= components.Count)
            {
                return;
            }

            removedIndices.Add(index);
        }

        /// <summary>
        /// 根据一个索引获取组件
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal T GetComponent(int index)
        {
            if(removedIndices.Contains(index))
            {
                throw new System.Exception("attempt to access an already deleted component");
            }

            if(index < components.Count)
            {
                return components[index];
            }

            throw new System.Exception($"dont have index:{index} at {typeof(T)}components");
        }

        /// <summary>
        /// 替换一个组件
        /// </summary>
        /// <param name="index">组件索引</param>
        /// <param name="component">组件数据</param>
        internal void ReplaceComponent(int index, T component)
        {
            if (removedIndices.Contains(index))
            {
                throw new System.Exception("attempt to access an already deleted component");
            }
            
            if(index < components.Count)
            {
                components[index] = component;
                return;
            }

            throw new System.Exception($"dont have index:{index} at {typeof(T)} components");
        }
    }
}