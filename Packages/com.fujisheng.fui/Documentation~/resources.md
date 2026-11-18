# Resource leases and providers

FUI does not select a resource backend. Applications or first-party extension packages implement `IViewProvider` and return ownership objects.

- `IViewLease` owns an `IView` and its page `IAssetScope`.
- `IAssetScope` owns child asset leases.
- `IPreloadLease` owns preloaded resources but no instantiated View.
- `IInputGate` returns a disposable input-blocking lease.

Providers must release temporary resources on failure, transfer ownership only after success, and make `Dispose` idempotent. A cached View keeps its `IViewLease` alive until eviction.

Creating a uGUI instance and obtaining `FUI.Rendering.UGUI.View` requires the `com.fujisheng.fui.ugui` package.

`Navigator.PreloadAsync` captures the current provider. Replacing that provider or shutting down the Navigator cancels the pending operation; a late result is disposed and is never added to the new provider's preload pool.

`RouteDescriptor.AssetKey` is the only backend key passed to a provider. Providers must not depend on a generated `Routes` type or call Presenter and binding lifecycle methods themselves.
