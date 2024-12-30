using FUI.Bindable;

using System;

namespace FUI
{
    internal interface IBindingContext<out TObservableObject> where TObservableObject : ObservableObject
    {
        internal IView View { get; }
        internal Type ViewModelType { get; }
        internal TObservableObject ViewModel { get; }
        internal bool UpdateView(IView view);
        internal bool UpdateViewModel(ObservableObject viewModel);
        internal void Binding();
        internal void Unbinding();
    }
}