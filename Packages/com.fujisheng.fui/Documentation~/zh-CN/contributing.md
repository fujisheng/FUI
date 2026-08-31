# 贡献

仓库级规则见 [`CONTRIBUTING.md`](../../../../CONTRIBUTING.md)。

修改 Core 时：

1. 保持 AOT 与 IL2CPP 兼容。
2. Core 不得依赖可选 Package。
3. 行为修改必须增加 EditMode 或 PlayMode 测试。
4. 生成 API 或诊断修改必须增加 Generator 测试。
5. 更新文档和 `CHANGELOG.md`。
6. 运行 `tools/verify-package.sh com.fujisheng.fui` 与 `tools/verify-repository.sh`。

公共 API 修改必须明确 SemVer 影响和迁移方式。
