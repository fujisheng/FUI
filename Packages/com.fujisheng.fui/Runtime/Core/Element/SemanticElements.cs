using FUI.Binding;

namespace FUI.Rendering
{
    /// <summary>提供与具体渲染后端无关的纯文本绑定目标。</summary>
    public interface ITextElement : IGraphicElement, ITextAlignmentElement, IFormattedTextElement
    {
        /// <summary>节点展示的纯文本值。</summary>
        BindableProperty<string> Text { get; }
        /// <summary>通用字号；具体字体资源由后端管理。</summary>
        BindableProperty<float> FontSize { get; }
        /// <summary>后端沿用既有富文本开关语义。</summary>
        BindableProperty<bool> RichText { get; }
    }

    /// <summary>保留现有任意对象文本绑定及后端的文化格式化语义。</summary>
    public interface ITextContentElement : IElement
    {
        BindableProperty<object> Content { get; }
    }

    /// <summary>提供与具体输入系统无关的点击命令绑定目标。</summary>
    public interface IClickElement : IElement
    {
        /// <summary>节点被激活时发出的点击命令。</summary>
        Command Click { get; }
    }

    /// <summary>提供控件自身的可交互状态；不改变父视图可见性或点击命令。</summary>
    public interface IInteractableElement : IElement
    {
        BindableProperty<bool> Interactable { get; }
    }

    /// <summary>提供浮点控件值与用户输入事件，不包含值域、布局或输入设备语义。</summary>
    public interface IFloatValueElement : IElement
    {
        /// <summary>可读写的当前控件值；程序赋值不应额外触发用户输入命令。</summary>
        BindableProperty<float> Value { get; }

        /// <summary>由用户输入导致有效值变更时发出的命令。</summary>
        Command<float> ValueChanged { get; }
    }

    /// <summary>提供布尔控件值与用户输入事件，不包含后端布局或输入设备语义。</summary>
    public interface IBoolValueElement : IElement
    {
        /// <summary>可读写的当前控件值；程序赋值不应额外触发用户输入命令。</summary>
        BindableProperty<bool> Value { get; }

        /// <summary>由用户输入导致有效值变更时发出的命令。</summary>
        Command<bool> ValueChanged { get; }
    }
    /// <summary>提供节点自身的可见性绑定，不包含布局或视图所有权。</summary>
    public interface IVisibilityElement : IElement
    {
        BindableProperty<bool> Visible { get; }
    }

    /// <summary>提供动态资源键与真实子VM绑定，实例加载及子投影由后端负责。</summary>
    public interface IDynamicViewElement : IElement
    {
        BindableProperty<string> Source { get; }
        BindableProperty<ObservableObject> ViewModel { get; }
    }
}
