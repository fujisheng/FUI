# FUI

面向 Unity 的编译期 MVVM 与类型安全导航框架。

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-black.svg)](https://unity.com/)
[![UPM package](https://img.shields.io/badge/UPM-com.fujisheng.fui-blue.svg)](https://openupm.com/packages/com.fujisheng.fui/)

[English](README.md)

FUI 围绕 Source Generator 数据绑定、typed Route、确定性导航和显式资源所有权构建。本仓库包含渲染无关的 Core 与独立版本的第一方扩展，包括可选的渲染后端。

## Package 目录

| Package | 状态 | 用途 |
|---|---|---|
| [`com.fujisheng.fui`](Packages/com.fujisheng.fui) | Stable | 渲染无关的 Core、Navigation 与 Source Generator |
| [`com.fujisheng.fui.ugui`](Packages/com.fujisheng.fui.ugui) | Stable | uGUI 渲染后端：View、Element 与 uGUI 编辑器工具 |
| [`com.fujisheng.fui.urui`](Packages/com.fujisheng.fui.urui) | 预览 | HTML/CSS 渲染后端，需要 Unity 6000.3 和 URUI 0.8 |
| [`com.fujisheng.fui.reddot`](Packages/com.fujisheng.fui.reddot/README.zh-CN.md) | Experimental | 生成式、合批红点 DAG 与 FUI Element |
| [`com.fujisheng.unitycli`](https://github.com/fujisheng/unitycli) | External | Unity 自动化桥接 |
| [`com.fujisheng.fui.cli`](https://github.com/fujisheng/fui-cli) | External | 面向 AI Agent 的 FUI 自动化 |

外部工具保持独立仓库并且完全可选，FUI Core 不依赖它们。

## 安装

二选一安装；同一个项目不要同时通过 OpenUPM 和 Git 引入同一 Package。

### OpenUPM 已发布版本

在 **Edit → Project Settings → Package Manager** 中添加 Scoped Registry：

| 字段 | 值 |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

然后打开 **Window → Package Manager**，选择 **+ → Add package by name**，安装渲染无关的 Core 与一个渲染后端。Core 2.0 和独立后端可通过下列 Git URL 安装。使用 OpenUPM 时，以注册表列出的已发布版本为准。该 Scope 同时覆盖未来的官方 FUI 扩展和安装前置包。完整步骤见 [OpenUPM Package 页面](https://openupm.com/packages/com.fujisheng.fui/) 或 [安装文档](Packages/com.fujisheng.fui/Documentation~/zh-CN/installation.md)。

### Git URL

在 **Window → Package Manager** 中选择 **+ → Add package from git URL**，分别添加 Core 与（uGUI 项目的）渲染后端：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.ugui#main
```

每个 Package 前缀标签固定到不可变发布版本；仅在需要升级时替换为同一个 Package 的另一个已发布 FUI 标签。

本地开发使用 Gitea 源仓库，将 `Packages/com.fujisheng.fui`（uGUI 项目另加 `Packages/com.fujisheng.fui.ugui`）作为 Embedded 或 Local Package 引入。

## 从这里开始

- [Core Package 指南](Packages/com.fujisheng.fui/README.zh-CN.md)
- [UGUI 后端指南 (English)](Packages/com.fujisheng.fui.ugui/README.md)
- [五分钟快速开始](Packages/com.fujisheng.fui/Documentation~/zh-CN/quick-start.md)
- [生态边界](docs/zh-CN/ecosystem.md)
- [扩展包开发规范](docs/zh-CN/package-authoring.md)
- [发布流程](docs/zh-CN/release-process.md)
- [贡献指南](CONTRIBUTING.md)

## 作者

由 [Fujisheng](https://github.com/fujisheng) 设计并维护。

如果 FUI 解决了你的实际问题，可以 Star 仓库以关注后续版本。
