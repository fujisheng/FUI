# 问题排查

## 找不到生成的 Routes

确认程序集声明 `[ViewModule]`、ViewModel 使用 `partial`，并且目标 `.asmdef` 目录中存在带 `RoslynAnalyzer`、`SourceGenerator` Label 的 Generator DLL。

## 生成类型重复

删除目标程序集目录中的旧版或重复 Generator，再重新执行 **Install Generator**。

## Mixed 属性没有更新

确认模块选择 Mixed 模式、安装 `com.unity.nuget.mono-cecil`，并且 `FUI.Editor.ILPostProcessing` 的 Version Define 已生效。

普通 `-batchmode -quit` 的首次编译可能早于编辑器协调器订阅，且 Unity 会在延迟检查前退出。FUI 同时在官方 `EditorApplication.quitting` 事件中检查配置的目标，退出时不请求域重载。这会准备**下次**加载的 DLL，不能修复首次冷启动 `-executeMethod` 动作已经加载的 ViewModel。

此场景采用“编译并退出 → 独立测试/动作进程”的流程。`-runTests` 不加 `-quit`，等待 Test Runner 完成，并要求新生成的 XML 用例数大于零、失败为零。C# 编译无错或 PDB 有效，都不能单独证明 Mixed 通知已织入。

[批处理回归夹具](../../../../tools/fixtures/README.md)和 `tools/verify-batch-quit.py` 跨两次启动验证真实 Mixed/Pure 通知及相同值去重。非零退出、编译错误、任一种已知 ILPP 失败前缀或缺少成功标记都会失败。脚本会恢复专用测试源，属于回归验证器，不能当作缓存准备命令；要求空闲的隔离夹具工程，不安装包、不手动修补二进制。

## View 验证失败

每个 `[Bind]`、`[Command]` 名称必须精确匹配 Prefab 中唯一 Element，并且目标成员类型兼容。嵌套容器不能重复拥有子 Element。

## 可选 Package 安装失败

确认 OpenUPM 已为 `com.fujisheng` 配置、版本已发布，且没有私有 Registry 覆盖同一 Scope。Setup 会显示 Package Manager 原始错误。

## Unity 编译结果不明确

等待 Unity 编译结束，再检查最新 Console Error 和 `Editor.log`。Generator 的 `dotnet` 测试不能代替 Unity 编译门禁。
