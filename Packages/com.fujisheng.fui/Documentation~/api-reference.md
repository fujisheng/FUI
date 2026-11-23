# Public API reference

## Binding

- `ObservableObject`, `BindableProperty<T>`, `ObservableList<T>`
- `Command`, `Command<T>`
- `ViewModuleAttribute`, `ViewContractAttribute`, `ObservablePropertyAttribute`
- `BindAttribute`, `CommandAttribute`, `BindingMode`
- `IValueConverter<TSource, TTarget>`

## Presentation and rendering

- `ViewModel`, `Presenter<TViewModel>`
- `IView`, `IElement`, `IListView`
- uGUI implementations in `com.fujisheng.fui.ugui`: `View`, `Element`, `ComponentElement<TComponent>`, and the built-in Element types in `FUI.Rendering.UGUI.Elements`

## Navigation

- `Navigator`, `Route<TViewModel>`, `ViewHandle`, `OpenOptions`
- `RoutePolicy`, `RoutePolicyAttribute`, `Layer`
- `HistoryMode`, `CoverageMode`, `InputCoverageMode`, `CacheMode`, `ViewState`
- `IMainThreadDispatcher`

## Ownership and transitions

- `IViewProvider`, `IViewLease`, `IAssetScope`, `IAssetLease<T>`, `IPreloadLease`
- `IInputGate`, `OperationPhase`
- `ITransitionProvider`, `ITransition`
- `ILogSink`

Generated binding contexts and infrastructure types are implementation details unless explicitly declared public in this list.
