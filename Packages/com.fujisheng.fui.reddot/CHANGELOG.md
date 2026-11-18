# Changelog

## [0.1.1] - 2026-11-03

- Updated dependencies to `com.fujisheng.fui` 2.0.0 and `com.fujisheng.fui.ugui` 1.0.0 after the uGUI rendering layer moved out of Core. Red Dot remains uGUI-based and does not migrate to URUI.
- Refreshed the sample's bundled core `FUI.Generators.dll` to match the packaged generator DLL shipped with Core 2.0.0 (generator header 1.0.3).

## [0.1.0] - 2026-09-02

- Added the batched DAG runtime, generated catalogs, FUI element, read-only debugger and verification tests.
- Hardened registration disposal, Runtime/Scope rebinding and Scope destruction against partial lifecycle failures.
- Added semantic dynamic-factory validation, paged versioned diagnostics, and full calculation/notification performance assertions.
- Replaced the placeholder badge with a visible prefab used by the Quick Start scene and Sample PlayMode tests.
