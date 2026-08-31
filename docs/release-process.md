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

Submit each package independently for its first OpenUPM import and configure `gitTagPrefix` to `<package-id>/`. After the package is registered, the GitHub release workflow sends each pushed package-prefixed tag to OpenUPM and waits for the version to become installable.

The OpenUPM step runs only for a tag push or a workflow dispatch that explicitly names a tag. Ordinary `main` pushes create or recover GitHub Releases only; they do not trigger an OpenUPM scan. Inspect the GitHub Actions result, then validate the published version in a clean Unity project.

Never move or recreate a published tag. Fix a bad release with a new patch version.
