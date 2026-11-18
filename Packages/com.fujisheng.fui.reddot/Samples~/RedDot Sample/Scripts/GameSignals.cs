using System;
using System.Collections.Generic;
using FUI.RedDot;

namespace FUI.Samples.RedDot
{
    /// <summary>拉取适配器只在注册期间监听事实，不在业务事件中计算红点。</summary>
    public sealed class FactRule : IRedDotRule
    {
        readonly CountFact fact;
        IRedDotInvalidator invalidator;
        /// <summary>实际计算次数，供推演和测试读取。</summary>
        public int Evaluations { get; private set; }
        /// <summary>绑定一个独立事实。</summary>
        public FactRule(CountFact fact) { this.fact = fact ?? throw new ArgumentNullException(nameof(fact)); }
        /// <summary>开始监听。</summary>
        public void Start(IRedDotInvalidator value) { invalidator = value; fact.Changed += Invalidate; }
        /// <summary>批次执行时才查询事实；故障由Center隔离。</summary>
        public RedDotValue Evaluate()
        {
            Evaluations++;
            if (fact.Unavailable) { throw new InvalidOperationException("Simulated sensor read failure."); }
            return new RedDotValue(fact.Count);
        }
        /// <summary>解除事实监听。</summary>
        public void Stop() { fact.Changed -= Invalidate; invalidator = null; }
        void Invalidate() => invalidator?.Invalidate();
    }

    /// <summary>单位的多条件规则。</summary>
    sealed class UnitUpgradeRule : IRedDotRule
    {
        readonly UnitFacts facts;
        IRedDotInvalidator invalidator;
        public UnitUpgradeRule(UnitFacts facts) { this.facts = facts; }
        public void Start(IRedDotInvalidator value) { invalidator = value; facts.Changed += Invalidate; }
        public RedDotValue Evaluate() => new RedDotValue(facts.CanUpgrade ? 1 : 0);
        public void Stop() { facts.Changed -= Invalidate; invalidator = null; }
        void Invalidate() => invalidator?.Invalidate();
    }

    /// <summary>物流推送适配器。首次连接回放事实，停止后不再接收业务变化。</summary>
    sealed class CargoSource : IRedDotSource
    {
        readonly CargoFacts facts;
        IRedDotPublisher publisher;
        public CargoSource(CargoFacts facts) { this.facts = facts; }
        public void Start(IRedDotPublisher value)
        {
            publisher = value;
            facts.Changed += Publish;
            foreach (var item in facts.Items) { Publish(item.Key, item.Value); }
        }
        public void Stop() { facts.Changed -= Publish; publisher = null; }
        void Publish(string key, int? count)
        {
            if (count.HasValue) { publisher.Publish(Nodes.Item(key), new RedDotValue(count.Value)); }
            else { publisher.Remove(Nodes.Item(key)); }
        }
    }

    /// <summary>一个游戏档案的事实与独立红点组合；持有并释放自己的中心。</summary>
    public sealed class GameSession : IDisposable
    {
        readonly List<IDisposable> registrations = new List<IDisposable>();
        readonly Dictionary<int, IDisposable> unitRegistrations = new Dictionary<int, IDisposable>();
        readonly Dictionary<int, UnitFacts> units = new Dictionary<int, UnitFacts>();
        readonly Queue<RedDotDiagnostic> history = new Queue<RedDotDiagnostic>();
        readonly List<long> messageIds = new List<long>();
        readonly MemorySource messages = new MemorySource();
        readonly MemorySource rewards = new MemorySource();
        readonly MemorySource linkInputs = new MemorySource();
        readonly MemorySource[] threatSources = { new MemorySource(), new MemorySource(), new MemorySource() };
        readonly IDisposable[] threatRegistrations = new IDisposable[3];
        IDisposable transportRegistration;
        IDisposable secondQuestRegistration;
        IDisposable brokenSubscriber;
        long nextMessage = 9000000000L;
        int nextUnit = 1003;
        bool disposed;

