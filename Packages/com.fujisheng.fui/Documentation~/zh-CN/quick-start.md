# 五分钟快速开始

1. 安装 `com.fujisheng.fui`；uGUI 渲染项目还需安装 `com.fujisheng.fui.ugui`。
2. 通过 Package Manager 从 `com.fujisheng.fui.ugui` 包导入 **Settings Sample**。
3. 打开 `SettingsSample.unity`。
4. 等待 Unity 编译完成。
5. 进入 Play Mode。

UGUI 包的 Sample 使用 `FUI.Rendering.UGUI`，并内含与 Core Generator 匹配、且仅作用于自身程序集的 Generator。业务程序集请从 **FUI → Setup** 安装 Generator。

示例会创建包含标题、音量、振动开关和关闭按钮的设置面板。Slider 和 Toggle 通过 TwoWay Binding 更新 ViewModel，关闭按钮通过生成 Command 关闭当前 `ViewHandle`。

关键文件：

- `SettingsViewModel.cs`：Contract、属性、Command 与 Presenter。
- `SettingsSampleBootstrap.cs`：Route 初始化与 Navigator 所有权。
- `SettingsSampleViewProvider.cs`：纯代码示例 View 与 Lease。

继续阅读[生成绑定](binding.md)和[类型安全导航](navigation.md)。
