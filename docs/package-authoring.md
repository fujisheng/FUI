# First-party package authoring

## Naming

```text
Package:   com.fujisheng.fui.addressables
Assembly:  FUI.Extensions.Addressables
Namespace: FUI.Extensions.Addressables
Tag:       com.fujisheng.fui.addressables/0.1.0
```

Use a lowercase package directory matching `package.json.name`. Do not create an empty package for a speculative integration.

## Required layout

Every package must provide `Runtime`, `Tests`, `Samples~`, `Documentation~`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, and `package.json`. Add `Editor` only for editor-only behavior.

## Dependencies

- Extensions may depend on `com.fujisheng.fui` through a released SemVer.
- Core cannot depend on an extension.
- Sibling extension dependencies must be explicit and must not form cycles.
- Never use `InternalsVisibleTo` to reach Core internals.
- Do not copy Core implementation into an extension.

## Template

Copy `tools/templates/extension-package`, replace all placeholder values, then add the new package to the root package catalog and repository verification matrix.

## Acceptance

Run `tools/verify-package.sh <package-id>` and `tools/verify-repository.sh`. A package is publishable only when tests, documentation links, changelog version, dependency direction, and package-prefixed tag validation pass.
