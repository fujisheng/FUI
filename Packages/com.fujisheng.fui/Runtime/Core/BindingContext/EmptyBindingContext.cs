using FUI.Presentation;
using FUI.Rendering;

namespace FUI.Infrastructure.Binding
{
    /// <summary>
    /// 空的绑定上下文
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class EmptyBindingContext : BindingContext<EmptyViewModel>
    {
        public EmptyBindingContext(IView view, EmptyViewModel viewModel) : base(view, viewModel) { }

        protected override void OnBinding() { }

        protected override void OnUnbinding() { }
    }
}
