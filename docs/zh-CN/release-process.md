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

每个 Package 首次导入 OpenUPM 时单独提交，并配置 `gitTagPrefix: <package-id>/`。Package 被收录后，GitHub Release Workflow 会将每个推送到 GitHub 的 Package 前缀标签提交给 OpenUPM，并等待对应版本变为可安装状态。

OpenUPM 步骤只会在标签推送，或手动触发 Workflow 且明确填写标签时执行。普通 `main` 推送仅创建或补建 GitHub Release，不触发 OpenUPM 扫描。检查 GitHub Actions 结果后，再在干净 Unity 工程验证已发布版本。

已发布标签不得移动或重建，错误版本通过新的 Patch 修复。
