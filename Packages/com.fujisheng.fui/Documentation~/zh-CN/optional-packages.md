# 可选 Package

FUI Core 在没有任何可选 Package 时保持完整功能。

`Tools/FUI/Setup` 会显示以下外部工具。在固定兼容版本发布到 OpenUPM 前，安装按钮保持禁用：

| Package | 仓库 | 用途 |
|---|---|---|
| `com.fujisheng.unitycli` | `fujisheng/unitycli` | Unity 自动化桥接 |
| `com.fujisheng.fui.cli` | `fujisheng/fui-cli` | 面向 AI Agent 的 FUI 自动化 |

两个工具使用独立仓库。发布完成后，安装 FUI CLI 会自动先安装 UnityCli，并在 Core 的 Patch 版本中启用对应安装项。

未来直接实现 FUI Provider、Transition、Element 或 Validator 的第一方扩展可以进入 FUI Monorepo，并使用独立 SemVer。
