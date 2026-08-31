# Presenter integration

`Presenter<TViewModel>` connects application services to a generated ViewModel without exposing View or BindingContext infrastructure.

Use lifecycle methods deliberately:

- `OnCreate`: allocate Presenter-owned state once.
- `OnOpen`: apply parameters and subscribe to application events.
- `OnOpenAsync`: await asynchronous preparation; the route then requires `OpenAsync`.
- `OnFocus` and `OnUnfocus`: react to navigation focus.
- `OnCovered` and `OnRevealed`: react to visual coverage without reopening.
- `OnClose` and `OnCloseAsync`: unsubscribe and persist transient state.
- `OnDestroy`: release Presenter-owned resources.

Keep navigation ownership outside the View. Application entry points may wrap a Navigator, but they should continue to pass typed Routes and exact ViewHandles.
