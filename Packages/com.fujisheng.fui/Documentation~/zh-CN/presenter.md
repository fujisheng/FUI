# Presenter 接入

`Presenter<TViewModel>` 负责连接应用服务和生成 ViewModel，不向业务暴露 View 或 BindingContext 基础设施。

- `OnCreate`：一次性创建 Presenter 状态。
- `OnOpen`：应用参数并订阅业务事件。
- `OnOpenAsync`：等待异步准备；对应 Route 必须使用 `OpenAsync`。
- `OnFocus`、`OnUnfocus`：响应导航焦点。
- `OnCovered`、`OnRevealed`：响应覆盖但不重复打开。
- `OnClose`、`OnCloseAsync`：取消订阅并保存临时状态。
- `OnDestroy`：释放 Presenter 自有资源。

导航所有权不应进入 View。业务入口可以包装 Navigator，但仍应传递 typed Route 与准确 ViewHandle。