        /// <summary>只供组合根、机制说明和测试使用；页面只拿Reader。</summary>
        public RedDotCenter Center { get; } = new RedDotCenter();
        /// <summary>运输事实。</summary>
        public CargoFacts Transport { get; } = new CargoFacts();
        /// <summary>生产事实。</summary>
        public CargoFacts Production { get; } = new CargoFacts();
        /// <summary>主任务事实。</summary>
        public CountFact QuestA { get; } = new CountFact();
        /// <summary>支线任务事实。</summary>
        public CountFact QuestB { get; } = new CountFact();
        /// <summary>任务提示事实，不增加可领奖数。</summary>
        public CountFact QuestHint { get; } = new CountFact();
        /// <summary>通讯传感器事实。</summary>
        public CountFact Sensor { get; } = new CountFact();
        /// <summary>两条任务规则的实际计算计数。</summary>
        public FactRule QuestRuleA { get; }
        /// <summary>第二条任务规则。</summary>
        public FactRule QuestRuleB { get; }
        /// <summary>当前选中的单位，无选中时为0。</summary>
        public int SelectedUnit { get; private set; } = 1001;
        /// <summary>当前单位事实。</summary>
        public UnitFacts Unit => units.TryGetValue(SelectedUnit, out var unit) ? unit : null;
        /// <summary>当前选中邮件。</summary>
        public long SelectedMessage { get; private set; }
        /// <summary>实际收件箱记录，不是写死在Prefab里的消息条目。</summary>
        public System.Collections.Generic.IReadOnlyList<long> MessageIds => messageIds;
        /// <summary>第二条规则是否在监听。</summary>
        public bool Tracking => secondQuestRegistration != null;
        /// <summary>运输是否连接。</summary>
        public bool TransportConnected => transportRegistration != null;
        /// <summary>有界历史，不因后续成功计算清空。</summary>
        public IEnumerable<RedDotDiagnostic> History => history;
        /// <summary>Source.Start向调用方抛出的最近一次自检失败；不是Core推送的诊断事件。</summary>
        public string LastSourceStartupFailure { get; private set; }
        /// <summary>Sample注册实例的业务名称，不冒充Core贡献快照中的唯一身份。</summary>
        public string OwnerLabels => "运输链路 CargoSource / 制造链路 CargoSource；主任务 FactRule / 支线任务 FactRule";

        /// <summary>创建确定性的内存档案。所有初始事实由适配器发布，不由页面写节点。</summary>
        public GameSession(int profile = 0)
        {
            Nodes.Register(Center);
            Center.DiagnosticReported += Record;
            Transport.Set("alloy", 2 + profile);
            Transport.Set("core", 1);
            Production.Set("alloy", 3);
            ConnectTransport();
            Own(Center.RegisterSource(new CargoSource(Production)));
            QuestA.Set(1); QuestB.Set(1); QuestHint.Set(4); Sensor.Set(2);
            QuestRuleA = new FactRule(QuestA); QuestRuleB = new FactRule(QuestB);
            Own(Center.RegisterRule(Nodes.Quests, QuestRuleA));
            secondQuestRegistration = Center.RegisterRule(Nodes.Quests, QuestRuleB);
            Own(Center.RegisterRule(Nodes.QuestHint, new FactRule(QuestHint)));
            Own(Center.RegisterRule(Nodes.Link, new FactRule(Sensor)));
            Own(Center.RegisterSource(messages)); Own(Center.RegisterSource(rewards)); Own(Center.RegisterSource(linkInputs));
            for (var i = 0; i < 3; i++)
            {
                threatRegistrations[i] = Center.RegisterSource(threatSources[i]);
                threatSources[i].Publish(Nodes.Threat, i == 0 ? 2 : i == 1 ? 5 : 3);
            }
            SetSharedReward(true);
            AddUnit(1001); AddUnit(1002); AddUnit(1003);
            units[1001].Train(); units[1001].Supply();
            ReceiveMessage(); ReceiveMessage(); ReceiveMessage();
            rewards.Publish(Nodes.Season, 3 + profile);
            Center.Flush();
        }

