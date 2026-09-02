# 安装

## 要求

- Unity 6000.0 或更高版本。
- uGUI 2.0.0 或更高版本。
- 使用生成绑定的 ViewModel 必须位于具有 `.asmdef` 的程序集。

## OpenUPM

正式发布版本通过 OpenUPM 安装。在 **Edit → Project Settings → Package Manager** 中添加 Scoped Registry：

| 字段 | 值 |
|---|---|
| Name | `package.openupm.com` |
| URL | `https://package.openupm.com` |
| Scope | `com.fujisheng` |

FUI/Setup 不会自动修改 Scoped Registry；安装可选 FUI 工具或扩展前，请自行完成这一次配置。

打开 **Window → Package Manager**，选择 **+ → Add package by name**，安装 `com.fujisheng.fui`。选择最新已发布版本（当前为 `1.0.3`）；可用版本见 [FUI OpenUPM 页面](https://openupm.com/packages/com.fujisheng.fui/)。

如果直接维护 `Packages/manifest.json`，请将以下字段合并到现有文件，不要替换已有 Registry 或依赖：

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.fujisheng"
      ]
    }
  ],
  "dependencies": {
    "com.fujisheng.fui": "1.0.3"
  }
}
```

## Git URL

通过 **Package Manager → Add package from git URL** 安装；系统必须已安装 Git，且 Unity 可以调用它：

```text
https://github.com/fujisheng/FUI.git?path=/Packages/com.fujisheng.fui#com.fujisheng.fui/1.0.3
```

`path` 用于从 Monorepo 选择目标 Package，标签固定到已发布的不可变版本。升级时只替换为对应的 Package 前缀标签。不要同时通过 Git 和 OpenUPM 引入同一 Package。

## Generator

打开 **FUI → Setup**，选择包含 ViewModel 的程序集定义，然后点击 **Install Generator**。Pure 模式不需要额外依赖；Mixed 模式还要安装官方 Cecil Package。

团队项目应提交安装到目标程序集目录的 Generator DLL 及其 `.meta`，不要提交 `Library`。
