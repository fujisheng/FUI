namespace EntityComponent
{
    public static partial class EntityQueryBuilder
    {
        /// <summary>
        /// 所有包含这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public static EntityQuery AllOf<T1>(EntityManager entityManager) where T1 : struct, IComponent
        {
            return new EntityQuery(entityManager).AllOf<T1>();
        }

        /// <summary>
        /// 所有包含这些组件的实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public static EntityQuery AllOf<T1, T2>(EntityManager entityManager) where T1 : struct, IComponent where T2 : struct, IComponent
        {
            return new EntityQuery(entityManager).AllOf<T1, T2>();
        }

        public static EntityQuery AllOf<T1, T2, T3>(EntityManager entityManager) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
        {
            return new EntityQuery(entityManager).AllOf<T1, T2, T3>();
        }
    }
}
