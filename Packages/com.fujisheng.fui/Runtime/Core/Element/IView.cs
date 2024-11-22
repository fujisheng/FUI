namespace FUI
{
    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IView : IElement
    {
        /// <summary>
        /// 视图名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 层级
        /// </summary>
        int Layer { get; set; }

        /// <summary>
        /// 顺序
        /// </summary>
        int Order { get; set; }

        /// <summary>
        /// 可见性
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }
}
