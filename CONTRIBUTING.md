# Contributing to FUI

Thank you for improving FUI. Bug reports, documentation fixes, tests, and focused pull requests are welcome.

## Before opening a change

1. Search existing issues and discussions.
2. Keep Core independent from optional extensions.
3. Add or update tests for behavior changes.
4. Update the affected package changelog and documentation.
5. Run `tools/verify-package.sh <package-id>` and `tools/verify-repository.sh`.

## Code conventions

- Use PascalCase for types and methods and camelCase for fields.
- Do not use `dynamic`.
- Keep runtime logs and exception messages in English.
- Preserve AOT and IL2CPP compatibility.
- Prefer a small public contract over exposing internal implementation.
- Do not add `InternalsVisibleTo` for extension packages.

## Package boundaries

Core cannot depend on optional packages. A first-party extension may depend on Core through its public API. Generic Unity tools and agent tooling belong in independent repositories.

See [Package authoring](docs/package-authoring.md) for naming, dependencies, tests, and release tags.

## Pull requests

Describe the problem, the chosen behavior, validation evidence, and compatibility impact. A pull request is ready when tests pass, documentation links resolve, and no generated Unity project or build output is tracked.
