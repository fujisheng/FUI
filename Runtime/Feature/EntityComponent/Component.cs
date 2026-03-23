namespace EntityComponent
{
    /// <summary>
    /// 组件
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// 这个组件所属的实体
        /// </summary>
        protected Entity Entity { get; private set; }

        /// <summary>
        /// 初始化这个组件
        /// </summary>
        internal void InternalInitialize(Entity entity)
        {
            Entity = entity;
            OnInitialize();
        }

        /// <summary>
        /// 释放这个组件
        /// </summary>
        internal void InternalRelease()
        {
            OnRelease();
            Entity = null;
        }

        /// <summary>
        /// 当这个组件初始化的时候
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 当这个组件被释放的时候
        /// </summary>
        protected virtual void OnRelease() { }
    }
}