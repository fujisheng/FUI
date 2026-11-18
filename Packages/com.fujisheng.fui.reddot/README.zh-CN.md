# FUI Red Dot

[English](README.md)

`FUI Red Dot` 是 FUI 的可选红点扩展。它通过强类型 RedDotKey、Source Generator Catalog、DAG 聚合和帧末合批，把红点计算与业务 ViewModel、Presenter 和 UI 视觉解耦。

核心边界：

- 业务通过中性的 `IRedDotRule` 或 `IRedDotSource` 提供贡献，不直接操作红点 GameObject。
- `FUI.RedDot.RedDotCenter` 统一计算和按 RedDotKey 精确通知，不进行全局广播。
- `FUI.RedDot.UGUI.RedDotElement` 只把 `RedDotState` 映射为 Dot、Count 或 Auto 视觉。
- 框架不包含网络权威、已读、确认、跨天和持久化语义。

领域入口保留 `RedDot` 前缀，避免与 FUI 和业务中的 `Element`、`Runtime`、`State` 冲突；具体算法使用 `SumAggregator / AnyAggregator / MaxAggregator` 等简洁名称。

安装、Catalog 写法和生命周期说明见[使用文档](Documentation~/index.md)。
