# Typed routes and ViewHandle

The generator creates a `Route<TViewModel>` for every View contract. A route contains the asset key, ViewModel factory, Presenter factory, binding factory, and immutable policy.

```csharp
Routes.Initialize();
var handle = navigator.Open(Routes.SettingsView, parameter);
```

Use `OpenAsync` when a Presenter overrides its asynchronous open lifecycle. Use `OpenWithViewModel` to project an existing ViewModel instance.

Every successful open returns a new `ViewHandle`. Closing or reusing a cached page never makes an old handle valid again. Store the exact handle you own and close that handle rather than looking up a page by string.

`Back()` navigates explicit history. Required and attached dependency pages do not become Back targets.
