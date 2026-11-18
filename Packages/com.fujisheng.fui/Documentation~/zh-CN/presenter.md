# Presenter 接入

`Presenter<TViewModel>` 负责连接应用服务和生成 ViewModel，不向业务暴露 View 或 BindingContext 基础设施。

- `OnCreate`：一次性创建 Presenter 状态。
- `OnOpen`：应用参数并订阅业务事件。
- `OnFocus`、`OnUnfocus`：响应导航焦点。
- `OnCovered`、`OnRevealed`：响应覆盖但不重复打开。
- `OnClose`、`OnCloseAsync`：取消订阅并保存临时状态。
- `OnDestroy`：释放 Presenter 自有资源。

业务请求和延迟表现由 Presenter 或应用服务管理。从 `OnOpen` 发起时，不纳入导航等待；每次打开持有独立取消源，关闭、销毁、重新打开时取消旧任务，处理正常取消并记录其他异常。业务必须先取得数据才能显示时，由业务先等待数据再打开页面。框架不再提供 `OnOpenAsync` 或推断同步能力。

导航所有权不应进入 View。业务入口可以包装 Navigator，但仍应传递 typed Route 与准确 ViewHandle。
