# FUI UGUI

`FUI UGUI` is the uGUI rendering backend for FUI. It provides the `FUI.Rendering.UGUI` runtime assembly: the `View` base class, the built-in `Element` types, asset loader contracts, and uGUI-specific utilities.

## Requirements

- Unity 6000.0 or newer.
- `com.fujisheng.fui` 2.0.0.
- `com.unity.ugui` 2.0.0 (resolved automatically as a package dependency).

## Installation

Install `com.fujisheng.fui.ugui` through the Unity Package Manager. The package depends on `com.fujisheng.fui` and `com.unity.ugui`, so Unity resolves both dependencies when the package is added. Do not add the same package twice through different sources.

## Namespace

Runtime types live in the `FUI.Rendering.UGUI` namespace; built-in Elements live in `FUI.Rendering.UGUI.Elements`.

## Relationship to FUI Core

`com.fujisheng.fui` contains the renderer-neutral core: MVVM primitives, source-generated bindings, typed navigation, and resource leases. This package adds the uGUI implementation of the rendering layer. The core package does not reference uGUI; install this package only when the project renders with uGUI.

## Documentation

See the `Documentation~` folder shipped with this package for installation, minimal usage, backend selection, and limitations.

## License

MIT. See [Third Party Notices](Third%20Party%20Notices.md) for the Unity Companion License notice and package dependencies.
