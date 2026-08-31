# Navigator 生命周期

Navigator 统一管理页面状态、顺序、焦点、依赖、缓存和失败回滚。必须在 Unity 主线程初始化后再打开 Route。

```text
创建 Lease → 初始化 View → 创建 Binding 与 Presenter
→ OnCreate → OnOpen → 进入动画 → Focus
→ 退出动画 → OnClose → 释放或缓存
```

异步操作携带 Operation Version，较早完成的异步结果不能覆盖后续打开或关闭。取消和失败路径会释放临时 Lease、Input Gate、Binding 和依赖所有权。

通过 `[RoutePolicy]` 配置 `CoverageMode`、`HistoryMode` 和 `CacheMode`，业务层不要重复维护 Navigator 状态。
