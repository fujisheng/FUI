using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    /// <summary>
    /// 视图工厂
    /// </summary>
    public interface IViewFactory
    {
        /// <summary>
        /// 创建一个视图
        /// </summary>
        /// <param name="viewName">视图名</param>
        /// <returns></returns>
        IView Create(string viewName);
        /// <summary>
        /// 异步创建一个视图
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        Task<IView> CreateAsync(string viewName, CancellationToken token);
    }
}
