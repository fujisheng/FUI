# Page Wizard 与验证

> `FUI/Page Wizard` 与 `FUI/Validate Views` 位于独立安装的 [`com.fujisheng.fui.ugui`](../../../com.fujisheng.fui.ugui/README.md) 包（`FUI.Rendering.UGUI.Editor` 程序集）。

`FUI/Page Wizard` 使用 FullScreen、Popup 或 Tips Policy 预设创建 View Prefab。生成结构只是起点，View Contract 仍是绑定要求的来源。

`FUI/Validate Views` 检查 Element 缺失、类型不匹配、重名、嵌套边界和页面结构。同一套规则在 Prefab 保存后增量执行，并在构建前作为门禁执行。

验证错误必须阻断发布，禁止用字符串运行时查找绕过诊断。

发布前需要验证全部 Prefab、运行 Sample PlayMode，并确认生成绑定诊断为零。
