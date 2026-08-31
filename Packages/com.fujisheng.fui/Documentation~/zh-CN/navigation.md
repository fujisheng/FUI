# Typed Route 与 ViewHandle

Generator 为每个 View Contract 生成 `Route<TViewModel>`。Route 固化 Asset Key、ViewModel、Presenter、Binding Factory 与页面 Policy。

```csharp
Routes.Initialize();
var handle = navigator.Open(Routes.SettingsView, parameter);
```

Presenter 覆盖异步打开生命周期时使用 `OpenAsync`；投影已有 ViewModel 时使用 `OpenWithViewModel`。

每次成功打开都会得到新的 `ViewHandle`。页面关闭或缓存复用后，旧 Handle 永久无效。调用方必须保存并关闭自己拥有的准确 Handle，不能使用字符串查找页面。

`Back()` 只遍历显式导航历史，依赖页面不会成为 Back 目标。
