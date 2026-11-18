# Provider 接入

FUI 不绑定具体资源系统。`IViewProvider` 只接收不可变的 `RouteDescriptor`，并通过 Lease 表达资源与实例所有权。

## 契约

```csharp
public interface IViewProvider
{
    IViewLease Create(RouteDescriptor route, IPreloadLease preload = null);

    ValueTask<IViewLease> CreateAsync(
        RouteDescriptor route,
        IPreloadLease preload,
        CancellationToken token);

    ValueTask<IPreloadLease> PreloadAsync(
        RouteDescriptor route,
        CancellationToken token);
}
```

Provider 不应依赖生成的 `Routes` 类型，也不应把具体后端 Handle 暴露给 Navigator。

## 所有权

- `IViewLease` 同时持有 `IView` 与页面级 `IAssetScope`。
- View 进入缓存时，`IViewLease` 必须继续存活。
- View 被销毁或缓存淘汰时，Navigator 释放 `IViewLease`。
- `IAssetScope.Dispose()` 必须兜底释放仍存活的全部子 `IAssetLease<T>`。
- `IPreloadLease` 只持有预热资源，不得持有已实例化 View。
- `Create` 或 `CreateAsync` 接收匹配的预热 Lease 后，应原子地接管或复制其所有权；失败路径必须保持可释放。

## 取消与失败

- 异步方法必须观察 `CancellationToken`。
- 创建失败前已经取得的临时资源由 Provider 自行释放。
- 返回成功后，资源所有权转移给返回 Lease。
- `Dispose` 必须幂等，避免取消、回滚和关闭竞争导致重复释放。
- `Navigator.PreloadAsync` 捕获调用时的 Provider；替换 Provider 或关闭 Navigator 会使未完成预热失效，迟到 Lease 只释放，不进入新 Provider 的预热池。

## View 创建

Provider 完成以下工作：

1. 依据 `RouteDescriptor.AssetKey` 定位资源。
2. 创建 UGUI 实例并取得 `FUI.Rendering.UGUI.View`（需安装 `com.fujisheng.fui.ugui`）。
3. 为页面创建 `IAssetScope`。
4. 返回同时持有 View 与 Scope 的 `IViewLease`。

绑定、Presenter、生命周期与导航状态由生成代码和 Navigator 负责，Provider 不应自行执行这些步骤。
