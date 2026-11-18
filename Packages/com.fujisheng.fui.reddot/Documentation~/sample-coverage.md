# RTS Sample 能力覆盖矩阵

本表是功能覆盖清单，不是代码行覆盖率。当前执行结果见 [验收记录](sample-verification/README.md)。普通操作仅修改事实，自动帧末提交；战术推演是唯一由用户推进批次的局部世界。

| ID | 页面 / 操作 | 事实与适配 | Key / 预期 | 验证 |
|---|---|---|---|---|
| R01 | 指挥中心：连接、断开、换档案 | 独立档案所有者 | Home / Any 0或1；断开隐藏 | Runtime生命周期 |
| R02 | 仓库：运输2、生产3、撤回运输 | 两个推送适配器 | Item(string) / 5→3 | 来源隔离 |
| R03 | 仓库：关闭运输 | 一个来源发布两个物资 | Item(alloy/core) / 仅运输贡献消失 | Remove与注销 |
| R04 | 任务：完成、领奖、停止追踪 | 两个独立Rule | Quests / 1+1；独立Stop | 多规则合批 |
| R05 | 任务：提示、共享奖励 | 提示事实、共享奖励事实 | DirectOnly忽略Hint；SharedReward两条父边 | 自定义与DAG |
| R06 | 单位：选中、训练、补料、升级 | 单位条件Rule | HeroUpgrade(int) / 热切换 | 动态生命周期 |
| R07 | 单位：招募、遣散 | 移除规则；视图自行释放订阅 | 无持有者且零值后回收 | 动态清理 |
| R08 | 通讯：收件、阅读、归档 | 邮件Source | Message(long) / 动态创建清理 | 动态参数 |
| R09 | 通讯：断开最高威胁链路 | 三个独立Source | Threat / Max(2,5,3)=5→3 | Max |
| R10 | 通讯：链路自检、恢复 | 故障Rule/聚合器/回调/Source | 各阶段隔离；历史不丢失 | 异常回滚 |
| R11 | 远征：换舰队、断开、嵌套分队 | 外层与内层Scope | 同Key不同Reader；空Scope不回退 | Scope优先级 |
| R12 | 远征：调动分队、撤销编制 | Transform父级与Scope销毁 | 最近Scope重解析 | Scope生命周期 |
| R13 | 远征：推进阶段 | Guid与Guid+int工厂 | Expedition / ExpeditionStage | Generator正例编译 |
| R14 | 推演：重复下令、推进批次 | 规则计数器，手动Tick | 1000次失效仅计算一次 | 批次 |
| R15 | 推演：连续执行、二次波次 | 回调触发下一次失效 | 原子提交、下一批重入、无变化不通知 | 批次隔离 |
| R16 | 推演：压力阵列 | 10000节点1000规则 | Flush耗时与分配实测 | 性能独立测量 |
| R17 | 仓库：数量预设 | 0/1/2/99/100 | Dot/Count/Auto/99+，上限9 | 显示模式 |
| R18 | 追踪终端：固定、关闭、换页 | 独立Overlay View | 同Key独立订阅，ApplyState脉冲 | 多View与释放 |
| R19 | 各页：展开信号详情 | 只读快照，不创建观察订阅 | 贡献、边、Dirty、BatchVersion、订阅 | 诊断真实性 |
| R20 | 源码与自动测试 | 不导入故意非法源码 | RDT0001–RDT0008，参数、环、不可变性、线程 | 编译期门禁 |

## 可执行断言定位

| 功能ID | 主要测试方法（位于Sample Tests，另注明框架测试） |
|---|---|
| R01 | InitialAnyAndLateRuntimeConfigurationProjectToHome；ShutdownHidesOldAccountAndReconnectRestoresEnabledElements；CompletingWatchMakesAnyZeroWithoutDisconnectingAndNewMailRestoresIt |
| R02–R03 | CargoSourcesKeepIdentityAndRemoveOnlyTheirOwnKeys；CargoSelectionExposesSingleKeyRemovalAndWholeSourceDisposal |
| R04 | RulesBatchAndStopIndependently |
| R05 | SharedRewardCountsTwoPathsAndCustomAggregationIgnoresHints |
| R06–R07 | UnitConditionsAndLastObserverControlDynamicLifetime；HotUnitKeySwitchAndDischargeRespectPinnedTerminalLifetime |
| R08–R09 | MailLongKeysAndMaxSourcesAreIndependent |
| R10 | FailuresRetainCorrectContributionAndRecoveryKeepsHistory；FailedSourceStartRollsBackAndStopCannotRepublish |
| R11–R12 | FleetScopeClearNestingReparentAndDestroyAreReal；CachedHomeFleetEntryRebindsAfterFleetSwitchAndDisconnect |
| R13 | FleetGuidAndCompositeFactoriesUseSeparateWorlds；Generator动态参数编译正例 |
| R14–R16 | TacticalMeasurementsUseTheActualBatchAndAtomicSnapshot；TacticalReentryIsNextBatchAndSaturationDoesNotOverflow；Core的FlushVisitsOnlyDirtyNodeAndItsAncestors、DynamicCleanupInspectsOnlyCandidates |
| R17 | AllVisualModesAndLimitsUseTheSameCommittedKey；原生TMP见包Tests/Runtime |
| R18 | TerminalAndPageHaveIndependentSubscriptionsAndCloseReleasesOnlyTerminal |
| R19 | SignalDetailsAreCollapsedAndDoNotCreateObservingSubscriptions；SignalDetailsStartInsideViewportAndGrowToRealTextHeight |
| R20 | Generator的CatalogGeneratorTests；Core的CatalogCopiesCallerOwnedArrays、PublicAccessIsRestrictedToCreatingThread、SumSaturatesAtMaximumInteger |

共享奖励使用专用RewardRoutes观察两条入口路径；不将仓库其他物资或任务数量混入“入口数”。Readiness仍是按边聚合的综合样例节点，不把它称为唯一奖励数量。

## 视觉执行约束

1920×1080；7个独立页面与1个独立追踪终端。先生成整页设计，再核对资源清单，复用匹配的原创ArtV2资源、生成缺失资源，最后走HTML→DOM→web_to_ugui_prefab。设计图不得成为整屏Sprite。动态文字使用UGUI Text；原生TMP行为由框架PlayMode测试覆盖。

## 边界

Sample不接服务器、网络、持久化或ArtOfTD业务。Source、Scope、Runtime均不取得Center所有权。性能测量不包含诊断快照、UI文字更新和截图；动态候选检查量不等于整个图删除流程的工作量。
