# Resource leases and providers

FUI does not select a resource backend. Applications or first-party extension packages implement `IViewProvider` and return ownership objects.

- `IViewLease` owns an `IView` and its page `IAssetScope`.
- `IAssetScope` owns child asset leases.
- `IPreloadLease` owns preloaded resources but no instantiated View.
- `IInputGate` returns a disposable input-blocking lease.

Providers must release temporary resources on failure, transfer ownership only after success, and make `Dispose` idempotent. A cached View keeps its `IViewLease` alive until eviction.

`RouteDescriptor.AssetKey` is the only backend key passed to a provider. Providers must not depend on a generated `Routes` type or call Presenter and binding lifecycle methods themselves.
