using System;
using FUI.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.RedDot;
using FUI.RedDot.UGUI;
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;

namespace FUI.Samples.RedDot
{
    /// <summary>指挥中心：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("GameHomeView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class GameHomeViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("HomeBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        [Bind("HeroNavBadge", nameof(RedDotElement.Key))]
        public RedDotKey HeroNavBadge => Nodes.Units;
        [Bind("BagNavBadge", nameof(RedDotElement.Key))]
        public RedDotKey BagNavBadge => Nodes.Bag;
        [Bind("QuestNavBadge", nameof(RedDotElement.Key))]
        public RedDotKey QuestNavBadge => Nodes.Operations;
        [Bind("MailNavBadge", nameof(RedDotElement.Key))]
        public RedDotKey MailNavBadge => Nodes.Mail;
        [Bind("SeasonNavBadge", nameof(RedDotElement.Key))]
        public RedDotKey SeasonNavBadge => Nodes.Season;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Home);
            Status = context.Status(SamplePage.Home);
            if (DetailsVisible) { Details = context.Details(SamplePage.Home); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("TrackButton", nameof(ButtonElement.OnClick))]
        public void Track() => context.Track(SamplePage.Home);
        [Command("ConnectButton", nameof(ButtonElement.OnClick))]
        public void Connect() => context.Execute(SamplePage.Home, "Connect");
        [Command("DisconnectButton", nameof(ButtonElement.OnClick))]
        public void Disconnect() => context.Execute(SamplePage.Home, "Disconnect");
        [Command("ProfileButton", nameof(ButtonElement.OnClick))]
        public void Profile() => context.Execute(SamplePage.Home, "Profile");
        [Command("CompleteWatchButton", nameof(ButtonElement.OnClick))]
        public void CompleteWatch() => context.Execute(SamplePage.Home, "CompleteWatch");
        [Command("BagButton", nameof(ButtonElement.OnClick))]
        public void OpenBag() => context.Open(Routes.BagView);
        [Command("QuestButton", nameof(ButtonElement.OnClick))]
        public void OpenQuest() => context.Open(Routes.QuestView);
        [Command("HeroButton", nameof(ButtonElement.OnClick))]
        public void OpenHero() => context.Open(Routes.HeroView);
        [Command("MailButton", nameof(ButtonElement.OnClick))]
        public void OpenMail() => context.Open(Routes.MailView);
        [Command("SeasonButton", nameof(ButtonElement.OnClick))]
        public void OpenSeason() => context.Open(Routes.SeasonView);
        [Command("TacticalButton", nameof(ButtonElement.OnClick))]
        public void OpenTactical() => context.Open(Routes.TacticalView);
    }

    /// <summary>指挥中心生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class HomePresenter : Presenter<GameHomeViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>物资仓库：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("BagView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class BagViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("CargoSlot0", nameof(ImageElement.Color))]
        UnityEngine.Color alloyTint;
        [ObservableProperty]
        [Bind("CargoSlot1", nameof(ImageElement.Color))]
        UnityEngine.Color coreTint;
        [ObservableProperty]
        [Bind("BagItemBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        [Bind("BagDotBadge", nameof(RedDotElement.Key))]
        public RedDotKey BagDotBadge => Nodes.Item("alloy");
        [Bind("BagCountBadge", nameof(RedDotElement.Key))]
        public RedDotKey BagCountBadge => Nodes.Item("alloy");
        [Bind("BagCompactBadge", nameof(RedDotElement.Key))]
        public RedDotKey BagCompactBadge => Nodes.Item("alloy");
        [Bind("SharedBadge", nameof(RedDotElement.Key))]
        public RedDotKey SharedBadge => Nodes.SharedReward;
        [Command("CargoSlot0", nameof(ButtonElement.OnClick))]
        public void SelectAlloy() => context.Execute(SamplePage.Bag, "SelectAlloy");
        [Command("CargoSlot1", nameof(ButtonElement.OnClick))]
        public void SelectCore() => context.Execute(SamplePage.Bag, "SelectCore");
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Bag);
            AlloyTint = context.SelectedItem == "alloy" ? new UnityEngine.Color(0.65F, 1F, 1F) : UnityEngine.Color.white;
            CoreTint = context.SelectedItem == "core" ? new UnityEngine.Color(0.65F, 1F, 1F) : UnityEngine.Color.white;
            Status = context.Status(SamplePage.Bag);
            if (DetailsVisible) { Details = context.Details(SamplePage.Bag); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("TrackButton", nameof(ButtonElement.OnClick))]
        public void Track() => context.Track(SamplePage.Bag);
        [Command("TransportButton", nameof(ButtonElement.OnClick))]
        public void Transport() => context.Execute(SamplePage.Bag, "Transport");
        [Command("ProduceButton", nameof(ButtonElement.OnClick))]
        public void Produce() => context.Execute(SamplePage.Bag, "Produce");
        [Command("WithdrawButton", nameof(ButtonElement.OnClick))]
        public void Withdraw() => context.Execute(SamplePage.Bag, "Withdraw");
        [Command("StopTransportButton", nameof(ButtonElement.OnClick))]
        public void StopTransport() => context.Execute(SamplePage.Bag, "StopTransport");
        [Command("UseButton", nameof(ButtonElement.OnClick))]
        public void Use() => context.Execute(SamplePage.Bag, "Use");
        [Command("PresetButton", nameof(ButtonElement.OnClick))]
        public void Preset() => context.Execute(SamplePage.Bag, "Preset");
    }

    /// <summary>物资仓库生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class BagPresenter : Presenter<BagViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>行动任务：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("QuestView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class QuestViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("QuestClaimBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        [Bind("HintBadge", nameof(RedDotElement.Key))]
        public RedDotKey HintBadge => Nodes.QuestHint;
        [Bind("SharedBadge", nameof(RedDotElement.Key))]
        public RedDotKey SharedBadge => Nodes.SharedReward;
        [Bind("ReadinessBadge", nameof(RedDotElement.Key))]
        public RedDotKey ReadinessBadge => Nodes.RewardRoutes;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Quest);
            Status = context.Status(SamplePage.Quest);
            if (DetailsVisible) { Details = context.Details(SamplePage.Quest); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("TrackButton", nameof(ButtonElement.OnClick))]
        public void Track() => context.Track(SamplePage.Quest);
        [Command("CompleteButton", nameof(ButtonElement.OnClick))]
        public void Complete() => context.Execute(SamplePage.Quest, "Complete");
        [Command("ClaimButton", nameof(ButtonElement.OnClick))]
        public void Claim() => context.Execute(SamplePage.Quest, "Claim");
        [Command("TrackingButton", nameof(ButtonElement.OnClick))]
        public void Tracking() => context.Execute(SamplePage.Quest, "Tracking");
        [Command("HintButton", nameof(ButtonElement.OnClick))]
        public void Hint() => context.Execute(SamplePage.Quest, "Hint");
        [Command("SharedButton", nameof(ButtonElement.OnClick))]
        public void Shared() => context.Execute(SamplePage.Quest, "Shared");
    }

    /// <summary>行动任务生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class QuestPresenter : Presenter<QuestViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>单位整备：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("HeroView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class HeroViewModel : ViewModel
    {
        SampleContext context;
        UnityEngine.Sprite[] portraits;
        [ObservableProperty]
        [Bind("UnitShowcase", nameof(ImageElement.Sprite))]
        UnityEngine.Sprite unitArt;
        [Bind("UnitShowcase", nameof(ImageElement.PreserveAspect))]
        public bool PreserveAspect => true;
        [ObservableProperty]
        [Bind("Unit1Button", nameof(ImageElement.Color))]
        UnityEngine.Color unit1Tint;
        [ObservableProperty]
        [Bind("Unit2Button", nameof(ImageElement.Color))]
        UnityEngine.Color unit2Tint;
        [ObservableProperty]
        [Bind("Unit3Button", nameof(ImageElement.Color))]
        UnityEngine.Color unit3Tint;
        [ObservableProperty]
        [Bind("HeroUpgradeBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Hero);
            if (portraits == null)
            {
                portraits = new[]
                {
                    UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/ArtV3/MechShowcase"),
                    UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/ArtV3/HeavyMechIcon"),
                    UnityEngine.Resources.Load<UnityEngine.Sprite>("UI/ArtV3/EngineerMechIcon"),
                };
            }
            var selected = context.Game.SelectedUnit;
            UnitArt = portraits[selected == 1002 ? 1 : selected == 1003 ? 2 : 0];
            var active = new UnityEngine.Color(0.7F, 1F, 1F);
            Unit1Tint = selected == 1001 ? active : UnityEngine.Color.white;
            Unit2Tint = selected == 1002 ? active : UnityEngine.Color.white;
            Unit3Tint = selected == 1003 ? active : UnityEngine.Color.white;
            Status = context.Status(SamplePage.Hero);
            if (DetailsVisible) { Details = context.Details(SamplePage.Hero); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("TrackButton", nameof(ButtonElement.OnClick))]
        public void Track() => context.Track(SamplePage.Hero);
        [Command("Unit1Button", nameof(ButtonElement.OnClick))]
        public void Unit1() => context.Execute(SamplePage.Hero, "Unit1");
        [Command("Unit2Button", nameof(ButtonElement.OnClick))]
        public void Unit2() => context.Execute(SamplePage.Hero, "Unit2");
        [Command("Unit3Button", nameof(ButtonElement.OnClick))]
        public void Unit3() => context.Execute(SamplePage.Hero, "Unit3");
        [Command("TrainButton", nameof(ButtonElement.OnClick))]
        public void Train() => context.Execute(SamplePage.Hero, "Train");
        [Command("SupplyButton", nameof(ButtonElement.OnClick))]
        public void Supply() => context.Execute(SamplePage.Hero, "Supply");
        [Command("UpgradeButton", nameof(ButtonElement.OnClick))]
        public void Upgrade() => context.Execute(SamplePage.Hero, "Upgrade");
        [Command("RecruitButton", nameof(ButtonElement.OnClick))]
        public void Recruit() => context.Execute(SamplePage.Hero, "Recruit");
        [Command("DischargeButton", nameof(ButtonElement.OnClick))]
        public void Discharge() => context.Execute(SamplePage.Hero, "Discharge");
    }

    /// <summary>单位整备生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class HeroPresenter : Presenter<HeroViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>通讯中心：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("MailView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class MailViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("Message1ButtonText", nameof(TextElement.Text))]
        string message1;
        [ObservableProperty]
        [Bind("Message2ButtonText", nameof(TextElement.Text))]
        string message2;
        [ObservableProperty]
        [Bind("Message3ButtonText", nameof(TextElement.Text))]
        string message3;
        [ObservableProperty]
        [Bind("MailItemBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        [Bind("ThreatBadge", nameof(RedDotElement.Key))]
        public RedDotKey ThreatBadge => Nodes.Threat;
        [Bind("LinkBadge", nameof(RedDotElement.Key))]
        public RedDotKey LinkBadge => Nodes.Link;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Mail);
            Message1 = MessageTitle(0);
            Message2 = MessageTitle(1);
            Message3 = MessageTitle(2);
            Status = context.Status(SamplePage.Mail);
            if (DetailsVisible) { Details = context.Details(SamplePage.Mail); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("TrackButton", nameof(ButtonElement.OnClick))]
        public void Track() => context.Track(SamplePage.Mail);
        [Command("ReceiveButton", nameof(ButtonElement.OnClick))]
        public void Receive() => context.Execute(SamplePage.Mail, "Receive");
        string MessageTitle(int index) => index < context.Game.MessageIds.Count
            ? "前线通讯 " + context.Game.MessageIds[index] : "暂无通讯";
        [Command("Message1Button", nameof(ButtonElement.OnClick))]
        public void SelectMessage1() => context.Execute(SamplePage.Mail, "Message1");
        [Command("Message2Button", nameof(ButtonElement.OnClick))]
        public void SelectMessage2() => context.Execute(SamplePage.Mail, "Message2");
        [Command("Message3Button", nameof(ButtonElement.OnClick))]
        public void SelectMessage3() => context.Execute(SamplePage.Mail, "Message3");
        [Command("ReadButton", nameof(ButtonElement.OnClick))]
        public void Read() => context.Execute(SamplePage.Mail, "Read");
        [Command("ArchiveButton", nameof(ButtonElement.OnClick))]
        public void Archive() => context.Execute(SamplePage.Mail, "Archive");
        [Command("ThreatButton", nameof(ButtonElement.OnClick))]
        public void Threat() => context.Execute(SamplePage.Mail, "Threat");
        [Command("RuleFaultButton", nameof(ButtonElement.OnClick))]
        public void RuleFault() => context.Execute(SamplePage.Mail, "RuleFault");
        [Command("AggregateFaultButton", nameof(ButtonElement.OnClick))]
        public void AggregateFault() => context.Execute(SamplePage.Mail, "AggregateFault");
        [Command("CallbackFaultButton", nameof(ButtonElement.OnClick))]
        public void CallbackFault() => context.Execute(SamplePage.Mail, "CallbackFault");
        [Command("StartFaultButton", nameof(ButtonElement.OnClick))]
        public void StartFault() => context.Execute(SamplePage.Mail, "StartFault");
        [Command("StopFaultButton", nameof(ButtonElement.OnClick))]
        public void StopFault() => context.Execute(SamplePage.Mail, "StopFault");
        [Command("RecoverButton", nameof(ButtonElement.OnClick))]
        public void Recover() => context.Execute(SamplePage.Mail, "Recover");
    }

    /// <summary>通讯中心生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class MailPresenter : Presenter<MailViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>远征舰队：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("SeasonView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class SeasonViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("SeasonRewardBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        [Bind("WingBadge", nameof(RedDotElement.Key))]
        public RedDotKey WingBadge => Nodes.Season;
        [Bind("DefaultFleetBadge", nameof(RedDotElement.Key))]
        public RedDotKey DefaultFleetBadge => Nodes.Season;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Season);
            Status = context.Status(SamplePage.Season);
            if (DetailsVisible) { Details = context.Details(SamplePage.Season); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("FleetButton", nameof(ButtonElement.OnClick))]
        public void Fleet() => context.Execute(SamplePage.Season, "Fleet");
        [Command("FleetConnectionButton", nameof(ButtonElement.OnClick))]
        public void FleetConnection() => context.Execute(SamplePage.Season, "FleetConnection");
        [Command("WingMoveButton", nameof(ButtonElement.OnClick))]
        public void WingMove() => context.Execute(SamplePage.Season, "WingMove");
        [Command("WingScopeButton", nameof(ButtonElement.OnClick))]
        public void WingScope() => context.Execute(SamplePage.Season, "WingScope");
        [Command("AdvanceButton", nameof(ButtonElement.OnClick))]
        public void Advance() => context.Execute(SamplePage.Season, "Advance");
        [Command("ClaimFleetButton", nameof(ButtonElement.OnClick))]
        public void ClaimFleet() => context.Execute(SamplePage.Season, "ClaimFleet");
    }

    /// <summary>远征舰队生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class SeasonPresenter : Presenter<SeasonViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>战术推演：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("TacticalView")]
    [RoutePolicy(Layer.Panel, CoverageMode = CoverageMode.Hide, CacheMode = CacheMode.KeepAlive)]
    public partial class TacticalViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("SimulationBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Tactical);
            Status = context.Status(SamplePage.Tactical);
            if (DetailsVisible) { Details = context.Details(SamplePage.Tactical); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.Back();
        [Command("DeployButton", nameof(ButtonElement.OnClick))]
        public void Deploy() => context.Execute(SamplePage.Tactical, "Deploy");
        [Command("RepeatedButton", nameof(ButtonElement.OnClick))]
        public void Repeated() => context.Execute(SamplePage.Tactical, "Repeated");
        [Command("DistinctButton", nameof(ButtonElement.OnClick))]
        public void Distinct() => context.Execute(SamplePage.Tactical, "Distinct");
        [Command("StepButton", nameof(ButtonElement.OnClick))]
        public void Step() => context.Execute(SamplePage.Tactical, "Step");
        [Command("ContinuousButton", nameof(ButtonElement.OnClick))]
        public void Continuous() => context.Execute(SamplePage.Tactical, "Continuous");
        [Command("UnchangedButton", nameof(ButtonElement.OnClick))]
        public void Unchanged() => context.Execute(SamplePage.Tactical, "Unchanged");
        [Command("WaveButton", nameof(ButtonElement.OnClick))]
        public void Wave() => context.Execute(SamplePage.Tactical, "Wave");
        [Command("RecursiveButton", nameof(ButtonElement.OnClick))]
        public void Recursive() => context.Execute(SamplePage.Tactical, "Recursive");
        [Command("SaturationButton", nameof(ButtonElement.OnClick))]
        public void Saturation() => context.Execute(SamplePage.Tactical, "Saturation");
    }

    /// <summary>战术推演生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class TacticalPresenter : Presenter<TacticalViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

    /// <summary>追踪终端：Command只操作事实；Key由标准FUI绑定精确切换。</summary>
    [ViewContract("TrackedStatusView")]
    [RoutePolicy(Layer.Overlay, HistoryMode = HistoryMode.Transient, CoverageMode = CoverageMode.KeepVisible, CacheMode = CacheMode.None)]
    public partial class TrackedStatusViewModel : ViewModel
    {
        SampleContext context;
        [ObservableProperty]
        [Bind("TrackedBadge", nameof(RedDotElement.Key))]
        RedDotKey badge;
        [ObservableProperty]
        [Bind("StatusText", nameof(TextElement.Text))]
        string status;
        [ObservableProperty]
        [Bind("DetailsText", nameof(TextElement.Text))]
        string details;
        [ObservableProperty]
        [Bind("DetailsPanel", nameof(RectTransformElement.Visible))]
        bool detailsVisible;
        internal void Configure(SampleContext value)
        {
            Detach(); context = value; context.Changed += Refresh; Refresh();
        }
        internal void Detach() { if (context != null) { context.Changed -= Refresh; } }
        void Refresh()
        {
            Badge = context.Key(SamplePage.Tracked);
            Status = context.Status(SamplePage.Tracked);
            if (DetailsVisible) { Details = context.Details(SamplePage.Tracked); }
        }
        [Command("DetailsButton", nameof(ButtonElement.OnClick))]
        public void ToggleDetails() { DetailsVisible = !DetailsVisible; Refresh(); }
        [Command("BackButton", nameof(ButtonElement.OnClick))]
        public void Back() => context.CloseTerminal();
    }

    /// <summary>追踪终端生命周期，关闭时解除机制说明监听，缓存重新打开再连接。</summary>
    public sealed class TrackedPresenter : Presenter<TrackedStatusViewModel>
    {
        protected override void OnOpen(object param) => VM.Configure((SampleContext)param);
        protected override void OnClose() => VM.Detach();
        protected override void OnDestroy() => VM.Detach();
    }

}
