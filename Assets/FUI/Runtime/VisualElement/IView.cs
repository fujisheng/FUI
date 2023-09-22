using FUI.Bindable;

namespace FUI
{
    public interface IView
    {
        /// <summary>
        /// 界面名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 绑定的上下文
        /// </summary>
        ObservableObject BindingContext { get; }

        /// <summary>
        /// 绑定一个可观察对象
        /// </summary>
        /// <param name="observableObject"></param>
        void Binding(ObservableObject observableObject);

        /// <summary>
        /// 解绑
        /// </summary>
        void Unbinding();

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
