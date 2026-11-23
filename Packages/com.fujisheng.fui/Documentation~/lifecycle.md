# Navigator lifecycle

Navigator owns page state, ordering, focus, dependencies, caching, and rollback. Initialize it on the Unity main thread before opening a route.

```text
Create lease → Initialize View → Create binding and Presenter
→ OnCreate → Present the interactive page → Enter transition completes
→ Exit transition → OnClose → Release or cache
```

The page can receive new gestures as soon as its bindings are ready and it enters navigation. Back can interrupt the enter transition. Operation versions keep a late animation result from overwriting a newer navigation action. Covering, hiding, or closing a page cancels its active gestures.

Set `CoverageMode`, `HistoryMode`, `CacheMode`, and `InputCoverage` through `[RoutePolicy]`. A focused Popup or a window that hides the previous page blocks lower pages while it is visible, including blank areas and during exit. `InputCoverageMode.BlockBehind` explicitly blocks lower pages. `InputCoverageMode.HitTest` uses the page's authored hit areas and lets composite HUDs receive input on lower companion pages.
