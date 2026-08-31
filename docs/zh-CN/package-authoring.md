# 第一方扩展包开发规范

## 命名

```text
Package:   com.fujisheng.fui.addressables
Assembly:  FUI.Extensions.Addressables
Namespace: FUI.Extensions.Addressables
Tag:       com.fujisheng.fui.addressables/0.1.0
```

Package 目录必须与 `package.json.name` 一致。没有真实需求时不创建空扩展。

## 必需结构

每个 Package 必须包含 `Runtime`、`Tests`、`Samples~`、`Documentation~`、`README.md`、`CHANGELOG.md`、`LICENSE.md` 和 `package.json`。只有存在 Editor 行为时才增加 `Editor`。

## 依赖

- 扩展通过已发布 SemVer 依赖 `com.fujisheng.fui`。
- Core 不得依赖扩展。
- 扩展之间的依赖必须显式声明且不能形成循环。
- 禁止使用 `InternalsVisibleTo` 访问 Core 内部实现。
- 禁止复制 Core 内部代码到扩展。

## 模板与验收

复制 `tools/templates/extension-package` 并替换占位值，然后更新根 Package Catalog 和验证矩阵。发布前运行 `tools/verify-package.sh <package-id>` 与 `tools/verify-repository.sh`。
