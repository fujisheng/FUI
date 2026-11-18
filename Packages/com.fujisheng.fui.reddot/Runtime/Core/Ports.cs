using System;

namespace FUI.RedDot
{
    /// <summary>由框架交给拉取规则的失效端口，只负责把规则加入下一批计算。</summary>
    public interface IRedDotInvalidator
    {
        /// <summary>声明当前规则的贡献可能已经变化。</summary>
        void Invalidate();
    }

    /// <summary>
    /// 拉取型贡献规则。Start 与 Stop 必须成对调用。
    /// </summary>
    public interface IRedDotRule
    {
        /// <summary>开始监听业务状态；保存 invalidator 以便后续声明失效。</summary>
        void Start(IRedDotInvalidator invalidator);

        /// <summary>在 Flush 中计算当前贡献；同一批最多调用一次。</summary>
        RedDotValue Evaluate();

        /// <summary>停止监听并释放对 invalidator 的引用。</summary>
        void Stop();
    }

    /// <summary>由框架分配给推送来源的身份隔离发布端口。</summary>
    public interface IRedDotPublisher
    {
        /// <summary>提交或替换当前来源对指定 RedDotKey 的待处理贡献。</summary>
        void Publish(RedDotKey key, RedDotValue value);

        /// <summary>移除当前来源对指定 RedDotKey 的贡献，不影响其他来源。</summary>
        void Remove(RedDotKey key);
    }

    /// <summary>
    /// 推送型贡献来源。一个来源可以发布多个 RedDotKey，但不会覆盖其他来源。
    /// </summary>
    public interface IRedDotSource
    {
        /// <summary>连接框架分配的发布端口。</summary>
        void Start(IRedDotPublisher publisher);

        /// <summary>停止发布并释放发布端口。</summary>
        void Stop();
    }

    /// <summary>面向 UI 和项目代码的只读查询与精确订阅接口。</summary>
    public interface IRedDotReader
    {
        /// <summary>读取最近一次成功提交的状态。</summary>
        RedDotState Get(RedDotKey key);

        /// <summary>订阅单个 RedDotKey；立即回放当前状态并返回幂等取消句柄。</summary>
        IDisposable Subscribe(RedDotKey key, Action<RedDotState> callback);
    }
}
