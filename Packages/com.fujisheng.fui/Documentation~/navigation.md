# Typed routes and ViewHandle

The generator creates a `Route<TViewModel>` for every View contract. A route contains the asset key, ViewModel factory, Presenter factory, binding factory, and immutable policy.

```csharp
Routes.Initialize();
var handle = navigator.Open(Routes.SettingsView, parameter);
```

`Open` loads resources synchronously and presents immediately. `OpenAsync` prepares resources in parallel and presents in asynchronous call order. Both invoke the same synchronous `OnOpen` lifecycle and complete after presentation is committed and the enter transition starts, without waiting for animation or business tasks. Use `OpenWithViewModel` / `OpenAsyncWithViewModel` to project an existing ViewModel instance.

Start all requests before awaiting them to overlap resource loading:

```csharp
var first = navigator.OpenAsync(Routes.SettingsView);
var second = navigator.OpenAsync(Routes.HelpView);
await first;
await second;
```

A later asynchronous request cannot present before an earlier pending one, even when its resources finish first. Synchronous `Open` bypasses that queue. Layer priority is unchanged. A failed or cancelled request is skipped; late resources are released. Cancellation stops the pending open, not a page that has already been presented.

Dependencies prepare alongside their parent and present within the same request in `RequiredBefore → parent → AttachedAfter` order. Enter animations do not block the queue. Resource preparation does not instantiate views or invoke lifecycle methods.

A page stays non-interactable while its enter animation is pending. Covering it cannot be undone by that older animation completing; if it is revealed before the animation finishes, interaction resumes only after completion.

A newly created or cache-restored page receives a new `ViewHandle`; reopening an active single-instance route reuses its existing handle. Closing or reusing a cached page never makes an old handle valid again. Store the exact handle you own and close that handle rather than looking up a page by string.

`Back()` navigates explicit history. Required and attached dependency pages do not become Back targets.
