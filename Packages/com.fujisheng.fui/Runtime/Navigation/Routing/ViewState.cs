namespace FUI.Navigation
{
    /// <summary>
    /// 单个 Handle 的完整生命周期状态。Covered 与 Cached 均不等同于关闭。
    /// </summary>
    public enum ViewState
    {
        /// <summary>已分配 Handle，尚未开始资源加载。</summary>
        Pending,
        /// <summary>正在获取 View 资源。</summary>
        Loading,
        /// <summary>实例已创建，正在执行打开生命周期或进入动画。</summary>
        Opening,
        /// <summary>页面已打开并处于活动状态。</summary>
        Open,
        /// <summary>被另一个显式页面覆盖，保留绑定和业务生命周期。</summary>
        Covered,
        /// <summary>已退出导航，正在执行关闭生命周期或退出动画。</summary>
        Closing,
        /// <summary>实例与资源仍保留，但当前 Handle 已失效。</summary>
        Cached,
        /// <summary>打开失败且已执行回滚。</summary>
        Failed,
        /// <summary>实例和资源已释放，或 Handle 从未存在。</summary>
        Destroyed,
    }
}
