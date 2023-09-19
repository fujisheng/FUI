using FUI.Bindable;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    /// <summary>
    /// 界面创建参数
    /// </summary>
    public struct ViewBuildParam
    {
        /// <summary>
        /// 界面名字
        /// </summary>
        public string viewName;

        /// <summary>
        /// 指定的ViewModel类型
        /// </summary>
        public Type viewModelType;

        /// <summary>
        /// 指定的ViewBehavior类型
        /// </summary>
        public Type viewBehaviorType;

        public ViewBuildParam(string viewName, Type viewModelType = null, Type viewBehaviorType = null)
        {
            this.viewName = viewName;
            this.viewModelType = viewModelType;
            this.viewBehaviorType = viewBehaviorType;
        }
    }

    /// <summary>
    /// 界面构造器
    /// </summary>
    public interface IViewBuilder
    {
        /// <summary>
        /// 创建一个界面
        /// </summary>
        /// <param name="param">界面参数</param>
        /// <param name="viewModel">返回的ViewModel</param>
        /// <param name="behavior">返回的ViewBehavior</param>
        /// <returns>要打开的View</returns>
        View BuildView(ViewBuildParam param, out ObservableObject viewModel, out ViewBehavior behavior);

        /// <summary>
        /// 异步创建一个界面
        /// </summary>
        /// <param name="param">界面参数</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <param name="viewModel">返回的ViewModel</param>
        /// <param name="behavior">返回的ViewBehavior</param>
        /// <returns>要打开的View</returns>
        Task<View> BuildViewAsync(ViewBuildParam param, CancellationToken cancellationToken, out ObservableObject viewModel, out ViewBehavior behavior);
    }
}
