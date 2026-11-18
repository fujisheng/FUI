# Five-minute quick start

1. Install `com.fujisheng.fui` and, for uGUI rendering, `com.fujisheng.fui.ugui` from Package Manager.
2. Import **Settings Sample** from the `com.fujisheng.fui.ugui` package in Package Manager.
3. Open `SettingsSample.unity`.
4. Wait for Unity compilation to finish.
5. Enter Play Mode.

The UGUI package's sample uses `FUI.Rendering.UGUI` and contains a core-matching Generator scoped to its own assembly. For production assemblies, install the Generator from **FUI → Setup**.

The sample creates a small settings panel with bound title, volume, vibration, and close controls. Moving the slider or toggle updates the ViewModel through two-way binding. The close button calls a generated command and closes the active `ViewHandle`.

The relevant files are intentionally small:

- `SettingsViewModel.cs`: contract, properties, commands, and Presenter.
- `SettingsSampleBootstrap.cs`: route initialization and Navigator ownership.
- `SettingsSampleViewProvider.cs`: code-only sample view and lease implementation.

Continue with [generated binding](binding.md) and [typed navigation](navigation.md).
