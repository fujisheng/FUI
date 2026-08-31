# FUI 生态边界

FUI 使用 Monorepo 管理 Core 与紧密耦合的第一方集成。

## 可以进入本仓库

- 实现 `IViewProvider` 或资源 Lease 的资源后端；
- 实现 `ITransitionProvider` 的动画集成；
- FUI 专用 Input Gate、Log Sink、Element、Converter 或 Validator；
- 主要价值由 FUI 生命周期定义的集成。

## 保持独立仓库

- `fui-cli` 与 `unitycli`；
- 通用 Agent 工具；
- 不依赖 FUI 也能服务其他 UI 框架的 Unity 工具；
- 具有独立发布受众的基础设施。

Core 永远不依赖扩展，扩展只使用 Core 公共 API。只有至少两个真实集成都需要同一协议，并且公共协议比复制实现更小，才将协议提升到 Core。

每个 Package 独立维护版本、CHANGELOG、测试、文档和 OpenUPM 条目。
