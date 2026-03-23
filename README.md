# FUI

Unity UGUI 场景下的 MVVM UI 框架，包含：

> An MVVM UI framework for Unity UGUI.

- 运行时 UI 管理（打开/关闭/返回、层级、全屏、依赖、过渡）
- 声明式数据绑定（属性绑定、命令绑定、值转换）
- 编译期代码生成（Roslyn Source Generator）
- Unity 编辑器工具链（Installer、IL 后处理、Inspector）

UPM 包名：`com.fujisheng.fui`

---

## 特性

- **UIManager 统一调度**：支持同步/异步打开、栈管理、层级管理与返回逻辑
- **MVVM + Presenter 分层**：`ViewModel` 负责状态，`Presenter` 负责交互逻辑
- **绑定代码自动生成**：减少手写胶水代码，降低运行时反射依赖
- **UGUI 控件封装完善**：`ButtonElement`、`InputFieldElement`、`ListViewElement` 等
- **编辑器可视化辅助**：可在 Inspector 查看绑定信息并快速定位

---

## 相较其他常见方案的优点

| 对比场景 | 常见做法 | FUI 的优势 |
| --- | --- | --- |
| 手写 UI 事件/赋值胶水代码 | 每个页面重复写监听、同步与解绑逻辑 | 通过 `Binding` / `Command` + Source Generator 自动生成绑定上下文，减少重复样板代码 |
| 运行时反射驱动绑定 | 运行时开销更高，出错点偏后置 | 以编译期生成 + 明确类型约束为主，绑定问题更早暴露、更易定位 |
| 只提供基础页面管理 | 打开/关闭/返回、层级、全屏和依赖逻辑需要业务层自行拼装 | `UIManager` + `ViewTaskQueue` + `UISettings` 提供完整生命周期与栈管理能力 |
| 只有运行时框架，缺少编辑器协同 | 配置与排障效率低 | 提供 `FUI/Installer`、IL 后处理、`ViewInspector`，从接入到排查链路更完整 |

---

## 快速开始

### 1) 安装（UPM）

在 `manifest.json` 中添加依赖：

```json
{
  "dependencies": {
    "com.fujisheng.fui": "https://<your-repo-url>.git"
  }
}
```

> 若 `package.json` 不在仓库根目录，请使用 `?path=/子目录名`（例如 `?path=/Packages/com.fujisheng.fui`）。

### 2) 初始化 UIManager

```csharp
using FUI.Manager;
using FUI.UGUI;

IAssetLoaderFactory assetLoaderFactory = /* your implementation */;
var viewFactory = new ViewFactory(assetLoaderFactory);
var uiManager = new UIManager(viewFactory);
uiManager.Initialize();

uiManager.Open("MainView");
```

### 3) 编写 ViewModel 绑定

```csharp
using FUI;
using FUI.Manager;
using FUI.UGUI.Control;

[ViewBinding("MainView")]
[Settings(Layer.Common)]
public partial class MainViewModel : ViewModel
{
    [Binding("StartButton", nameof(ButtonElement.TextValue))]
    public string StartText { get; set; } = "Start";

    [Command("StartButton", nameof(ButtonElement.OnClick))]
    public void OnStartClicked()
    {
        // business logic
    }
}
```

---

## 典型工作流

1. 搭建 View（Prefab）并确定 Element 命名
2. 编写 `ViewModel`（`[Binding]` / `[Command]`）
3. 在 Unity 菜单执行 `FUI/Installer` 安装 Source Generator
4. 编译后确认生成代码与绑定关系
5. 通过 `UIManager` 驱动页面生命周期
6. 用 `ViewInspector` 排查绑定问题

---

## 核心概念

- **UIManager**（`Runtime/Manager/UIManager.cs`）
  - 入口 API：`Open` / `OpenAsync` / `Close` / `Back` / `CloseAll`
- **UIEntity**（`Runtime/Core/UIEntity/`）
  - 聚合 `IView + ViewModel + Presenter`，负责生命周期
- **BindingContext**（`Runtime/Core/BindingContext/`）
  - 管理 View 与 ViewModel 的绑定/解绑
- **BindingContextTypeResolver**（`Runtime/Core/BindingContext/BindingContextTypeResolver.cs`）
  - 解析 View、ViewModel、Context、Presenter 映射
- **UGUI Elements**（`Runtime/UGUI/Contol/`）
  - FUI 对 UGUI 组件的可绑定封装层

---

## 编辑器菜单

- `FUI/Installer`：构建并安装 `FUI.SourceGenerator.dll`
- `FUI/ILPostProcessos`：手动执行 IL 注入
- `GameObject/FUI/*`：创建 FUI 控件模板

相关文件：

- `Editor/Installer/InstallerWindow.cs`
- `Editor/ILPostProcesses/ILPostProcessesEditor.cs`
- `Editor/Menu/MenuOptions.cs`

---

## 项目结构

```text
FUI/
├─ package.json
├─ Runtime/
│  ├─ Core/
│  ├─ Manager/
│  ├─ UGUI/
│  └─ Feature/
├─ Editor/
└─ FUI.SourceGenerator~/
```

主要 asmdef：

- `FUI.Core`
- `FUI.Manager`
- `FUI.UGUI`
- `FUI.Editor`

---

## Source Generator 与 IL 后处理

- Source Generator 工程：`FUI.SourceGenerator~/`
  - 目标框架：`netstandard2.0`
  - 依赖：`Microsoft.CodeAnalysis.CSharp 4.0.1`
- Installer 会：
  1. `dotnet build FUI.SourceGenerator.sln -c Release`
  2. 复制 `FUI.SourceGenerator.dll` 到目标 asmdef 目录
  3. 写入 `.meta`（`RoslynAnalyzer` + `sourceGenerator: 1`）

---

## 兼容性

- `package.json` 声明 Unity：`2019.4`
- Source Generator 工程：`netstandard2.0`

Unity 官方参考：

- Roslyn analyzers / source generators
  - https://docs.unity3d.com/2022.3/Documentation/Manual/roslyn-analyzers.html
- UPM Git URL 安装
  - https://docs.unity3d.com/Manual/upm-ui-giturl.html

---

## 许可

本仓库采用 **MIT License**，详见根目录 [LICENSE](./LICENSE) 文件。

> `Editor/Menu/LICENSE.txt` 为菜单代码来源的第三方许可，与本项目主许可相互独立。
