using FUI.RedDot;

namespace FUI.Samples.RedDot
{
    /// <summary>以内存整数模拟业务侧拉取规则，数值变化后只负责声明失效。</summary>
    public sealed class MemoryCountRule : IRedDotRule
    {
        IRedDotInvalidator invalidator;
        int count;

        public void Start(IRedDotInvalidator owner)
        {
            invalidator = owner;
        }

        public RedDotValue Evaluate()
        {
            return new RedDotValue(count);
        }

        public void Stop()
        {
            invalidator = null;
        }

        public void SetCount(int value)
        {
            count = value < 0 ? 0 : value;
            invalidator?.Invalidate();
        }
    }

    /// <summary>以内存调用模拟推送来源，只能通过框架分配的 Publisher 发布贡献。</summary>
    public sealed class MemorySource : IRedDotSource
    {
        IRedDotPublisher publisher;

        public void Start(IRedDotPublisher value)
        {
            publisher = value;
        }

        public void Stop()
        {
            publisher = null;
        }

        public void Publish(RedDotKey key, int count)
        {
            publisher?.Publish(key, new RedDotValue(count < 0 ? 0 : count));
        }

        /// <summary>只移除本来源在指定Key上的贡献。</summary>
        public void Remove(RedDotKey key) => publisher?.Remove(key);
    }
}
