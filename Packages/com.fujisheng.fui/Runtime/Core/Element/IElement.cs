namespace FUI
{
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

        /// <summary>
        /// 可见性
        /// </summary>
        bool Visible { get; set; }
    }
}
