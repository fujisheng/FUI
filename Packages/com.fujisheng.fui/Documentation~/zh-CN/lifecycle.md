# Navigator 生命周期

Navigator 统一管理页面状态、顺序、焦点、依赖、缓存和失败回滚。必须在 Unity 主线程初始化后再打开 Route。

```text
创建 Lease → 初始化 View → 创建 Binding 与 Presenter
→ OnCreate → OnOpen → 呈现页面并提供交互 → 进入动画完成
→ 退出动画 → OnClose → 释放或缓存
```

页面绑定完成并进入导航后即可接收新的指针手势。入场期间可以执行返回，旧动画完成时由 Operation Version 校验状态。覆盖、隐藏和退出会取消已有的按下或拖拽，抬起不会交给新露出的页面。

通过 `[RoutePolicy]` 配置 `CoverageMode`、`HistoryMode`、`CacheMode` 和 `InputCoverage`。默认取得焦点的 Popup 和隐藏前页的窗口会在可见期间阻断下层指针，包括空白区域和退出动画。`InputCoverageMode.BlockBehind` 显式阻断下层。`InputCoverageMode.HitTest` 只使用当前页面控件的命中区域，适合由多个独立页面组成的 HUD。
