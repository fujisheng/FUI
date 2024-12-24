namespace EntityComponent
{
    /// <summary>
    /// 标记一个结构是组件
    /// </summary>
    public interface IComponent
    {
        void Initialize();
        void Release();
    }

    /// <summary>
    /// 单例组件 标记这个组件是单例
    /// </summary>
    public interface ISingeton
    {

    }
}