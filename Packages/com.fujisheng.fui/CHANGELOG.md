# Changelog

All notable public changes to `com.fujisheng.fui` are documented here. Versions follow Semantic Versioning.

## [Unreleased]

### Added

- `FUI0301` checks ViewModel and Presenter source for renderer-owned objects. Generated binding contexts have assembly and derived-type visibility.
- `IScrollableListView.ScrollRequest` exposes item positioning as a ViewModel binding.
- Renderer-neutral `ITextElement` and `IClickElement` contracts for portable generated binding. Existing uGUI Elements and Mixed-mode pages retain their current behavior.
- Source-linked .NET Core/Navigation projects and portable contract tests under `tools/dotnet`, including execution of Pure generated bindings against the actual runtime.
- Float/bool semantic value contracts and a renderer-neutral Settings Sample ViewModel; uGUI contracts forward the existing properties and commands. Isolated Unity validation passes; Client/game and target-platform regression validation remain pending.

### Changed

- List bindings use `IListView.ItemsSource`. Update property bindings and package consumers as a coordinated change.

### Fixed

- Dispatch an awaited main-thread switch continuation directly to the captured synchronization context; completing a Task on that context could otherwise resume the caller on a worker thread. Safe await callbacks preserve the target context while flowing AsyncLocal, including on Mono. Portable tests, standalone Mono and isolated Unity cancellation/late-resource integration pass; standalone Player and IL2CPP validation remain pending.
- Preserve a valid source span at the rewritten Mixed setter entry instead of generating zero-span visible sequence points and repeated IL offsets. The old mapping could produce malformed Portable PDBs and a native Mono debugger assertion. Existing instrumented target assemblies must be recompiled to regenerate their DLL/PDB; upgrading the processor alone does not repair them. Setter debugging retains its entry location, with fewer internal stepping locations.

The portable contracts and Settings uGUI adapter pass the isolated Unity 6000.3.22f1 framework gates. A Runtime Mixed setter probe also passes after the PDB fix. Client/game flows, target-platform input, standalone Player and IL2CPP remain unverified.

These additions do not claim a complete headless rendering backend, Webity integration, or full game/target-platform regression validation.

## [2.0.0] - 2026-11-03

### Breaking Changes

- 抽离 uGUI 渲染层到独立的 `com.fujisheng.fui.ugui` 包。`com.fujisheng.fui` 不再声明 `com.unity.ugui` 依赖，也不再内置 `FUI.Rendering.UGUI` 程序集、uGUI Settings Sample 与 uGUI 专用 Editor 工具（Page Wizard、Binding Inspector、View Validator）。
- 命名空间和类型名保持不变（`FUI.Rendering.UGUI`、`FUI.Rendering.UGUI.Elements`、`FUI.Editor.*`）；uGUI 项目升级到 2.0.0 时需同时安装 `com.fujisheng.fui.ugui` 1.0.0。

## [1.0.3] - 2026-09-02

### Fixed

- Rebuilt and verified the packaged Source Generator so `Roslyn~/FUI.Generators.dll` is byte-identical to the checked-in generator source.

## [1.0.2] - 2026-08-31

### Changed

- Moved all FUI editor tools from `Tools/FUI/...` to the top-level `FUI/...` menu.

## [1.0.1] - 2026-08-31

### Fixed

- Rebuilt the packaged Generator after repository normalization so source, Package DLL, and Settings Sample DLL pass the strict identity gate.
- Made repository verification portable when `rg` is unavailable and added an idempotent GitHub Release recovery path.

## [1.0.0] - 2026-08-31

First stable public release.

- Source-generated MVVM binding and diagnostics.
- Typed routes and `ViewHandle` navigation.
- Synchronous and asynchronous navigation with failure rollback.
- Explicit view, asset, preload, and input-gate leases.
- uGUI Elements, list bindings, nested views, and custom Element extension points.
- Page Wizard, Binding Inspector, prefab validation, and build validation.
- Pure and Mixed observable-property modes.

The repository contains earlier internal development history. Version `1.0.0` is the first supported public API baseline.
