# 发布流程

所有 Package 使用独立 SemVer 与包名前缀标签。

## 准备

1. 更新目标 `package.json.version`。
2. 将 CHANGELOG 的 Unreleased 内容归入同一版本。
3. 运行 `tools/verify-package.sh <package-id>`。
4. 运行 `tools/verify-repository.sh`。
5. 合并到 Gitea `main`。
6. 确认 GitHub 镜像提交一致。

## 标签

```bash
git tag -a com.fujisheng.fui/1.0.0 -m "FUI Core 1.0.0"
git push origin com.fujisheng.fui/1.0.0
```

确认 Gitea 与 GitHub 标签解析到同一发布提交。GitHub Actions 负责创建或更新对应 Release。

## OpenUPM

每个 Package 单独提交，`gitTagPrefix` 配置为 `<package-id>/`。等待版本可安装后，在干净 Unity 工程完成最终验证，再发布推广内容。

已发布标签不得移动或重建，错误版本通过新的 Patch 修复。

只有以上门禁全部通过后才开始对外发布内容，八周节奏和各渠道写法见[推广执行手册](launch-kit.md)。
