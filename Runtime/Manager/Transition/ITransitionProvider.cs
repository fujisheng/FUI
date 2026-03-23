namespace FUI.Manager
{
    /// <summary>
    /// UI过渡提供器接口
    /// </summary>
    public interface ITransitionProvider
    {
        ITransition Get(IView view);
    }
}