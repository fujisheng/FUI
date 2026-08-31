# Installation

## Requirements

- Unity 6000.0 or newer.
- uGUI 2.0.0 or newer.
- A project assembly definition for ViewModels that use generated binding.

## OpenUPM

Use OpenUPM for published releases. In **Edit → Project Settings → Package Manager**, add a scoped registry with these values:

| Field | Value |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

FUI/Setup never changes scoped registries. Configure this once yourself before installing optional FUI tools or extensions.

Open **Window → Package Manager**, select **+ → Add package by name**, and install `com.fujisheng.fui`. Select the latest published version (currently `1.0.2`). The available versions are listed on the [FUI OpenUPM page](https://openupm.com/packages/com.fujisheng.fui/).

If your project manages `Packages/manifest.json` directly, merge these entries into the existing file; do not replace other registries or dependencies:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.fujisheng"
      ]
    }
  ],
  "dependencies": {
    "com.fujisheng.fui": "1.0.2"
  }
}
```

## Git URL

Use **Package Manager → Add package from git URL**. Git must be installed and available to Unity:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.2
```

The `path` selects the package from this Monorepo. The tag pins the install to a published, immutable release. To install a later release, replace only the tag with its matching package-prefixed tag. Do not add the same package through both Git and OpenUPM.

## Generator setup

Open **FUI → Setup**, assign the assembly definition that contains your ViewModels, then select **Install Generator**. Pure mode needs no additional package. For Mixed mode, also select **Install Cecil For Mixed Mode**.

Do not commit generated `Library` content. Commit the installed analyzer DLL and its `.meta` file when the target assembly is shared by a team.
