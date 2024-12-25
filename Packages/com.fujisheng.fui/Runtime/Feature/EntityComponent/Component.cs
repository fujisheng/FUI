namespace EntityComponent
{
    /// <summary>
    /// ���
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// ������������ʵ��
        /// </summary>
        protected Entity Entity { get; private set; }

        /// <summary>
        /// ��ʼ��������
        /// </summary>
        internal void InternalInitialize(Entity entity)
        {
            Entity = entity;
            OnInitialize();
        }

        /// <summary>
        /// �ͷ�������
        /// </summary>
        internal void InternalRelease()
        {
            OnRelease();
            Entity = null;
        }

        /// <summary>
        /// ����������ʼ����ʱ��
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// �����������ͷŵ�ʱ��
        /// </summary>
        protected virtual void OnRelease() { }
    }
}