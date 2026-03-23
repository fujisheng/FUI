using System;

namespace FUI
{
    public class BindingAttribute : Attribute { }

    public class ViewBindingAttribute : Attribute { }

    public class CommandAttribute : Attribute { }

    public interface IValueConverter { }

    public interface IValueConverter<T1, T2> { }

    public interface ISynchronizeProperties
    {
        void Synchronize();
    }

    public interface IElement { }
}

namespace FUI.Bindable
{
    public class ObservableObject { }
    public class BindableProperty<T> { }
    public interface IBindableProperty<T> { }
    public interface INotifyPropertyChanged { }
    public interface INotifyCollectionChanged { }
    public class CommandTemplate<T> { }
}

namespace FUI.BindingDescriptor
{
    public class ContextDescriptor { }

    public class ContextDescriptor<TViewModel> : ContextDescriptor where TViewModel : FUI.Bindable.ObservableObject { }

    public class CommandBindingDescriptor { }

    public class PropertyBindingDescriptor { }
}