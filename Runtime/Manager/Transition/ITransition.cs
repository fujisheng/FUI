using System.Threading;
using System.Threading.Tasks;

namespace FUI.Manager
{
    /// <summary>
    /// UI过渡接口
    /// </summary>
    public interface ITransition
    {
        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 播放过渡
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns></returns>
        ValueTask PlayAsync(CancellationToken ct);

        /// <summary>
        /// 强制停止
        /// </summary>
        void Stop();
    }
}
