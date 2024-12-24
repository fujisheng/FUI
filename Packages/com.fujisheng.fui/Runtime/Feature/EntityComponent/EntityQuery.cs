using System;
using System.Collections.Generic;

namespace EntityComponent
{
    #region 遍历方法委托定义
    public delegate void E_C<T>(Entity entity, ref T c) where T : struct, IComponent;
    public delegate void E_C_C<T1, T2>(Entity entity, ref T1 c1, ref T2 c2) where T1 : struct, IComponent where T2 : struct, IComponent;
    public delegate void E_C_C_C<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent;
    public delegate void E_C_C_C_C<T1, T2, T3, T4>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent;
    #endregion

    public class EntityQuery
    {
        EntityManager entityManager;
        List<Entity> entities;
        uint verion;

        private EntityQuery() { }

        internal EntityQuery(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            entities = new List<Entity>();
            this.verion = 0u;
        }

        #region 遍历实体
        /// <summary>
        /// 遍历满足条件的Entity
        /// </summary>
        /// <param name="action"></param>
        public void Foreach(Action<Entity> action)
        {
            this.verion = entityManager.Version;

            foreach (var entity in entities)
            {
                action.Invoke(entity);
            }
        }

        /// <summary>
        /// 遍历满足条件的Entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void Foreach<T>(E_C<T> action) where T : struct, IComponent
        {
            this.verion = entityManager.Version;

            foreach(var entity in entities)
            {
                var component = entityManager.GetComponent<T>(entity);
                action.Invoke(entity, ref component);
                entity.Replace(component);
            }
        }

        /// <summary>
        /// 遍历满足条件的Entity
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="action"></param>
        public void Foreach<T1, T2>(E_C_C<T1,T2> action) where T1 :struct,IComponent where T2 : struct, IComponent
        {
            this.verion = entityManager.Version;
            foreach(var entity in entities)
            {
                var c1 = entityManager.GetComponent<T1>(entity);
                var c2 = entityManager.GetComponent<T2>(entity);
                action.Invoke(entity, ref c1, ref c2);
                entity.Replace(c1);
                entity.Replace(c2);
            }
        }

        /// <summary>
        /// 遍历满足条件的Entity
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="action"></param>
        public void Foreach<T1,T2,T3>(E_C_C_C<T1,T2,T3>action) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
        {
            this.verion = entityManager.Version;
            foreach(var entity in entities)
            {
                var c1 = entityManager.GetComponent<T1>(entity);
                var c2 = entityManager.GetComponent<T2>(entity);
                var c3 = entityManager.GetComponent<T3>(entity);
                action.Invoke(entity, ref c1, ref c2, ref c3);
                entity.Replace(c1);
                entity.Replace(c2);
                entity.Replace(c3);
            }
        }

        /// <summary>
        /// 遍历满足条件的Entity
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="action"></param>
        public void Foreach<T1, T2, T3, T4>(E_C_C_C_C<T1, T2, T3, T4> action) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
        {
            this.verion = entityManager.Version;
            foreach (var entity in entities)
            {
                var c1 = entityManager.GetComponent<T1>(entity);
                var c2 = entityManager.GetComponent<T2>(entity);
                var c3 = entityManager.GetComponent<T3>(entity);
                var c4 = entityManager.GetComponent<T4>(entity);
                action.Invoke(entity, ref c1, ref c2, ref c3, ref c4);
                entity.Replace(c1);
                entity.Replace(c2);
                entity.Replace(c3);
                entity.Replace(c4);
            }
        }
        #endregion

        /// <summary>
        /// 查找所有拥有这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery AllOf<T1>() where T1 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            this.entities.Clear();

            this.entityManager.Foreach(item =>
            {
                if (this.entityManager.HasComponent<T1>(item))
                {
                    this.entities.Add(item);
                }
            });
            return this;
        }

        /// <summary>
        /// 查找所有拥有这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery AllOf<T1, T2>() where T1:struct,IComponent where T2 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            this.entities.Clear();

            this.entityManager.Foreach(item =>
            {
                if (this.entityManager.HasComponent<T1>(item)
                && this.entityManager.HasComponent<T2>(item))
                {
                    this.entities.Add(item);
                }
            });
            return this;
        }

        /// <summary>
        /// 查找所有拥有这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery AllOf<T1, T2, T3>() where T1:struct,IComponent where T2:struct,IComponent where T3 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            this.entities.Clear();

            this.entityManager.Foreach(item =>
            {
                if (this.entityManager.HasComponent<T1>(item)
                && this.entityManager.HasComponent<T2>(item)
                && this.entityManager.HasComponent<T3>(item))
                {
                    this.entities.Add(item);
                }
            });
            return this;
        }

        /// <summary>
        /// 查找所有拥有这些组件的实体
        /// </summary>
        public EntityQuery AllOf<T1,T2,T3,T4>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            this.entities.Clear();

            this.entityManager.Foreach(item =>
            {
                if (this.entityManager.HasComponent<T1>(item)
                && this.entityManager.HasComponent<T2>(item)
                && this.entityManager.HasComponent<T3>(item)
                && this.entityManager.HasComponent<T4>(item))
                {
                    this.entities.Add(item);
                }
            });
            return this;
        }

        /// <summary>
        /// 查找所有拥有任意组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery AnyOf<T1, T2>() where T1:struct, IComponent where T2 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            if (entities.Count != 0)
            {
                entities.RemoveAll(item =>
                    !this.entityManager.HasComponent<T1>(item)
                    && !this.entityManager.HasComponent<T2>(item));
                return this;
            }

            this.entityManager.Foreach(item =>
            {
                if (this.entityManager.HasComponent<T1>(item)
                    || this.entityManager.HasComponent<T2>(item))
                {
                    this.entities.Add(item);
                }
            });
            return this;
        }

        /// <summary>
        /// 查找所有不拥有这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery NoneOf<T1>()where T1 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            entities.RemoveAll(item => this.entityManager.HasComponent<T1>(item));
            return this;
        }

        /// <summary>
        /// 查找所有不拥有这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public EntityQuery NoneOf<T1, T2>() where T1 : struct, IComponent where T2:struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            entities.RemoveAll(item =>
                this.entityManager.HasComponent<T1>(item)
                || this.entityManager.HasComponent<T2>(item));
            return this;
        }

        /// <summary>
        /// 查找所有不拥有这些组件的实体
        /// </summary>
        public EntityQuery NoneOf<T1,T2,T3>()where T1 : struct, IComponent where T2:struct,IComponent where T3 : struct, IComponent
        {
            if (verion == entityManager.Version)
            {
                return this;
            }

            entities.RemoveAll(item =>
                this.entityManager.HasComponent<T1>(item)
                || this.entityManager.HasComponent<T2>(item)
                || this.entityManager.HasComponent<T3>(item));
            return this;
        }
    }
}
