using FUI.Rendering;

namespace FUI.Transitions
{
    /// <summary>
    /// UI过渡提供器接口
    /// </summary>
    public interface ITransitionProvider
    {
        ITransition Get(IView view);
    }
}
