namespace FUI.Rendering
{
    /// <summary>
    /// 定义可由绑定系统寻址的渲染节点名称与父级关系。
    /// </summary>
    public interface IElement
    {
        /// <summary>
        /// 名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 父节点
        /// </summary>
        IElement Parent { get; }
    }
}
