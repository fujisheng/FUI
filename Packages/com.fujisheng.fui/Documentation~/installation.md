# Installation

## Requirements

- Unity 6000.0 or newer.
- A project assembly definition for ViewModels that use generated binding.
- For uGUI rendering: `com.fujisheng.fui.ugui` 1.0.0, installed alongside Core.

## OpenUPM

Use OpenUPM for published releases. In **Edit → Project Settings → Package Manager**, add a scoped registry with these values:

| Field | Value |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

FUI/Setup never changes scoped registries. Configure this once yourself before installing optional FUI tools or extensions.

Open **Window → Package Manager**, select **+ → Add package by name**, and choose a version published in the registry. Use the Git installation method below for the Core 2.0 and uGUI 1.0 source versions. Versions are listed on the [FUI OpenUPM page](https://openupm.com/packages/com.fujisheng.fui/) and the [FUI UGUI OpenUPM page](https://openupm.com/packages/com.fujisheng.fui.ugui/).

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
    "com.fujisheng.fui": "2.0.0",
    "com.fujisheng.fui.ugui": "1.0.0"
  }
}
```

Remove `com.fujisheng.fui.ugui` from the dependencies section if the project does not render with uGUI.

## Git URL

Use **Package Manager → Add package from git URL**. Git must be installed and available to Unity. Add Core and, for uGUI projects, the backend as separate URLs:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.ugui#main
```

The `path` selects the package from this Monorepo. Pin both Git URLs to the same commit SHA for reproducible installs. Do not add the same package through both Git and OpenUPM.

## Generator setup

Open **FUI → Setup**, assign the assembly definition that contains your ViewModels, then select **Install Generator**. Pure mode needs no additional package. For Mixed mode, also select **Install Cecil For Mixed Mode**.

Do not commit generated `Library` content. Commit the installed analyzer DLL and its `.meta` file when the target assembly is shared by a team.
