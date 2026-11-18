using System;
using System.Threading;
using System.Threading.Tasks;
using FUI.Navigation;
using FUI.Resources;
using FUI.Transitions;
using FUI.Rendering;

namespace FUI.Resources
{
    /// <summary>
    /// 单个资源的所有权凭证。Dispose 后资源是否立即卸载由具体 Provider 决定。
    /// </summary>
    public interface IAssetLease<out T> : IDisposable
    {
        /// <summary>
        /// Lease 当前持有的资源值。
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// 页面级资源作用域。释放 Scope 时必须兜底释放仍存活的全部子 Lease。
    /// </summary>
    public interface IAssetScope : IDisposable
    {
        /// <summary>同步加载资源，并把返回 Lease 登记到当前 Scope。</summary>
        IAssetLease<T> Load<T>(string key);

        /// <summary>异步加载资源，并把返回 Lease 登记到当前 Scope。</summary>
        ValueTask<IAssetLease<T>> LoadAsync<T>(string key, CancellationToken token);

        /// <summary>同步实例化资源，实例所有权属于返回 Lease。</summary>
        IAssetLease<T> Instantiate<T>(string key);

        /// <summary>异步实例化资源，实例所有权属于返回 Lease。</summary>
        ValueTask<IAssetLease<T>> InstantiateAsync<T>(string key, CancellationToken token);
    }

    /// <summary>
    /// 顶层 View 与其资源作用域的共同所有权；缓存期间该 Lease 必须保持存活。
    /// </summary>
    public interface IViewLease : IDisposable
    {
        /// <summary>已经创建但由 Navigator 控制启用状态的 View。</summary>
        IView View { get; }

        /// <summary>供当前 View 及其动态子节点共享的资源作用域。</summary>
        IAssetScope Assets { get; }
    }

    /// <summary>
    /// 仅包含预热资源的 Lease，不允许持有已实例化 View 或触发生命周期。
    /// </summary>
    public interface IPreloadLease : IDisposable
    {
        /// <summary>该预热资源对应的精确 Route 变体。</summary>
        RouteDescriptor Route { get; }
    }

    /// <summary>
    /// View 创建与预热后端。FUI 只管理 Lease，不依赖具体资源系统。
    /// </summary>
    public interface IViewProvider
    {
        /// <summary>同步创建 View，并在成功时接管匹配的预热 Lease；提供 Lease 时只能实例化已准备资源，不得重新加载。</summary>
        IViewLease Create(RouteDescriptor route, IPreloadLease preload = null);

        /// <summary>异步创建 View，并在成功时接管匹配的预热 Lease。</summary>
        ValueTask<IViewLease> CreateAsync(
            RouteDescriptor route,
            IPreloadLease preload,
            CancellationToken token);

        /// <summary>只预热 Route 资源，不实例化 View。</summary>
        ValueTask<IPreloadLease> PreloadAsync(
            RouteDescriptor route,
            CancellationToken token);
    }

    /// <summary>
    /// 需要阻塞外部输入的操作阶段。
    /// </summary>
    public enum OperationPhase
    {
        Open,
        Close,
        Preload,
    }

    /// <summary>
    /// 外部输入门。返回的 Lease 用于引用计数，FUI 不创建遮罩 Canvas。
    /// </summary>
    public interface IInputGate
    {
        /// <summary>
        /// 获取一次输入阻塞所有权；Navigator 保证在成功、失败和取消路径中释放。
        /// </summary>
        IDisposable Acquire(ViewHandle handle, OperationPhase phase);
    }
}
