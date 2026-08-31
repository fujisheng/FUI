# 问题排查

## 找不到生成的 Routes

确认程序集声明 `[ViewModule]`、ViewModel 使用 `partial`，并且目标 `.asmdef` 目录中存在带 `RoslynAnalyzer`、`SourceGenerator` Label 的 Generator DLL。

## 生成类型重复

删除目标程序集目录中的旧版或重复 Generator，再重新执行 **Install Generator**。

## Mixed 属性没有更新

确认模块选择 Mixed 模式、安装 `com.unity.nuget.mono-cecil`，并且 `FUI.Editor.ILPostProcessing` 的 Version Define 已生效。

## View 验证失败

每个 `[Bind]`、`[Command]` 名称必须精确匹配 Prefab 中唯一 Element，并且目标成员类型兼容。嵌套容器不能重复拥有子 Element。

## 可选 Package 安装失败

确认 OpenUPM 已为 `com.fujisheng` 配置、版本已发布，且没有私有 Registry 覆盖同一 Scope。Setup 会显示 Package Manager 原始错误。

## Unity 编译结果不明确

等待 Unity 编译结束，再检查最新 Console Error 和 `Editor.log`。Generator 的 `dotnet` 测试不能代替 Unity 编译门禁。
