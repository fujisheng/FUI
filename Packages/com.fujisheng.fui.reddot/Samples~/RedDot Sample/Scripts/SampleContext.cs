using System;
using System.Collections.Generic;
using System.Text;
using FUI.Navigation;
using FUI.RedDot;
using FUI.RedDot.UGUI;

namespace FUI.Samples.RedDot
{
    /// <summary>页面分类，不参与Key构造。</summary>
    public enum SamplePage { Home, Bag, Quest, Hero, Mail, Season, Tactical, Tracked }

    /// <summary>局部远征世界的所有者，Scope只消费Reader。</summary>
    sealed class FleetWorld : IDisposable
    {
        public static readonly Guid ExpeditionId = new Guid("28ff6134-9eb7-4a6e-870d-b7142b6b6a21");
        readonly MemorySource source = new MemorySource();
        readonly IDisposable registration;
        int stage = 1;
        public RedDotCenter Center { get; } = new RedDotCenter();
        public FleetWorld(int count)
        {
            Nodes.Register(Center); registration = Center.RegisterSource(source);
            source.Publish(Nodes.Expedition(ExpeditionId), count); Center.Flush();
        }
        public void Advance() => source.Publish(Nodes.ExpeditionStage(ExpeditionId, stage++), 1);
        public void Claim()
        {
            source.Remove(Nodes.Expedition(ExpeditionId));
            for (var i = 1; i < stage; i++) { source.Remove(Nodes.ExpeditionStage(ExpeditionId, i)); }
        }
        public void Dispose() { registration.Dispose(); Center.Dispose(); }
    }

