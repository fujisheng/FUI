# FUI

Compile-time MVVM and typed navigation for Unity.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-black.svg)](https://unity.com/)
[![UPM package](https://img.shields.io/badge/UPM-com.fujisheng.fui-blue.svg)](https://openupm.com/packages/com.fujisheng.fui/)

[中文](README.zh-CN.md)

FUI is a Unity package ecosystem built around source-generated binding, typed routes, deterministic navigation, and explicit resource ownership. The repository contains the renderer-neutral Core package and independently versioned first-party extensions, including rendering backends.

## Packages

| Package | Status | Purpose |
|---|---|---|
| [`com.fujisheng.fui`](Packages/com.fujisheng.fui) | Stable | Renderer-neutral Core, navigation, and source generation |
| [`com.fujisheng.fui.ugui`](Packages/com.fujisheng.fui.ugui) | Stable | uGUI rendering backend: Views, Elements, and uGUI editor tools |
| [`com.fujisheng.fui.urui`](Packages/com.fujisheng.fui.urui) | Preview | HTML/CSS backend for Unity 6000.3 and URUI 0.8 |
| [`com.fujisheng.fui.reddot`](Packages/com.fujisheng.fui.reddot) | Experimental | Generated, batched red dot DAG and FUI Element |
| [`com.fujisheng.unitycli`](https://github.com/fujisheng/unitycli) | External | Unity automation bridge |
| [`com.fujisheng.fui.cli`](https://github.com/fujisheng/fui-cli) | External | AI-oriented FUI automation |

External tools stay in their own repositories and are optional. FUI Core does not depend on them.

## Install

Choose one installation method. Do not install the same package from both OpenUPM and Git in one project.

### OpenUPM releases

In **Edit → Project Settings → Package Manager**, add this scoped registry:

| Field | Value |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

Then open **Window → Package Manager**, choose **+ → Add package by name**, and install the renderer-neutral core plus one rendering backend. Use the Git URLs below for the Core 2.0 and split backend source tree. Choose published versions from the registry when using OpenUPM. The scope also covers official FUI extensions and installation prerequisites. See the [OpenUPM package page](https://openupm.com/packages/com.fujisheng.fui/) or the [full installation guide](Packages/com.fujisheng.fui/Documentation~/installation.md).

### Git URL

In **Window → Package Manager**, choose **+ → Add package from git URL** and add the core package and, for uGUI projects, the backend package as separate URLs:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.ugui#main
```

Use `#main` for the source version, or replace it with a commit SHA to pin all FUI packages to the same revision. OpenUPM carries separately tagged releases.

For local development, clone this GitHub repository and reference `Packages/com.fujisheng.fui` (plus `Packages/com.fujisheng.fui.ugui` for uGUI projects) as embedded or local packages.

## Start here

- [Core package guide](Packages/com.fujisheng.fui/README.md)
- [UGUI backend guide](Packages/com.fujisheng.fui.ugui/README.md)
- [Five-minute quick start](Packages/com.fujisheng.fui/Documentation~/quick-start.md)
- [Ecosystem boundaries](docs/ecosystem.md)
- [Package authoring rules](docs/package-authoring.md)
- [Release process](docs/release-process.md)
- [Contributing](CONTRIBUTING.md)

## Author

Designed and maintained by [Fujisheng](https://github.com/fujisheng).

If FUI solves a problem for your project, star the repository to follow releases.
