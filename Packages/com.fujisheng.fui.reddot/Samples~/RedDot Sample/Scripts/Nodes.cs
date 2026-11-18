using System;
using FUI.RedDot;

namespace FUI.Samples.RedDot
{
    /// <summary>Sample 的静态节点和动态 RedDotKey 工厂目录。</summary>
    [RedDotCatalog]
    public static partial class Nodes
    {
        [RedDotNode(Aggregator = typeof(AnyAggregator))]
        public static readonly RedDotKey Home = RedDotKey.Root("Home");

        [RedDotNode(nameof(Home))]
        public static readonly RedDotKey Mail = RedDotKey.Child(Home, "Mail");

        [RedDotNode(nameof(Home))]
        public static readonly RedDotKey Readiness = RedDotKey.Child(Home, "Readiness");

        [RedDotNode(nameof(Readiness))]
        public static readonly RedDotKey Bag = RedDotKey.Child(Readiness, "Bag");

        [RedDotNode(nameof(Readiness))]
        public static readonly RedDotKey Operations = RedDotKey.Child(Readiness, "Operations");

        [RedDotNode(nameof(Operations), Aggregator = typeof(DirectOnlyAggregator))]
        public static readonly RedDotKey Quests = RedDotKey.Child(Operations, "Quests");

        /// <summary>专门统计共享奖励的可达入口，不混入物资数量和任务数量。</summary>
        [RedDotNode(nameof(Home))]
        public static readonly RedDotKey RewardRoutes = RedDotKey.Child(Home, "RewardRoutes");

        [RedDotNode(nameof(Bag), nameof(RewardRoutes))]
        public static readonly RedDotKey WarehouseReward = RedDotKey.Child(Bag, "RewardEntry");

        [RedDotNode(nameof(Operations), nameof(RewardRoutes))]
        public static readonly RedDotKey TaskReward = RedDotKey.Child(Operations, "RewardEntry");

        [RedDotNode(nameof(WarehouseReward), nameof(TaskReward))]
        public static readonly RedDotKey SharedReward = RedDotKey.Child(WarehouseReward, "SharedReward");

        [RedDotNode(nameof(Home), Aggregator = typeof(MaxAggregator))]
        public static readonly RedDotKey Threat = RedDotKey.Child(Home, "Threat");

        [RedDotNode(nameof(Home), Aggregator = typeof(LinkHealthAggregator))]
        public static readonly RedDotKey Link = RedDotKey.Child(Home, "Link");

        [RedDotNode(nameof(Link))]
        public static readonly RedDotKey LinkFault = RedDotKey.Child(Link, "Fault");

        [RedDotNode(nameof(Home), Aggregator = typeof(AnyAggregator))]
        public static readonly RedDotKey Units = RedDotKey.Child(Home, "Units");

        [RedDotNode(nameof(Quests))]
        public static readonly RedDotKey QuestHint = RedDotKey.Child(Quests, "Hint");

        [RedDotNode(nameof(Home))]
        public static readonly RedDotKey Season = RedDotKey.Child(Home, "Season");

        [RedDotFactory(nameof(Units))]
        public static RedDotKey HeroUpgrade(int heroId)
        {
            return RedDotKey.Dynamic(Units, "HeroUpgrade", heroId);
        }

        /// <summary>物资标识保留原始字符串，由框架负责稳定编码。</summary>
        [RedDotFactory(nameof(Bag))]
        public static RedDotKey Item(string itemId) => RedDotKey.Dynamic(Bag, "Item", itemId);

        /// <summary>邮件使用长整型序号。</summary>
        [RedDotFactory(nameof(Mail))]
        public static RedDotKey Message(long messageId) => RedDotKey.Dynamic(Mail, "Message", messageId);

        /// <summary>远征实例使用独立 Guid。</summary>
        [RedDotFactory(nameof(Season))]
        public static RedDotKey Expedition(Guid id) => RedDotKey.Dynamic(Season, "Expedition", id);

        /// <summary>组合参数按声明顺序参与编码，不拼接字符串。</summary>
        [RedDotFactory(nameof(Season))]
        public static RedDotKey ExpeditionStage(Guid id, int stage) => RedDotKey.Dynamic(Season, "Stage", id, stage);
    }

    /// <summary>链路自检用无状态聚合器：故障输入非零时模拟设备解码失败。</summary>
    public sealed class LinkHealthAggregator : IRedDotAggregator
    {
        /// <summary>故障由事实输入控制，不在聚合器内保存状态。</summary>
        public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].Key == Nodes.LinkFault && inputs[i].Value.Count > 0)
                {
                    throw new InvalidOperationException("Simulated link decoder failure.");
                }
            }
            var count = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                count = int.MaxValue - count < inputs[i].Value.Count ? int.MaxValue : count + inputs[i].Value.Count;
            }
            return new RedDotValue(count);
        }
    }

    /// <summary>
    /// 示例自定义聚合：仅统计直接贡献，忽略子节点。
    /// </summary>
    public sealed class DirectOnlyAggregator : IRedDotAggregator
    {
        /// <summary>对直接贡献求和，并有意忽略所有子节点输入。</summary>
        public RedDotValue Aggregate(ReadOnlySpan<RedDotAggregationInput> inputs)
        {
            var count = 0;
            for (var index = 0; index < inputs.Length; index++)
            {
                if (inputs[index].Kind != RedDotInputKind.Direct) { continue; }
                var value = inputs[index].Value.Count;
                count = int.MaxValue - count < value ? int.MaxValue : count + value;
            }
            return new RedDotValue(count);
        }
    }
}
