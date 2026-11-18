# Typed Route 与 ViewHandle

Generator 为每个 View Contract 生成 `Route<TViewModel>`。Route 固化 Asset Key、ViewModel、Presenter、Binding Factory 与页面 Policy。

```csharp
Routes.Initialize();
var handle = navigator.Open(Routes.SettingsView, parameter);
```

`Open` 同步加载资源并立即呈现；`OpenAsync` 并行准备资源，按照异步调用顺序呈现。两者都执行同步 `OnOpen`，提交呈现并启动进入动画后返回，不等待动画或业务任务。投影已有 ViewModel 使用 `OpenWithViewModel` / `OpenAsyncWithViewModel`。

先发起多个调用，再分别 await，资源才能并行准备：

```csharp
var first = navigator.OpenAsync(Routes.SettingsView);
var second = navigator.OpenAsync(Routes.HelpView);
await first;
await second;
```

后发异步请求即使先加载完成，也必须等待前面的请求提交。同步 `Open` 不进入此队列，可以立即呈现；Layer 优先级保持不变。失败或取消会跳过该请求，迟到资源会释放。取消信号只控制提交前的请求，不关闭已经呈现的页面。

父页面与依赖并行准备资源，在同一次请求中按 `RequiredBefore → 父页面 → AttachedAfter` 呈现。进入动画不阻塞队列。资源准备期间不创建页面、不执行生命周期。

页面进入动画未结束时保持不可交互。页面被覆盖后，旧进入动画结束不会把它错误恢复为可交互；若动画结束前页面已重新揭示，也要等动画结束后才恢复交互。

普通共享依赖在请求内去重；声明 AllowMultiple 的重复节点需要独立可转移的资源 Lease，保持多实例所有权。

新建或缓存恢复的页面会得到新的 `ViewHandle`；重复打开活动单实例页面复用原 Handle。页面关闭或缓存复用后，旧 Handle 永久无效。调用方必须保存并关闭自己拥有的准确 Handle，不能使用字符串查找页面。

`Back()` 只遍历显式导航历史，依赖页面不会成为 Back 目标。
