# 公共 API 速查

## Binding

- `ObservableObject`、`BindableProperty<T>`、`ObservableList<T>`
- `Command`、`Command<T>`
- `ViewModuleAttribute`、`ViewContractAttribute`、`ObservablePropertyAttribute`
- `BindAttribute`、`CommandAttribute`、`BindingMode`
- `IValueConverter<TSource, TTarget>`

## Presentation 与 Rendering

- `ViewModel`、`Presenter<TViewModel>`
- `IView`、`IElement`、`IListView`
- `View`、`Element`、`ComponentElement<TComponent>`
- `FUI.Rendering.UGUI.Elements` 中的内置 Element

## Navigation

- `Navigator`、`Route<TViewModel>`、`ViewHandle`、`OpenOptions`
- `RoutePolicy`、`RoutePolicyAttribute`、`Layer`
- `HistoryMode`、`CoverageMode`、`CacheMode`、`ViewState`
- `IMainThreadDispatcher`

## Ownership 与 Transition

- `IViewProvider`、`IViewLease`、`IAssetScope`、`IAssetLease<T>`、`IPreloadLease`
- `IInputGate`、`OperationPhase`
- `ITransitionProvider`、`ITransition`
- `ILogSink`

生成 BindingContext 和 Infrastructure 类型除非出现在本列表，否则视为内部实现。
