# FUI

Compile-time MVVM and typed navigation for Unity uGUI.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-black.svg)](https://unity.com/)
[![UPM package](https://img.shields.io/badge/UPM-com.fujisheng.fui-blue.svg)](https://openupm.com/packages/com.fujisheng.fui/)

[中文](README.zh-CN.md)

FUI is a Unity package ecosystem built around source-generated binding, typed routes, deterministic navigation, and explicit resource ownership. The repository contains the stable Core package and may host tightly coupled first-party extensions in the future.

## Packages

| Package | Status | Purpose |
|---|---|---|
| [`com.fujisheng.fui`](Packages/com.fujisheng.fui) | Stable | Core, navigation, source generation, and uGUI rendering |
| [`com.fujisheng.unitycli`](https://github.com/fujisheng/unitycli) | External | Unity automation bridge |
| [`com.fujisheng.fui.cli`](https://github.com/fujisheng/fui-cli) | External | AI-oriented FUI automation |

External tools stay in their own repositories and are optional. FUI Core does not depend on them.

## Install

Choose one installation method. Do not install the same package from both OpenUPM and Git in one project.

### OpenUPM (recommended)

In **Edit → Project Settings → Package Manager**, add this scoped registry:

| Field | Value |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

Then open **Window → Package Manager**, choose **+ → Add package by name**, and install `com.fujisheng.fui` at the latest published version (currently `1.0.3`). The scope also covers official FUI extensions and installation prerequisites. See the [OpenUPM package page](https://openupm.com/packages/com.fujisheng.fui/) or the [full installation guide](Packages/com.fujisheng.fui/Documentation~/installation.md).

### Git URL

In **Window → Package Manager**, choose **+ → Add package from git URL** and enter:

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.3
```

The package-prefixed tag pins an immutable release. Replace the tag only with another published FUI release tag.

For local development, use the Gitea source repository and reference `Packages/com.fujisheng.fui` as an embedded or local package.

## Start here

- [Core package guide](Packages/com.fujisheng.fui/README.md)
- [Five-minute quick start](Packages/com.fujisheng.fui/Documentation~/quick-start.md)
- [Ecosystem boundaries](docs/ecosystem.md)
- [Package authoring rules](docs/package-authoring.md)
- [Release process](docs/release-process.md)
- [Contributing](CONTRIBUTING.md)

## Author

Designed and maintained by [Fujisheng](https://github.com/fujisheng).

If FUI solves a problem for your project, star the repository to follow releases.
