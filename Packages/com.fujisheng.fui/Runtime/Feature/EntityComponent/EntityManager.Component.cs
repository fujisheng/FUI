namespace EntityComponent
{
    public partial class EntityManager
    {
        /// <summary>
        /// 添加一个单例组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件值</param>
        public void AddSingetonComponent<T>(T component) where T : struct, IComponent, ISingeton
        {
            AddComponent(SingetonEntity, component);
        }

        /// <summary>
        /// 获取一个单例组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public T GetSingetonComponent<T>() where T : struct, IComponent, ISingeton
        {
            return GetComponent<T>(SingetonEntity);
        }

        /// <summary>
        /// 移除一个单例组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void RemoveSingetonComponent<T>() where T : struct, IComponent, ISingeton
        {
            RemoveComponent<T>(SingetonEntity);
        }

        /// <summary>
        /// 替换一个单例组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        public void ReplaceSingetonComponent<T>(T component) where T : struct, IComponent, ISingeton
        {
            ReplaceComponent<T>(SingetonEntity, component);
        }

        /// <summary>
        /// 为一个实体添加一个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">实体</param>
        /// <param name="component">组件值</param>
        /// <returns></returns>
        public void AddComponent<T>(Entity entity, T component) where T : struct, IComponent
        {
            lookup.AddComponent(entity.Index, component);
            Version++;
        }

        /// <summary>
        /// 获取一个实体上的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            return lookup.GetComponent<T>(entity.Index);
        }

        /// <summary>
        /// 移除一个实体上的某个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
        {
            lookup.RemoveComponent<T>(entity.Index);
            Version++;
        }

        /// <summary>
        /// 某个实体是否拥有某个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        public bool HasComponent<T>(Entity entity) where T : struct, IComponent
        {
            return lookup.HasComponent<T>(entity.Index);
        }

        /// <summary>
        /// 替换某个实体上的某个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">实体</param>
        /// <param name="component">组件数据</param>
        public void ReplaceComponent<T>(Entity entity, T component) where T : struct, IComponent
        {
            lookup.ReplaceComponent<T>(entity.Index, component);
        }
    }
}
