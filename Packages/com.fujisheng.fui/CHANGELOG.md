# Changelog

All notable public changes to `com.fujisheng.fui` are documented here. Versions follow Semantic Versioning.

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
