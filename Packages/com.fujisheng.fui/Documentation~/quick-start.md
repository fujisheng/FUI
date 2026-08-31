# Five-minute quick start

1. Install FUI and import **Settings Sample** from Package Manager.
2. Open `SettingsSample.unity`.
3. Wait for Unity compilation to finish.
4. Enter Play Mode.

The sample contains a package-matching Generator scoped to its own assembly. For production assemblies, install the Generator from **FUI → Setup**.

The sample creates a small settings panel with bound title, volume, vibration, and close controls. Moving the slider or toggle updates the ViewModel through two-way binding. The close button calls a generated command and closes the active `ViewHandle`.

The relevant files are intentionally small:

- `SettingsViewModel.cs`: contract, properties, commands, and Presenter.
- `SettingsSampleBootstrap.cs`: route initialization and Navigator ownership.
- `SettingsSampleViewProvider.cs`: code-only sample view and lease implementation.

Continue with [generated binding](binding.md) and [typed navigation](navigation.md).
