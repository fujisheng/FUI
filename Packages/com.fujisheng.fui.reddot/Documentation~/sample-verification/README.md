# Sample 验收记录 · 2026-09-05

验证工程：FUI/Library/FUITestProject，Unity 6000.3.22f1，macOS Editor，GameView 1920×1080。没有接入ArtOfTD Client业务，没有发布或推送。

## 自动化门禁

| 门禁 | 结果 | 证据 |
|---|---|---|
| RedDot Generator | 9/9 | 生成器测试结果；RDT0001–RDT0008逐ID断言，正例同时编译生成源码 |
| Core EditMode | 22/22 | [EditMode.xml](EditMode.xml)；含线程、复制、DAG、生命周期、脏子图和候选清理 |
| UGUI与Sample PlayMode | 33/33 | [PlayMode.xml](PlayMode.xml)；框架8、事实/适配10、Sample页面与场景15 |
| 包结构与仓库依赖环 | 通过 | tools/verify-repository.sh --structure-only；2个包通过 |
| Generator源码与产物 | 通过 | FUI.RedDot.Generators~/verify.sh；三份RedDot DLL SHA-256一致 |
| Unity编译 | 通过 | refresh后editor.status.isCompiling=false；Console无编译错误 |

共64个自动测试通过，不将“64个测试”误称为行覆盖率100%。没有启用行覆盖率采集。

为保留正在使用的独立工程，未执行会删除并重建工程的verify-package.sh完整批处理；其门禁分别通过现有工程的Unity Test Runner、独立Generator验证脚本和结构检查执行。测试不是dotnet build替代Unity编译。

RedDot源码构建/包/Sample DLL SHA-256均为：

`2bdc1b6a130aaca5ce1e9f7b8cd43e5cad9380213298b8009c941a8fea575a7d`

Sample使用与当前Core一致的FUI.Generators.dll：

`5c720475b95d5f39413256f75d8495dd59ff9526f2d7ac462a40fe31f33ba643`

本轮没有修改FUI Core Generator源码或其产物。仓库中原有的Core导航/Generator未提交变更保持原样。

## 真实运行走查

从RedDotQuickStart场景进入Play，使用ui_click_element逐步点击；两轮走查共177次工具调用，包含60次实际UI点击，均返回isOccluded=false。每步回读View或实际Text/active状态，不通过注入ViewModel或直接打开Route替代交互。完整记录见 [live-clicks.json](live-clicks.json)。第二轮额外验证在线状态下Any真正归零，并重新截图主界面的新增操作。

- 指挥中心断开后Home视觉隐藏，连接后恢复。
- 在线“结束基地值守”使Home Count=0；远征仍为2；接收新邮件后Home重新为1，区别于离线隐藏。
- 仓库合金5，撤回运输后页面和独立终端同为3；返回时终端仍存在，关闭终端后仅主页面存在。
- 任务共享奖励入口数2；领取后共享奖励视觉关闭。
- 单位1002仅训练时不亮，补齐材料后亮。
- 邮件已读后提醒关闭；威胁5→3；五类故障操作与恢复可执行，历史故障可读。
- 舰队A=2→B=5；断开后外层隐藏；取消分队Scope后不穿透；移到默认区域后为3；返回主页舰队入口也保持断开。
- 推演待提交可见；推进后结果来自实际Flush；同值计算无通知、回调失效下一批、递归拒绝和int.MaxValue均实际回读。
- 普通流程及故障演示结束时Console错误列表为空。Core故障由诊断接口隔离，框架测试预期的LogException另由LogAssert明确断言；没有用清空Console掩盖异常。

## 本机推演数据

| 场景 | Rule计算 | 通知 | 单次Flush | 托管分配 |
|---|---:|---:|---:|---:|
| 同Rule失效1000次 | 1 | 2 | 2.582 ms | 0 B |
| 1000个不同Rule | 1000 | 1001 | 4.893 ms | 0 B |
| 1000个Rule原值重算 | 1000 | 0 | 0.415 ms | 0 B |

这是带Editor与CLI的本机单次数据，不是跨设备性能保证。通知包含根节点；测量区间只包含Flush，不含文字、快照与截图。Core测试另验证只重算脏节点及祖先；候选清理检查量不是整个删除过程的总工作量。

## 视觉证据

以下都是实际Unity GameView捕获，未用Web截图或设计母图代替：

| 页面 | 运行图 | 附加状态 |
|---|---|---|
| 指挥中心 | [GameHomeView](screenshots/GameHomeView.png) | [断开](screenshots/HomeOffline.png)、[在线无提醒](screenshots/HomeNoReminders.png) |
| 物资仓库 | [BagView](screenshots/BagView.png) | [独立追踪终端](screenshots/TrackedStatusView.png) |
| 行动任务 | [QuestView](screenshots/QuestView.png) | [信号详情](screenshots/QuestDetails.png) |
| 单位整备 | [HeroView](screenshots/HeroView.png) | 重装单位选中、双条件满足 |
| 通讯中心 | [MailView](screenshots/MailView.png) | [故障历史](screenshots/MailFaults.png) |
| 远征舰队 | [SeasonView](screenshots/SeasonView.png) | [边界与调动](screenshots/ScopeBoundary.png) |
| 战术推演 | [TacticalView](screenshots/TacticalView.png) | [待提交](screenshots/TacticalPending.png)、[重复失效](screenshots/TacticalRepeated.png)、[饱和](screenshots/TacticalSaturation.png) |

目视检查发现并修正了红点数字偏左和详情文本在滚动视口外的问题；详情增加顶部对齐、真实内容高度以及布局回归测试。艺术母图是构图参考，不承诺逐像素复刻。当前适配以1920×1080为基准，未宣称完成所有宽高比、所有平台的视觉验收。

## 能力定位

完整矩阵见 [sample-coverage.md](../sample-coverage.md)，运行步骤见Sample README。开源使用边界：原创AI生成图有提示词与资产清单，没有引入商业游戏截图、标志或角色；此记录不是绝对的法律保证。
