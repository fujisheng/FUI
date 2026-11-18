# FUI UGUI documentation

`FUI UGUI` adds the uGUI rendering backend to FUI. The runtime assembly is `FUI.Rendering.UGUI`.

## Installation

1. Requires Unity 6000.0 or newer.
2. Add `com.fujisheng.fui.ugui` through the Unity Package Manager.
3. Unity resolves the package dependencies `com.fujisheng.fui` 2.0.0 and `com.unity.ugui` 2.0.0 automatically.
4. Do not add the same package through both OpenUPM and a Git URL.

## Minimal usage

1. Add a `View` component (`FUI.Rendering.UGUI`) to the root of a prefab.
2. Add the built-in Element components (`FUI.Rendering.UGUI.Elements`) to the child GameObjects that the View should expose.
3. Create the View through an asset loader and resolve its Elements by name.

```csharp
using FUI.Rendering.UGUI;
using FUI.Rendering.UGUI.Elements;

// assetLoader is the project's IAssetLoader implementation.
View view = View.Create(assetLoader, "UI/SettingsView", "SettingsView");
view.GetElement<TextElement>("Title").Text.Value = viewModel.Title;
```

The generated binding and the ViewModel come from `com.fujisheng.fui`. Commands are exposed as `Command` properties on the Elements, for example `ButtonElement.OnClick`.

## Choosing a rendering backend

Use either `FUI UGUI` or `FUI URUI` for a view layer. Both packages implement the renderer-neutral contracts from `com.fujisheng.fui`; each provides its own runtime assembly and Element types. Do not mix Element types from different backends in one view hierarchy.

## Limitations

- This package covers uGUI only. Other UI toolkits need their own rendering backend package.
- `FUI UGUI` and `FUI URUI` are alternatives; choose one backend per view hierarchy.
- The runtime targets Unity 6000.0 or newer; earlier Unity versions are not supported.
