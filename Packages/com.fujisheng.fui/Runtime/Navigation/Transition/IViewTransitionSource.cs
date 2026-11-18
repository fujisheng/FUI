namespace FUI.Transitions
{
    /// <summary>由视图组合提供的过渡，导航和项目 Provider 不需要知道表现后端。</summary>
    public interface IViewTransitionSource
    {
        ITransition EnterTransition { get; }
        ITransition ExitTransition { get; }
    }
}
