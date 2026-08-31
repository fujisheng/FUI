using System;

namespace FUI.Binding
{
    /// <summary>仅供生成器语义分析使用的属性绑定特性占位类型。</summary>
    public class BindAttribute : Attribute { }

    /// <summary>仅供生成器语义分析使用的 View 契约特性占位类型。</summary>
    public class ViewContractAttribute : Attribute { }

    /// <summary>仅供生成器语义分析使用的命令绑定特性占位类型。</summary>
    public class CommandAttribute : Attribute { }

    /// <summary>仅供生成器识别值转换器契约的非泛型占位接口。</summary>
    public interface IValueConverter { }

    /// <summary>仅供生成器识别强类型值转换器契约的占位接口。</summary>
    public interface IValueConverter<T1, T2> { }

    /// <summary>仅供生成器识别可观察对象继承关系的占位类型。</summary>
    public class ObservableObject { }
    /// <summary>仅供生成器识别可绑定属性类型的占位类型。</summary>
    public class BindableProperty<T> { }
    /// <summary>仅供生成器识别可绑定属性接口的占位类型。</summary>
    public interface IBindableProperty<T> { }
    /// <summary>仅供生成器识别属性变更通知接口的占位类型。</summary>
    public interface INotifyPropertyChanged { }
    /// <summary>仅供生成器识别集合变更通知接口的占位类型。</summary>
    public interface INotifyCollectionChanged { }
    /// <summary>仅供生成器识别命令参数签名的占位类型。</summary>
    public class CommandTemplate<T> { }
}

namespace FUI.Presentation
{
    /// <summary>仅供生成器识别属性同步契约的占位接口。</summary>
    public interface ISynchronizeProperties
    {
        void Synchronize();
    }
}

namespace FUI.Rendering
{
    /// <summary>仅供生成器识别渲染 Element 契约的占位接口。</summary>
    public interface IElement { }
}

namespace FUI.Infrastructure.Binding
{
    /// <summary>仅供生成器输出非泛型绑定上下文描述的占位类型。</summary>
    public class ContextDescriptor { }

    /// <summary>仅供生成器输出强类型绑定上下文描述的占位类型。</summary>
    public class ContextDescriptor<TViewModel> : ContextDescriptor where TViewModel : FUI.Binding.ObservableObject { }

    /// <summary>仅供生成器输出命令绑定描述的占位类型。</summary>
    public class CommandBindingDescriptor { }

    /// <summary>仅供生成器输出属性绑定描述的占位类型。</summary>
    public class PropertyBindingDescriptor { }
}
