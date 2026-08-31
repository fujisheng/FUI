# FUI Core 1.0

Compile-time MVVM, typed navigation, and resource-safe uGUI rendering for Unity 6000.0 or newer.

[中文](README.zh-CN.md) · [Documentation](Documentation~/index.md) · [Changelog](CHANGELOG.md)

## Why FUI

- Source-generated property bindings, commands, routes, and diagnostics.
- Typed `Route<TViewModel>` and generation-safe `ViewHandle` navigation.
- Synchronous and asynchronous lifecycle operations with rollback.
- Explicit ownership through view, asset, preload, and input-gate leases.
- Extensible providers, transitions, Elements, converters, and logging.
- Editor validation, Binding Inspector, Page Wizard, and build gates.

FUI Core has no dependency on a resource backend, animation library, CLI, or agent tool.

## Install

Use OpenUPM for stable releases. Git URL installation for this Monorepo uses the package path and package-prefixed tag:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.1
```

## Minimal model

Declare the generator module once in the assembly that owns the ViewModels:

```csharp
[assembly: FUI.Binding.ViewModule("Sample.UI",
    PropertyMode = FUI.Binding.PropertyGenerationMode.Pure)]
```

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

Start with the [five-minute guide](Documentation~/quick-start.md) and import the Settings Sample from Package Manager.

## Assemblies

| Assembly | Responsibility |
|---|---|
| `FUI.Core` | Binding primitives, ViewModels, Presenters, and rendering contracts |
| `FUI.Navigation` | Routes, Navigator, resource ownership, and transitions |
| `FUI.Rendering.UGUI` | uGUI View and Element implementations |
| `FUI.Editor` | Setup, Page Wizard, inspection, and validation |
| `FUI.Editor.ILPostProcessing` | Optional Mixed-mode property injection |
| `FUI.Generators` | Source Generator and analyzers |

## Property modes

Pure mode generates properties from fields and requires no Cecil dependency. Mixed mode also supports automatic properties and requires Unity's official Cecil package, installed from `Tools/FUI/Setup`.

## License

MIT. See [Third Party Notices](Third%20Party%20Notices.md) for development and editor-side dependencies.
