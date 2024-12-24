using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityComponent
{
    public partial class EntityManager
    {
        /// <summary>
        /// 所有的entity
        /// </summary>
        List<Entity> entities;

        /// <summary>
        /// 标记哪些entity 是被销毁的
        /// </summary>
        HashSet<int> destoryedEntityIndices;

        /// <summary>
        /// 单例实体
        /// </summary>
        internal readonly Entity SingetonEntity;

        /// <summary>
        /// 实体组件查找表
        /// </summary>
        EntityComponentIndexLookup lookup;

        /// <summary>
        /// 版本
        /// </summary>
        internal uint Version { get; private set; }
        
        internal EntityManager()
        {
            entities = new List<Entity>();
            destoryedEntityIndices = new HashSet<int>();
            SingetonEntity = CreateEntity();
            lookup = new EntityComponentIndexLookup();
            Version = 1u;
        }

        /// <summary>
        /// 创建一个Entity
        /// </summary>
        /// <returns></returns>
        public Entity CreateEntity()
        {
            var index = GetNewIndex();
            if(index < entities.Count)
            {
                destoryedEntityIndices.Remove(index);
                return entities[index];
            }

            var entity = new Entity(index);
            entities.Add(entity);
            Version++;
            return entity;
        }

        /// <summary>
        /// 获取一个要创建的Entity的索引
        /// </summary>
        /// <returns></returns>
        int GetNewIndex()
        {
            return destoryedEntityIndices.Count != 0 ? destoryedEntityIndices.First() : entities.Count;
        }

        /// <summary>
        /// 销毁一个Entity
        /// </summary>
        /// <param name="entity"></param>
        public void DestroyEntity(Entity entity)
        {
            if (destoryedEntityIndices.Contains(entity.Index))
            {
                throw new Exception($"{entity} is already been destoryed");
            }

            if(entity.Index == SingetonEntity.Index)
            {
                throw new Exception($"attemp to destroy SingetonEntity");
            }

            destoryedEntityIndices.Add(entity.Index);
            Version++;
        }

        /// <summary>
        /// 销毁所有的实体 TODO 移除这个实体身上的所有组件
        /// </summary>
        public void DestroyEntities()
        {
            foreach(var entity in entities)
            {
                destoryedEntityIndices.Add(entity.Index);
            }
            Version++;
        }

        /// <summary>
        /// 通过一个索引获取实体
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal Entity GetEntity(int index)
        {
            if (destoryedEntityIndices.Contains(index))
            {
                throw new Exception($"attemp to access an destroyed entity:{index}");
            }
            if(index >= entities.Count)
            {
                throw new Exception($"dont have entity:{index}");
            }

            return entities[index];
        }

        /// <summary>
        /// 遍历所有的entity
        /// </summary>
        /// <param name="action"></param>
        public void Foreach(Action<Entity> action)
        {
            for(int i = 0; i < entities.Count; i++)
            {
                if (destoryedEntityIndices.Contains(i))
                {
                    continue;
                }
                action.Invoke(entities[i]);
            }
        }
    }
}
