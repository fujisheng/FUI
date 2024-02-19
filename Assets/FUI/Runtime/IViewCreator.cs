using FUI.Bindable;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FUI
{
    /// <summary>
    /// 界面创建参数
    /// </summary>
    public struct UIBuildParam
    {
        /// <summary>
        /// 界面名字
        /// </summary>
        public string viewName;

        /// <summary>
        /// 界面类型
        /// </summary>
        public Type viewType;

        /// <summary>
        /// 指定的ViewModel类型
        /// </summary>
        public Type viewModelType;

        /// <summary>
        /// 指定的ViewBehavior类型
        /// </summary>
        public Type viewBehaviorType;

        public UIBuildParam(string viewName, Type viewModelType = null, Type viewBehaviorType = null)
        {
            this.viewName = viewName;
            this.viewType = null;
            this.viewModelType = viewModelType;
            this.viewBehaviorType = viewBehaviorType;
        }

        public UIBuildParam(string viewName, Type viewType)
        {
            this.viewName = viewName;
            this.viewType = viewType;
            this.viewModelType = null;
            this.viewBehaviorType = null;
        }
    }

    /// <summary>
    /// 界面构造器
    /// </summary>
    public interface IUIBuilder
    {
        /// <summary>
        /// 创建一个界面
        /// </summary>
        /// <param name="param">界面参数</param>
        /// <param name="viewModel">返回的ViewModel</param>
        /// <param name="behavior">返回的ViewBehavior</param>
        /// <returns>要打开的View</returns>
        IView BuildView(UIBuildParam param, out ObservableObject viewModel, out ViewBehavior behavior);

        /// <summary>
        /// 构建一个界面
        /// </summary>
        /// <param name="param">界面参数</param>
        /// <param name="viewModel">viewModel实例</param>
        /// <returns></returns>
        IView BuildView(UIBuildParam param, ObservableObject viewModel);

        /// <summary>
        /// 异步创建一个界面
        /// </summary>
        /// <param name="param">界面参数</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>要打开的View</returns>
        Task<(IView view, ObservableObject viewModel, ViewBehavior behavior)> BuildViewAsync(UIBuildParam param, CancellationToken cancellationToken);
    }
}
