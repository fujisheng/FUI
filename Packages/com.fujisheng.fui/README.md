# FUI Core 2.0

Renderer-neutral compile-time MVVM, typed navigation, and explicit resource ownership for Unity 6000.0 or newer.

[中文](README.zh-CN.md) · [Documentation](Documentation~/index.md) · [Changelog](CHANGELOG.md)

## Why FUI

- Source-generated property bindings, commands, routes, and diagnostics.
- Typed `Route<TViewModel>` and generation-safe `ViewHandle` navigation.
- Synchronous loading or parallel asynchronous loading with ordered presentation and rollback.
- Explicit ownership through view, asset, preload, and input-gate leases.
- Extensible providers, transitions, Elements, converters, and logging.
- Editor setup, validation, and build gates; the uGUI Binding Inspector, Page Wizard, and View Validator ship with the UGUI backend.

FUI Core has no dependency on a UI toolkit, resource backend, animation library, CLI, or agent tool.

## Install

Choose one installation method. Do not add the same package through both OpenUPM and Git.

### OpenUPM (recommended)

Add the `package.openupm.com` scoped registry with the `com.fujisheng` scope, then add `com.fujisheng.fui` from Package Manager (currently `2.0.0`). For uGUI projects, also add the backend package `com.fujisheng.fui.ugui` (currently `1.0.0`). See [Installation](Documentation~/installation.md) for the exact registry and manifest settings.

### Git URL

Use **Package Manager → Add package from git URL**. This Monorepo URL includes both the core package path and an immutable package-prefixed tag:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
```

For uGUI rendering, add the backend package with its own URL:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.ugui#main
```

## Minimal model

Declare the generator module once in the assembly that owns the ViewModels:

```csharp
[assembly: FUI.Binding.ViewModule("Sample.UI",
    PropertyMode = FUI.Binding.PropertyGenerationMode.Pure)]
```

The page example below uses the uGUI backend package (`com.fujisheng.fui.ugui`):

Declare a page:

```csharp
using FUI.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Rendering.UGUI.Elements;

[ViewContract("SettingsView")]
[RoutePolicy(Layer.Popup)]
public partial class SettingsViewModel : ViewModel
{
    [ObservableProperty]
    [Bind("Title", nameof(TextElement.Content))]
    string title = "Settings";

    [Command("Close", nameof(ButtonElement.OnClick))]
    public void Close() { }
}

public sealed class SettingsPresenter : Presenter<SettingsViewModel> { }
```

Initialize generated routes and navigate:

```csharp
Sample.UI.Routes.Initialize();
var navigator = new Navigator(viewProvider);
navigator.Initialize();

var handle = navigator.Open(Sample.UI.Routes.SettingsView);
navigator.Close(handle);
```

Start with the [five-minute guide](Documentation~/quick-start.md) and import the Settings Sample from the UGUI package in Package Manager.

## Assemblies

| Assembly | Responsibility |
|---|---|
| `FUI.Core` | Binding primitives, ViewModels, Presenters, and rendering contracts |
| `FUI.Navigation` | Routes, Navigator, resource ownership, and transitions |
| `FUI.Editor` | Setup, validation contracts, and build gates |
| `FUI.Editor.ILPostProcessing` | Optional Mixed-mode property injection |
| `FUI.Generators` | Source Generator and analyzers |

The `FUI.Rendering.UGUI` and `FUI.Rendering.UGUI.Editor` assemblies ship in the independently versioned [`com.fujisheng.fui.ugui`](../com.fujisheng.fui.ugui/README.md) package.

## Property modes

Pure mode generates properties from fields and requires no Cecil dependency. Mixed mode also supports automatic properties and requires Unity's official Cecil package, installed from `FUI/Setup`.

## License

MIT. See [Third Party Notices](Third%20Party%20Notices.md) for development dependencies. The UGUI backend's notices, including its Unity-derived editor controls, are in the [`com.fujisheng.fui.ugui` Third Party Notices](../com.fujisheng.fui.ugui/Third%20Party%20Notices.md).
