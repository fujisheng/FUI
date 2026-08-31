# Installation

## Requirements

- Unity 6000.0 or newer.
- uGUI 2.0.0 or newer.
- A project assembly definition for ViewModels that use generated binding.

## OpenUPM

Add the OpenUPM scoped registry for `com.fujisheng`, then install `com.fujisheng.fui@1.0.0` from Package Manager.

## Git URL

Use **Package Manager → Add package from git URL**:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.0
```

## Generator setup

Open **Tools → FUI → Setup**, assign the assembly definition that contains your ViewModels, then select **Install Generator**. Pure mode needs no additional package. For Mixed mode, also select **Install Cecil For Mixed Mode**.

Do not commit generated `Library` content. Commit the installed analyzer DLL and its `.meta` file when the target assembly is shared by a team.
