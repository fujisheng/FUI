# Red Dot Sample · 曙光远征

一个原创科幻RTS主题、独立运行的功能样例。导入Sample，打开 `Scenes/RedDotQuickStart.unity`，进入Play Mode。无需导入TMP Essentials，不连接服务器，不依赖ArtOfTD业务。

## 页面与玩法

七个页面分别拥有独立Prefab、ViewModel、Presenter和Route；主界面六个入口打开对应页面，返回恢复缓存页面。追踪终端是第八个独立Prefab，使用Overlay层，关闭即释放。

| 页面 | 推荐操作 | 观察结果 |
|---|---|---|
| 指挥中心 | 断开连接 → 切换档案 | 默认红点先隐藏再重连；远征不受账号切换影响。Home使用Any，仅表示有无事务 |
| 物资仓库 | 合金5 → 撤回运输 → 选择数据核心 → 关闭运输 | 合金5→3；单Key撤回保留核心1，整Source注销后核心0。点击接收运输可恢复链路 |
| 行动任务 | 切换追踪 → 完成目标 → 领取奖励 | 两个Rule独立贡献；忽略Hint的DirectOnly；共享奖励在仓库和任务入口同时消失 |
| 单位整备 | 选第二个单位 → 训练 → 补料 → 固定追踪 → 遣散 → 关闭终端 | 双条件满足才亮；选中对象热切Key；最后一个订阅退出后，零值动态节点才回收 |
| 通讯中心 | 选择邮件 → 阅读/归档；断开最高威胁链路 | long动态Key；Max(2,5,3)从5降到3；链路自检演示各阶段故障隔离 |
| 远征舰队 | 切换舰队 → 断开 → 撤销分队编制 → 调动分队 | A/B/分队/默认账号互相隔离；空Scope不穿透；最近父级与组件销毁后重新解析 |
| 战术推演 | 重复下令 → 推进批次；批量下令 → 推进批次 | 1000次失效合为一次计算；1000个不同Rule分别计算。结果来自实际计量 |
| 追踪终端 | 从物资/单位/任务页固定，返回并切页 | 终端继续跟踪原Key；自己的订阅和动画不依赖页面是否显示 |

各页“信号详情”默认收起，展开后查看真实Key、聚合器、贡献、边、Dirty、BatchVersion、订阅数、预期与实际值。只读快照按诊断版本缓存，不增加观察订阅。动态节点不存在时明确显示“尚未实例化或已回收”。

主界面“结束基地值守”批量处理当前档案的待办，使Any从1变成0，但Reader仍在线；进入通讯中心接收新邮件后重新变为1。独立远征舰队不受影响。它与“断开连接”是两种不同场景。

仓库的“数量预设”依次设置0、1、2、99、100，便于对照Dot、Count、Auto、99+和上限9。分类及Count展示固定观察合金，右上角Auto观察当前选中物资；选择核心时不应误认为这些位置仍是同一个Key。

## 数据怎么流动

`GameFacts` 只保存游戏事实与变化事件，不引用RedDot。`GameSignals` 中的Rule拉取事实、Source转发事实变化；注册句柄分别管理每个贡献者。`SampleContext` 是明确的游戏命令与导航编排端口，ViewModel只通过FUI Command调用它、通过强类型Key绑定显示。

`Bootstrap` 负责组合根和生命周期。普通操作不调用Flush；默认Center由Runtime在LateUpdate提交，局部舰队由所有者Tick。只有初始化、测试辅助和战术推演“推进批次”显式Flush。关闭时先释放View订阅，再Shutdown桥接，最后由所有者释放各Center。

示例Catalog在 `Scripts/Nodes.cs`。Sum沿直接边累加，因此共享奖励1经过两个入口，上层计为2个入口，不是2份奖励；Home使用Any，避免把物资数量与威胁值相加。

## 推演与故障

战术推演使用独立局部Center：10000个节点、1000个Rule；重复失效、批量失效、同值提交、回调下一批、递归Flush拒绝、饱和计数分别可操作。首次打开构造与预热，不算入Flush测量；每批统计计算数、通知数、耗时、托管分配。诊断、文字刷新和截图都在测量区间外。这里的实测值不是所有设备的性能承诺。

通讯故障注入是样例自检，不是真实网络。Rule异常保留自身上次贡献，聚合器异常保留节点状态，订阅异常不阻断其他回调；Source.Start失败回滚，Stop重入发布被拒绝。点击恢复链路后重新提交，最近32条历史故障仍可在详情查看。历史记录不等于当前故障仍未恢复。

## Unity适配与资源边界

Sample使用专用 `RtsRedDotElement` 的Legacy Text适配，免TMP资源导入；真实TMP标签由包的UGUI PlayMode测试验证。`ApplyState()` 的轻微脉冲只改变本Prefab视觉，不改Count。原生RedDotElement仍支持Inspector配置TMP、显示模式和上限。

FUI CLI生成静态UGUI/FUI层级，Sample在创建View前装配RedDotElement与Scope。每个Prefab拥有自己的视觉对象，不使用全局红点对象池。诊断长文本的顶部对齐和动态滚动高度由Sample专用组件完成。

分队整体换父级采用隐藏 → 实际reparent → 恢复的生命周期，不依赖Unity向孙级组件发送父级变化回调。Runtime和Scope不拥有Center。

原创图片按整体设计 → 独立资产 → HTML → 固定DOM提取 → `ui.web_to_ugui_prefab` 生成流程制作。设计母图不进入Prefab；文本、红点、列表项和按钮均为实际UGUI控件。ArtV2与ArtV3是生成批次，使用一致设计语言。未引入商业游戏截图、角色或标志；AI生成记录不是绝对的法律保证。

包内 `Documentation~/sample-coverage.md` 保存能力矩阵，`Documentation~/sample-design` 保存整体设计、提示词、布局与资产清单，`Documentation~/sample-verification` 保存测试结果和Unity实际截图。