        /// <summary>恢复物流连接，会回放断开期间累积的事实。</summary>
        public void ConnectTransport()
        {
            if (transportRegistration == null) { transportRegistration = Center.RegisterSource(new CargoSource(Transport)); }
        }
        /// <summary>注销整条物流链，只移除它的贡献。</summary>
        public void DisconnectTransport() { transportRegistration?.Dispose(); transportRegistration = null; }
        /// <summary>切换支线任务规则。</summary>
        public void ToggleTracking()
        {
            if (secondQuestRegistration == null) { secondQuestRegistration = Center.RegisterRule(Nodes.Quests, QuestRuleB); }
            else { secondQuestRegistration.Dispose(); secondQuestRegistration = null; }
        }
        /// <summary>领取任务和共享奖励，只改变对应事实贡献。</summary>
        public void ClaimRewards() { QuestA.Set(0); QuestB.Set(0); SetSharedReward(false); }
        /// <summary>结束基地值守，处理当前档案的全部待办；不触碰独立远征世界，不直接覆盖节点。</summary>
        public void CompleteWatch()
        {
            Recover(); ClaimRewards(); QuestHint.Set(0); Sensor.Set(0);
            Transport.Remove("alloy"); Transport.Remove("core"); Production.Remove("alloy");
            foreach (var unit in units.Values) { if (unit.CanUpgrade) { unit.Upgrade(); } }
            foreach (var id in messageIds) { messages.Publish(Nodes.Message(id), 0); }
            foreach (var source in threatSources) { source.Remove(Nodes.Threat); }
            linkInputs.Remove(Nodes.Link); rewards.Remove(Nodes.Season);
        }
        /// <summary>同一奖励只有一个来源，两个父入口通过DAG共享。</summary>
        public void SetSharedReward(bool available) => rewards.Publish(Nodes.SharedReward, available ? 1 : 0);
        /// <summary>选择实际存在的单位；热切换由VM更新Key。</summary>
        public void SelectUnit(int id) { SelectedUnit = units.ContainsKey(id) ? id : 0; }
        /// <summary>招募新单位并注册独立规则。</summary>
        public void Recruit() { SelectedUnit = ++nextUnit; AddUnit(SelectedUnit); }
        /// <summary>遣散时移除规则；尚被终端订阅的零值节点仍会保留。</summary>
        public void Discharge()
        {
            if (!unitRegistrations.TryGetValue(SelectedUnit, out var handle)) { return; }
            handle.Dispose(); unitRegistrations.Remove(SelectedUnit); units.Remove(SelectedUnit); SelectedUnit = 0;
        }
        /// <summary>接收新邮件，长整型Key由工厂构造。</summary>
        public void ReceiveMessage()
        {
            SelectedMessage = ++nextMessage;
            messageIds.Insert(0, SelectedMessage);
            messages.Publish(Nodes.Message(SelectedMessage), 1);
        }
        /// <summary>选择收件箱中的现有邮件；选择不意味着已读。</summary>
        public void SelectMessage(int index)
        {
            if (index >= 0 && index < messageIds.Count) { SelectedMessage = messageIds[index]; }
        }
        /// <summary>阅读保留邮件但清除未读事实。</summary>
        public void ReadMessage() { if (SelectedMessage != 0) { messages.Publish(Nodes.Message(SelectedMessage), 0); } }
        /// <summary>归档移除来源记录。</summary>
        public void ArchiveMessage()
        {
            if (SelectedMessage == 0) { return; }
            messages.Remove(Nodes.Message(SelectedMessage)); messageIds.Remove(SelectedMessage);
            SelectedMessage = messageIds.Count == 0 ? 0 : messageIds[0];
        }
        /// <summary>断开最高威胁链路，其余两条不变。</summary>
        public void DisconnectHighestThreat() { threatRegistrations[1]?.Dispose(); threatRegistrations[1] = null; }
        /// <summary>演示Rule故障，健康Source仍可改变节点。</summary>
        public void FailRule() { Sensor.SetUnavailable(true); linkInputs.Publish(Nodes.Link, 3); }
        /// <summary>演示聚合失败保留该节点上次状态。</summary>
        public void FailAggregator() => linkInputs.Publish(Nodes.LinkFault, 1);
        /// <summary>订阅者单点失败不影响页面和其他终端。</summary>
        public void FailSubscriber()
        {
            brokenSubscriber?.Dispose();
            brokenSubscriber = Center.Subscribe(Nodes.Link, state => { throw new InvalidOperationException("Simulated terminal callback failure."); });
        }
        /// <summary>恢复链路，不抹除历史故障。</summary>
        public void Recover()
        {
            Sensor.SetUnavailable(false); linkInputs.Remove(Nodes.LinkFault); brokenSubscriber?.Dispose(); brokenSubscriber = null;
        }
        /// <summary>故意失败的来源启动会向调用者抛出；在Sample内记录自检结果。</summary>
        public string TestSourceStart()
        {
            try { Center.RegisterSource(new FaultSource(true)); }
            catch (InvalidOperationException exception)
            {
                LastSourceStartupFailure = exception.Message;
                return exception.Message;
            }
            throw new InvalidOperationException("Expected the source start failure.");
        }
        /// <summary>停止失败由框架隔离，发布器已先失效。</summary>
        public void TestSourceStop() { Center.RegisterSource(new FaultSource(false)).Dispose(); }
        void AddUnit(int id)
        {
            var facts = new UnitFacts(id); units.Add(id, facts);
            unitRegistrations.Add(id, Center.RegisterRule(Nodes.HeroUpgrade(id), new UnitUpgradeRule(facts)));
        }
        void Own(IDisposable value) => registrations.Add(value);
        void Record(RedDotDiagnostic diagnostic)
        {
            if (history.Count == 32) { history.Dequeue(); }
            history.Enqueue(diagnostic);
        }
        /// <summary>释放监听后释放Center；Runtime和Scope不参与所有权。</summary>
        public void Dispose()
        {
            if (disposed) { return; }
            disposed = true;
            brokenSubscriber?.Dispose(); DisconnectTransport(); secondQuestRegistration?.Dispose();
            foreach (var item in unitRegistrations.Values) { item.Dispose(); }
            foreach (var item in threatRegistrations) { item?.Dispose(); }
            for (var i = registrations.Count - 1; i >= 0; i--) { registrations[i].Dispose(); }
            Center.DiagnosticReported -= Record;
            Center.Dispose();
        }
    }

    /// <summary>只用于通讯自检的失败来源，不能用于正常游戏数据。</summary>
    sealed class FaultSource : IRedDotSource
    {
        readonly bool failStart;
        IRedDotPublisher publisher;
        public FaultSource(bool failStart) { this.failStart = failStart; }
        public void Start(IRedDotPublisher value)
        {
            publisher = value; publisher.Publish(Nodes.Link, new RedDotValue(9));
            if (failStart) { throw new InvalidOperationException("Simulated source start failure; registration rolled back."); }
        }
        public void Stop()
        {
            if (!failStart) { publisher.Publish(Nodes.Link, new RedDotValue(99)); }
        }
    }
}
