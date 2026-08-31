# Navigator lifecycle

Navigator owns page state, ordering, focus, dependencies, caching, and rollback. Initialize it on the Unity main thread before opening a route.

Typical lifecycle:

```text
Create lease → Initialize View → Create binding and Presenter
→ OnCreate → OnOpen → enter transition → Focus
→ exit transition → OnClose → release or cache
```

Asynchronous operations carry an operation version. A late completion cannot overwrite a newer open or close. Cancellation and failures release temporary leases, input gates, bindings, and dependency ownership.

Use `CoverageMode`, `HistoryMode`, and `CacheMode` through `[RoutePolicy]`. Do not reproduce Navigator state in application-level managers.
