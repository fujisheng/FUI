# FUI Core 2.0

面向 Unity 6000.0 及以上版本、渲染无关的编译期 MVVM、类型安全导航与显式资源所有权框架。

[English](README.md) · [中文文档](Documentation~/zh-CN/index.md) · [更新记录](CHANGELOG.md)

## 核心能力

- Source Generator 生成属性绑定、Command、Route 与诊断。
- 使用 `Route<TViewModel>` 和具有代际保护的 `ViewHandle` 导航。
- 同步加载或并行异步加载，按异步调用顺序呈现，失败自动回滚。
- 通过 View、Asset、Preload、Input Gate Lease 显式管理所有权。
- 可扩展 Provider、Transition、Element、Converter 与日志。
- 提供 Setup、Prefab 与构建验证；uGUI 专用的 Page Wizard、Binding Inspector 与 View Validator 随 UGUI 后端提供。

FUI Core 不依赖具体 UI 工具包、资源系统、动画库、CLI 或 AI Agent 工具。

## 安装

二选一安装；不要同时通过 OpenUPM 和 Git 引入同一个 Package。

### OpenUPM（推荐）

添加 `package.openupm.com` Scoped Registry，并设置 Scope 为 `com.fujisheng`。随后从 Package Manager 安装 `com.fujisheng.fui`；uGUI 项目还需安装后端包 `com.fujisheng.fui.ugui`。完整 Registry 和 `manifest.json` 配置见[安装文档](Documentation~/zh-CN/installation.md)。

### Git URL

通过 **Package Manager → Add package from git URL** 安装。此 Monorepo URL 同时包含 Core 的 Package 路径和不可变的 Package 前缀标签：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#main
```

uGUI 渲染需使用独立 URL 添加后端包：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui.ugui#main
```

## 最小示例

在包含 ViewModel 的程序集声明生成模块：

```csharp
[assembly: FUI.Binding.ViewModule("Sample.UI",
    PropertyMode = FUI.Binding.PropertyGenerationMode.Pure)]
```

以下页面示例使用 uGUI 后端包（`com.fujisheng.fui.ugui`）：

声明页面：

```csharp
using FUI.Binding;
using FUI.Navigation;
using FUI.Presentation;
using FUI.Rendering.UGUI.Elements;

[ViewContract("SettingsView")]
[RoutePolicy(Layer.Popup)]
public partial class SettingsViewModel : ViewModel
{
    [ObservableProperty]
    [Bind("Title", nameof(TextElement.Content))]
    string title = "Settings";

    [Command("Close", nameof(ButtonElement.OnClick))]
    public void Close() { }
}

public sealed class SettingsPresenter : Presenter<SettingsViewModel> { }
```

初始化生成的 Route 并导航：

```csharp
Sample.UI.Routes.Initialize();
var navigator = new Navigator(viewProvider);
navigator.Initialize();

var handle = navigator.Open(Sample.UI.Routes.SettingsView);
navigator.Close(handle);
```

从[五分钟快速开始](Documentation~/zh-CN/quick-start.md)继续，并从 Package Manager 的 UGUI 包导入 Settings Sample。

## 程序集

| 程序集 | 职责 |
|---|---|
| `FUI.Core` | 绑定基础、ViewModel、Presenter 与渲染协议 |
| `FUI.Navigation` | Route、Navigator、资源所有权与 Transition |
| `FUI.Editor` | Setup、验证协议与构建门禁 |
| `FUI.Editor.ILPostProcessing` | 可选 Mixed 模式属性注入 |
| `FUI.Generators` | Source Generator 与 Analyzer |

`FUI.Rendering.UGUI` 与 `FUI.Rendering.UGUI.Editor` 程序集位于独立版本的 [`com.fujisheng.fui.ugui` (English)](../com.fujisheng.fui.ugui/README.md) 包中。

## 属性模式

Pure 模式从字段生成属性，不需要 Cecil。Mixed 模式支持自动属性，需要从 `FUI/Setup` 安装 Unity 官方 Cecil Package。

## 授权

MIT。开发依赖见 [Third Party Notices](Third%20Party%20Notices.md)；UGUI 后端的第三方声明（包括源自 Unity 的 Editor 控件）见 [`com.fujisheng.fui.ugui` Third Party Notices](../com.fujisheng.fui.ugui/Third%20Party%20Notices.md)。
