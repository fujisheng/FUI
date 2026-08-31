# FUI 3.0

FUI 是独立的 Unity MVVM、导航与 UGUI 渲染包。运行时不依赖第三方库；资源系统、动画和输入控制均通过接口接入。

## 程序集

| 程序集 | 命名空间 | 职责 |
|---|---|---|
| `FUI.Core` | `FUI.Binding`、`FUI.Presentation`、`FUI.Rendering` | 纯 C# 绑定、ViewModel、Presenter 与渲染抽象 |
| `FUI.Navigation` | `FUI.Navigation`、`FUI.Resources`、`FUI.Transitions` | Route、Navigator、资源所有权与动画协议 |
| `FUI.Rendering.UGUI` | `FUI.Rendering.UGUI`、`.Elements` | UGUI View 和 Element 实现 |
| `FUI.Editor` | `FUI.Editor.Validation`、`.Inspection`、`.Installation` | 校验、绑定检查与安装工具 |
| `FUI.Editor.ILPostProcessing` | `FUI.Editor.ILPostProcessing` | Mixed 模式的可选自动属性注入 |
| `FUI.Generators` | `FUI.Generators` | Source Generator 与 Analyzer |

## 应用入口

为包含 ViewModel 的程序集指定生成命名空间：

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
[RoutePolicy(Layer.Popup, CoverageMode = CoverageMode.KeepVisible)]
public partial class SettingsViewModel : ViewModel
{
    [ObservableProperty]
    [Bind("Title", nameof(TextElement.Content))]
    string title;
}

public sealed class SettingsPresenter : Presenter<SettingsViewModel>
{
}
```

生成器输出 `${ModuleNamespace}.Routes` 和同命名空间的 internal `BindingRegistry`：

```csharp
Sample.UI.Routes.Initialize();

var navigator = new Navigator(viewProvider);
navigator.Initialize();
ViewHandle handle = navigator.Open(Sample.UI.Routes.SettingsView);
navigator.Close(handle);
```

业务层只需要操作 `ViewModel`、`Presenter<T>`、`Navigator`、`Route<T>` 和 `ViewHandle`。`View`、`Element`、`ComponentElement<T>` 属于渲染扩展 API；`ViewInstance` 和 `BindingContext<T>` 属于隐藏基础设施。

## 属性模式

Pure 模式不需要 Cecil，只支持字段生成属性：

```csharp
[ObservableProperty]
[Bind("Title", nameof(TextElement.Content))]
string title;
```

Mixed 模式同时支持自动属性：

```csharp
[Bind("Title", nameof(TextElement.Content))]
public string Title { get; set; }
```

Mixed 模式通过 `Tools/FUI/Setup` 安装 Unity 官方 Cecil 包并启用可选 `FUI.Editor.ILPostProcessing`。主包不直接依赖 Cecil。

## Editor 工具

- `Tools/FUI/Setup`：安装 Analyzer，并按需安装 Mixed 模式使用的 Cecil；属性模式只由 `ViewModule.PropertyMode` 决定。
- `Tools/FUI/Page Wizard`：创建 View Prefab，并提供 FullScreen、Popup、Tips Policy 预设。
- `Tools/FUI/Validate Views`：检查 Element 缺失、类型错误、重名、嵌套边界和页面结构。
- `BindingInspector`：在 Edit Mode 查看绑定关系、定位对象并跳转源码。
- Prefab 保存后自动增量验证；构建前通过 `IPreprocessBuildWithReport` 执行同一套门禁。

## 扩展点

- `IViewProvider`：连接资源后端并返回 `IViewLease`。
- `IAssetScope`：管理页面级资源所有权。
- `ITransitionProvider`：为 View 提供进入和退出动画。
- `IInputGate`：在异步打开、关闭或预热期间持有输入阻塞 Lease。
- `Element`、`ComponentElement<T>`：实现自定义 UGUI Element。

Element 扩展规则见 [ELEMENTS.md](Documentation~/ELEMENTS.md)，Provider 约定见 [PROVIDER.md](Documentation~/PROVIDER.md)，升级说明见 [MIGRATION_3.0.md](Documentation~/MIGRATION_3.0.md)。

## 验证 Generator

```bash
cd Packages/com.fujisheng.fui/FUI.Generators~
./verify.sh
```

脚本以锁定依赖构建 `FUI.Generators.dll`、运行测试，并校验包内预编译 DLL 与构建产物一致。
