namespace FUI.Rendering
{
    /// <summary>汇总选择类控件共同的显隐与自身交互状态。</summary>
    public interface ISelectableElement : IVisibilityElement, IInteractableElement { }

    /// <summary>按钮公共入口；点击与原能力接口共用同一个命令。</summary>
    public interface IButtonElement : ISelectableElement, IClickElement { }
    /// <summary>图形控件公共入口，复用同一显隐和中性颜色状态。</summary>
    public interface IGraphicElement : IVisibilityElement, IColorElement { }

    /// <summary>滑块公共入口；值和命令沿用原浮点契约，后端保留原数值处理规则。</summary>
    public interface ISliderElement : ISelectableElement, IFloatValueElement
    {
        FUI.Binding.BindableProperty<float> MinValue { get; }
        FUI.Binding.BindableProperty<float> MaxValue { get; }
        FUI.Binding.BindableProperty<bool> WholeNumbers { get; }
    }

    /// <summary>开关公共入口；IsOn与原布尔Value是同一属性对象。</summary>
    public interface IToggleElement : ISelectableElement, IBoolValueElement
    {
        FUI.Binding.BindableProperty<bool> IsOn { get; }
    }
}
