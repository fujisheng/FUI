namespace FUI
{
    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IView
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
        /// 获取视觉元素
        /// </summary>
        /// <typeparam name="T">视觉元素类型</typeparam>
        /// <param name="path">路径</param>
        /// <returns></returns>
        T GetVisualElement<T>(string path) where T : IVisualElement;

        /// <summary>
        /// 获取视觉元素
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        IVisualElement GetVisualElement(string path);

        /// <summary>
        /// 激活
        /// </summary>
        void Enable();

        /// <summary>
        /// 禁用
        /// </summary>
        void Disable();

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }
}
