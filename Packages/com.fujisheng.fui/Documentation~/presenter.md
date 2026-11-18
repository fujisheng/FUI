# Presenter integration

`Presenter<TViewModel>` connects application services to a generated ViewModel without exposing View or BindingContext infrastructure.

Use lifecycle methods deliberately:

- `OnCreate`: allocate Presenter-owned state once.
- `OnOpen`: apply parameters and subscribe to application events.
- `OnFocus` and `OnUnfocus`: react to navigation focus.
- `OnCovered` and `OnRevealed`: react to visual coverage without reopening.
- `OnClose` and `OnCloseAsync`: unsubscribe and persist transient state.
- `OnDestroy`: release Presenter-owned resources.

Business requests and delayed effects belong to the Presenter or application service. Start them from `OnOpen` without making navigation await them; own their cancellation per open session, cancel on close/destroy/reopen, and observe failures. If data is required before a page may appear, fetch it in the application before opening the route. There is no framework `OnOpenAsync` hook or inferred synchronous capability.

Keep navigation ownership outside the View. Application entry points may wrap a Navigator, but they should continue to pass typed Routes and exact ViewHandles.
