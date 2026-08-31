# Contributing

Repository-wide contribution rules live in [`CONTRIBUTING.md`](../../../CONTRIBUTING.md).

For Core changes:

1. Preserve AOT and IL2CPP compatibility.
2. Keep Core independent from optional packages.
3. Add EditMode or PlayMode tests for behavior changes.
4. Add generator tests for generated API or diagnostics changes.
5. Update this documentation and `CHANGELOG.md`.
6. Run `tools/verify-package.sh com.fujisheng.fui` and `tools/verify-repository.sh`.

Public API changes require a SemVer decision and an entry describing migration impact.