    /// <summary>Sample编排端口；普通动作改事实，不Flush。诊断只查询，不额外订阅节点。</summary>
    sealed class SampleContext : IDisposable
    {
        readonly GameSession[] profiles = { new GameSession(), new GameSession(1) };
        readonly FleetWorld[] fleets = { new FleetWorld(2), new FleetWorld(5) };
        readonly FleetWorld wing = new FleetWorld(7);
        readonly StringBuilder description = new StringBuilder(1024);
        readonly Dictionary<IRedDotDiagnostics, SnapshotCache> snapshots = new Dictionary<IRedDotDiagnostics, SnapshotCache>();
        sealed class SnapshotCache
        {
            public ulong Version;
            public RedDotNodeSnapshot[] Nodes;
        }
        Navigator navigator;
        ViewHandle terminal;
        TacticalSimulation simulation;
        int profile;
        int fleet;
        int preset;
        bool connected;
        bool disposed;
        public event Action Changed;
        public GameSession Game => profiles[profile];
        public bool FleetConnected { get; private set; } = true;
        public bool WingAttached { get; private set; } = true;
        public bool WingEnabled { get; private set; } = true;
        public int WingGeneration { get; private set; }
        public IRedDotReader FleetReader => FleetConnected ? fleets[fleet].Center : null;
        public IRedDotReader WingReader => wing.Center;
        public IRedDotReader DefaultReader => connected ? Game.Center : null;
        public TacticalSimulation Simulation => simulation ?? (simulation = new TacticalSimulation());
        public RedDotKey TrackedKey { get; private set; } = Nodes.Item("alloy");
        public string LastAction { get; private set; } = "选择功能入口处理基地事务。";
        public string SelectedItem { get; private set; } = "alloy";
        public string ItemName => SelectedItem == "core" ? "数据核心" : "合金补给";
        public string ProfileName => $"档案 {(profile == 0 ? "曙光" : "极光")} · {(connected ? "已连接" : "离线")}";
        public string FleetName => $"舰队 {(fleet == 0 ? "A" : "B")} · {(FleetConnected ? "已连接" : "离线")}";
        public void AttachNavigator(Navigator value) => navigator = value;
        public void Open(Route route) { navigator.Open(route, this); Refresh(); }
        public void Back() { navigator.Back(); Refresh(); }
        public void Connect() { connected = true; RedDotRuntime.Configure(Game.Center); Refresh(); }
        public void Disconnect() { connected = false; RedDotRuntime.Shutdown(); Refresh(); }
        public void SwitchProfile() { profile = 1 - profile; Connect(); }
        public void Track(SamplePage page)
        {
            // 固定当时的Key，遣散单位后终端的观察订阅仍保留，直到用户关闭。
            TrackedKey = Key(page);
            if (TrackedKey == default) { TrackedKey = Nodes.Home; }
            terminal = navigator.Open(Routes.TrackedStatusView, this); Refresh();
        }
        public void CloseTerminal() { navigator.Close(terminal); terminal = default; Refresh(); }
        public RedDotKey Key(SamplePage page)
        {
            switch (page)
            {
                case SamplePage.Bag: return Nodes.Item(SelectedItem);
                case SamplePage.Quest: return Nodes.Quests;
                case SamplePage.Hero: return Game.SelectedUnit == 0 ? default : Nodes.HeroUpgrade(Game.SelectedUnit);
                case SamplePage.Mail: return Game.SelectedMessage == 0 ? default : Nodes.Message(Game.SelectedMessage);
                case SamplePage.Season: return Nodes.Season;
                case SamplePage.Tactical: return Simulation.Root;
                case SamplePage.Tracked: return TrackedKey;
                default: return Nodes.Home;
            }
        }
        public IRedDotReader Reader(SamplePage page)
        {
            if (page == SamplePage.Season) { return FleetReader; }
            if (page == SamplePage.Tactical) { return Simulation.Center; }
            return DefaultReader;
        }
        /// <summary>明确的游戏命令白名单，不接受任意Key写入。</summary>
        public void Execute(SamplePage page, string action)
        {
            LastAction = action;
            switch (action)
            {
                case "Connect": Connect(); break;
                case "Disconnect": Disconnect(); break;
                case "Profile": SwitchProfile(); break;
                case "CompleteWatch": Game.CompleteWatch(); break;
                case "SelectAlloy": SelectedItem = "alloy"; break;
                case "SelectCore": SelectedItem = "core"; break;
                case "Transport": Game.ConnectTransport(); Game.Transport.Set("alloy", 2); Game.Transport.Set("core", 1); break;
                case "Produce": Game.Production.Set("alloy", 3); break;
                case "Withdraw": Game.Transport.Remove(SelectedItem); break;
                case "StopTransport": Game.DisconnectTransport(); break;
                case "Use": Game.Transport.Remove(SelectedItem); Game.Production.Remove(SelectedItem); break;
                case "Preset":
                    SelectedItem = "alloy";
                    var values = new[] { 0, 1, 2, 99, 100 }; Game.Transport.Remove("alloy");
                    Game.Production.Set("alloy", values[preset++ % values.Length]); break;
                case "Complete": Game.QuestA.Set(1); Game.QuestB.Set(1); break;
                case "Claim": Game.ClaimRewards(); break;
                case "Tracking": Game.ToggleTracking(); break;
                case "Hint": Game.QuestHint.Set(Game.QuestHint.Count == 0 ? 4 : 0); break;
                case "Shared": Game.SetSharedReward(true); break;
                case "Unit1": Game.SelectUnit(1001); break;
                case "Unit2": Game.SelectUnit(1002); break;
                case "Unit3": Game.SelectUnit(1003); break;
                case "Train": Game.Unit?.Train(); break;
                case "Supply": Game.Unit?.Supply(); break;
                case "Upgrade": Game.Unit?.Upgrade(); break;
                case "Recruit": Game.Recruit(); break;
                case "Discharge": Game.Discharge(); break;
                case "Receive": Game.ReceiveMessage(); break;
                case "Message1": Game.SelectMessage(0); break;
                case "Message2": Game.SelectMessage(1); break;
                case "Message3": Game.SelectMessage(2); break;
                case "Read": Game.ReadMessage(); break;
                case "Archive": Game.ArchiveMessage(); break;
                case "Threat": Game.DisconnectHighestThreat(); break;
                case "RuleFault": Game.FailRule(); break;
                case "AggregateFault": Game.FailAggregator(); break;
                case "CallbackFault": Game.FailSubscriber(); break;
                case "StartFault": LastAction = Game.TestSourceStart(); break;
                case "StopFault": Game.TestSourceStop(); break;
                case "Recover": Game.Recover(); break;
                case "Fleet": fleet = 1 - fleet; FleetConnected = true; break;
                case "FleetConnection": FleetConnected = !FleetConnected; break;
                case "WingMove": WingAttached = !WingAttached; break;
                case "WingScope": WingEnabled = !WingEnabled; WingGeneration++; break;
                case "Advance": fleets[fleet].Advance(); break;
                case "ClaimFleet": fleets[fleet].Claim(); break;
                case "Deploy": _ = Simulation; break;
                case "Repeated": Simulation.QueueRepeated(); break;
                case "Distinct": Simulation.QueueDistinct(); break;
                case "Unchanged": Simulation.QueueUnchanged(); break;
                case "Wave": Simulation.QueueNextWave(); break;
                case "Recursive": Simulation.QueueRecursive(); break;
                case "Saturation": Simulation.QueueSaturation(); break;
                case "Step": Simulation.Step(); break;
                case "Continuous": Simulation.Continuous = !Simulation.Continuous; break;
                default: throw new ArgumentException("Unknown sample action: " + action, nameof(action));
            }
            Refresh();
        }
        public string Status(SamplePage page)
        {
            switch (page)
            {
                case SamplePage.Home: return ProfileName + (Game.Center.Get(Nodes.Home).Count > 0 ? "\n有新的基地事务等待处理。" : "\n当前基地事务已全部处理。") + "\n请选择下方部门，远征舰队独立运行。";
                case SamplePage.Bag: return $"{ItemName}：运输 {Game.Transport.Get(SelectedItem)} ＋ 生产 {Game.Production.Get(SelectedItem)}\n运输：{(Game.TransportConnected ? "连接" : "离线，待重新确认清单")}\n已确认待处理：{Game.Center.Get(Nodes.Item(SelectedItem)).Count}\n数据核心待处理：{Game.Center.Get(Nodes.Item("core")).Count}";
                case SamplePage.Quest: return $"主任务 {Game.QuestA.Count} / 支线 {Game.QuestB.Count}\n支线追踪：{Game.Tracking}\n两入口共享一份奖励，按边统计不是奖励翻倍。";
                case SamplePage.Hero: return Game.Unit == null ? "未选中单位。招募后可以训练和补料。" : $"单位 {Game.SelectedUnit}\n训练：{Game.Unit.Trained}　材料：{Game.Unit.Supplied}\n两项同时满足才可升级。";
                case SamplePage.Mail: return $"邮件 {Game.SelectedMessage}\n最高威胁：{Game.Center.Get(Nodes.Threat).Count}　链路：{Game.Center.Get(Nodes.Link).Count}\n传感器故障：{Game.Sensor.Unavailable}";
                case SamplePage.Season: return FleetName + $"\n分队：{(WingAttached ? "编入舰队" : "直属指挥部")}\n{(WingEnabled ? "独立编制" : "使用上级编制")}，不同编制分别结算。";
                case SamplePage.Tactical:
                    var s = Simulation;
                    return $"节点 {s.Center.NodeCount}　待提交 {s.Center.HasPendingChanges}\n本批计算 {s.Evaluations}　通知 {s.Notifications}\nFlush {s.Milliseconds:F3} ms　GC {s.AllocatedBytes} B\n原子读取 {s.AtomicRead}　递归拒绝 {s.RecursiveRejected}\n连续推进 {s.Continuous}";
                default: return TrackedKey.Value + "\n独立 View / 独立订阅";
            }
        }
        public string Details(SamplePage page)
        {
            if (!(Reader(page) is IRedDotDiagnostics diagnostics)) { return "当前无 Reader。Scope 是显式边界，不回退默认账号。"; }
            var key = Key(page);
            description.Clear(); description.AppendLine(Status(page)); description.AppendLine("操作：" + LastAction);
            description.AppendLine(Expectation(page));
            // 只在详情展开且诊断版本改变时复制快照；观察不会创建订阅或延长动态节点寿命。
            if (!snapshots.TryGetValue(diagnostics, out var cache) || cache.Version != diagnostics.DiagnosticsVersion)
            {
                cache = new SnapshotCache { Version = diagnostics.DiagnosticsVersion, Nodes = diagnostics.GetDiagnosticsSnapshot() };
                snapshots[diagnostics] = cache;
            }
            var found = false;
            foreach (var node in cache.Nodes)
            {
                if (node.Key != key) { continue; }
                found = true;
                description.AppendLine($"Key: {node.Key.Value}\n{node.Aggregator} / Count {node.State.Count} / Batch {node.State.BatchVersion}");
                description.AppendLine($"Dirty {node.IsDirty} / Rules {node.RuleCount} / Subscribers {node.SubscriberCount} / Dynamic {node.IsDynamic}");
                foreach (var parent in node.Parents) { description.AppendLine("父 " + parent.Value); }
                for (var i = 0; i < Math.Min(node.Children.Length, 6); i++) { description.AppendLine("子 " + node.Children[i].Value); }
                if (node.Children.Length > 6) { description.AppendLine($"共 {node.Children.Length} 个子节点；完整图见 Debugger。"); }
                foreach (var item in node.Contributions) { description.AppendLine($"直接贡献 {item.Owner}: {item.Count}"); }
            }
            if (!found) { description.AppendLine("节点尚未实例化，或已因没有持有者而回收。"); }
            if (page == SamplePage.Bag || page == SamplePage.Quest) { description.AppendLine(Game.OwnerLabels); }
            if (page == SamplePage.Mail)
            {
                if (Game.LastSourceStartupFailure != null) { description.AppendLine("历史自检（Sample / Source.Start）：" + Game.LastSourceStartupFailure); }
                foreach (var error in Game.History) { description.AppendLine($"历史 {error.Kind} / {error.Owner} / batch {error.BatchVersion}: {error.Exception.Message}"); }
            }
            return description.ToString();
        }
        static string Expectation(SamplePage page)
        {
            switch (page)
            {
                case SamplePage.Home: return "预期：Home按Any仅返回0/1；断开隐藏默认账号，舰队不受影响。";
                case SamplePage.Bag: return "预期：合金运输2+生产3=5；撤回合金仍留核心1；关闭运输后核心也清零。所有显示模式只改变外观。";
                case SamplePage.Quest: return "预期：两个Rule各1，停止支线只剩1。DirectOnly不计Hint；共享奖励1经两个入口计2，领取一次清除两处。";
                case SamplePage.Hero: return "预期：训练与材料同时满足才贡献1。切换Key解除旧订阅；遣散移除Rule，终端关闭后零值节点才可回收。";
                case SamplePage.Mail: return "预期：Max(2,5,3)=5，最高链路移除后3。Rule失败保留旧贡献，聚合失败保留旧节点，回调失败不阻断其他观察者。恢复不会删除历史故障。";
                case SamplePage.Season: return "预期：初始舰队A=2/B=5/独立分队=7/默认账号=3。空Scope不穿透；撤销分队Scope后读取最近上级。";
                case SamplePage.Tactical: return "预期：1000次重复失效计算1次；1000个不同Rule计算1000次。同批BatchVersion相同，回调失效下一批生效；同值不通知。耗时与分配仅测Flush。";
                default: return "预期：终端与页面共享Key但独立订阅；隐藏页面不影响终端，关闭终端只释放自己的订阅。";
            }
        }
        public void Refresh() => Changed?.Invoke();
        public void TickLocalWorlds()
        {
            foreach (var world in fleets) { world.Center.Tick(); }
            wing.Center.Tick();
            if (simulation != null && simulation.Continuous) { simulation.Step(); }
        }
        public void FlushForTests() { Game.Center.Flush(); TickLocalWorlds(); Refresh(); }
        public void Dispose()
        {
            if (disposed) { return; } disposed = true; Changed = null; snapshots.Clear();
            simulation?.Dispose(); wing.Dispose();
            foreach (var world in fleets) { world.Dispose(); }
            foreach (var world in profiles) { world.Dispose(); }
        }
    }
}
