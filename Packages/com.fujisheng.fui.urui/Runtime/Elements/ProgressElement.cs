using FUI.Rendering;

namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 进度的绑定适配；填充结构和皮肤由作者资源声明。
    /// </summary>
    public sealed class ProgressElement : ValueElement<float>, IFloatValueElement
    {
        /// <summary>
        /// 组合普通作者节点及进度控制器，不创建标记或静态样式。
        /// </summary>
        public ProgressElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.ProgressIndicator control) : base(element, name, control) { }
        /// <summary>
        /// 绑定原生进度值控件。
        /// </summary>
        public ProgressElement(global::URUI.Elements.Element element, string name) : base(element, name) { }
    }
}
