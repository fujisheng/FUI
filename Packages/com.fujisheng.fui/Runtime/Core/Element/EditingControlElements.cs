using System.Collections.Generic;
using FUI.Binding;

namespace FUI.Rendering
{
    /// <summary>下拉框公共契约；Value保留后端整数语义，多选掩码不转为单一索引。</summary>
    public interface IDropdownElement : ISelectableElement
    {
        BindableProperty<int> Value { get; }
        BindableProperty<IReadOnlyList<string>> Options { get; }
        BindableProperty<float> AlphaFadeSpeed { get; }
        Command<int> ValueChanged { get; }
    }

    /// <summary>支持多选的下拉后端额外能力；不要求Legacy控件伪造多选。</summary>
    public interface IMultiSelectDropdownElement : IDropdownElement
    {
        BindableProperty<bool> MultiSelect { get; }
    }

    /// <summary>输入框公共契约，三种用户事件分别复用原命令；不同于显示文本的样式契约。</summary>
    public interface IInputFieldElement : ISelectableElement
    {
        BindableProperty<string> Text { get; }
        BindableProperty<bool> ReadOnly { get; }
        BindableProperty<int> CharacterLimit { get; }
        BindableProperty<float> CaretBlinkRate { get; }
        BindableProperty<int> CaretWidth { get; }
        Command<string> ValueChanged { get; }
        Command<string> EndEdit { get; }
        Command<string> Submit { get; }
    }

    /// <summary>滚动条公共契约；原浮点状态/命令同源，方向和数值算法留给后端。</summary>
    public interface IScrollbarElement : ISelectableElement, IFloatValueElement
    {
        BindableProperty<float> Size { get; }
        BindableProperty<int> NumberOfSteps { get; }
    }
}
