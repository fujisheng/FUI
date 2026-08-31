# 安装

## 要求

- Unity 6000.0 或更高版本。
- uGUI 2.0.0 或更高版本。
- 使用生成绑定的 ViewModel 必须位于具有 `.asmdef` 的程序集。

## OpenUPM

为 `com.fujisheng` 配置 OpenUPM Scoped Registry，然后通过 Package Manager 安装 `com.fujisheng.fui@1.0.1`。

## Git URL

通过 **Package Manager → Add package from git URL** 安装：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.1
```

## Generator

打开 **Tools → FUI → Setup**，选择包含 ViewModel 的程序集定义，然后点击 **Install Generator**。Pure 模式不需要额外依赖；Mixed 模式还要安装官方 Cecil Package。

团队项目应提交安装到目标程序集目录的 Generator DLL 及其 `.meta`，不要提交 `Library`。
