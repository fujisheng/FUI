namespace FUI
{
    /// <summary>
    /// 空的绑定上下文
    /// </summary>
    public class EmptyBindingContext : BindingContext<EmptyViewModel>
    {
        public EmptyBindingContext(IView view, EmptyViewModel viewModel) : base(view, viewModel) { }

        protected override void OnBinding() { }

        protected override void OnUnbinding() { }
    }
}