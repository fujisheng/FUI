# FUI Core 1.0

面向 Unity 6000.0 及以上版本的编译期 MVVM、类型安全导航和资源安全 uGUI 渲染框架。

[English](README.md) · [中文文档](Documentation~/zh-CN/index.md) · [更新记录](CHANGELOG.md)

## 核心能力

- Source Generator 生成属性绑定、Command、Route 与诊断。
- 使用 `Route<TViewModel>` 和具有代际保护的 `ViewHandle` 导航。
- 同步、异步生命周期以及失败回滚。
- 通过 View、Asset、Preload、Input Gate Lease 显式管理所有权。
- 可扩展 Provider、Transition、Element、Converter 与日志。
- 提供 Page Wizard、Binding Inspector、Prefab 与构建验证。

FUI Core 不依赖具体资源系统、动画库、CLI 或 AI Agent 工具。

## 安装

正式版本推荐使用 OpenUPM。Monorepo 的 Git URL 安装地址为：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.0
```

## 最小示例

在包含 ViewModel 的程序集声明生成模块：

```csharp
[assembly: FUI.Binding.ViewModule("Sample.UI",
    PropertyMode = FUI.Binding.PropertyGenerationMode.Pure)]
```

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

从[五分钟快速开始](Documentation~/zh-CN/quick-start.md)继续，并通过 Package Manager 导入 Settings Sample。

## 程序集

| 程序集 | 职责 |
|---|---|
| `FUI.Core` | 绑定基础、ViewModel、Presenter 与渲染协议 |
| `FUI.Navigation` | Route、Navigator、资源所有权与 Transition |
| `FUI.Rendering.UGUI` | uGUI View 与 Element |
| `FUI.Editor` | Setup、Page Wizard、检查与验证 |
| `FUI.Editor.ILPostProcessing` | 可选 Mixed 模式属性注入 |
| `FUI.Generators` | Source Generator 与 Analyzer |

## 属性模式

Pure 模式从字段生成属性，不需要 Cecil。Mixed 模式支持自动属性，需要从 `Tools/FUI/Setup` 安装 Unity 官方 Cecil Package。

## 授权

MIT。开发依赖和 Editor 侧第三方代码见 [Third Party Notices](Third%20Party%20Notices.md)。
