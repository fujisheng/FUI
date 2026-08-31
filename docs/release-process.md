# Release process

Packages use independent SemVer and package-prefixed tags.

## Prepare

1. Update the target `package.json.version`.
2. Move changelog entries from Unreleased to the same version.
3. Run `tools/verify-package.sh <package-id>`.
4. Run `tools/verify-repository.sh`.
5. Merge the release commit to Gitea `main`.
6. Confirm the GitHub mirror points to the same commit.

## Tag

Create an annotated tag with the exact package ID and version:

```bash
git tag -a com.fujisheng.fui/1.0.0 -m "FUI Core 1.0.0"
git push origin com.fujisheng.fui/1.0.0
```

Confirm the Gitea and GitHub tag object resolves to the release commit. GitHub Actions creates or updates the matching GitHub Release.

## OpenUPM

Submit each package independently and configure `gitTagPrefix` to `<package-id>/`. Wait for the tagged version to become installable, then validate it in a clean Unity project before announcing the release.

Never move or recreate a published tag. Fix a bad release with a new patch version.

Start announcements only after every gate above passes. Use the [launch kit](launch-kit.md) for the eight-week editorial cadence and channel-specific copy.
