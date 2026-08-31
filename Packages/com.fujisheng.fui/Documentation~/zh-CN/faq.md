# 常见问题

## FUI 必须使用 Addressables 吗？

不需要。应用可以为任意资源后端实现 `IViewProvider`，也可以使用未来的可选 Provider Package。

## FUI 的绑定使用运行时反射吗？

生成绑定、Route 与 Factory 不依赖运行时类型扫描和激活。Editor 工具可以读取 Editor-only 元数据。

## TextMeshPro 是必需依赖吗？

FUI 同时支持 Legacy Text 和 TMP Element。内置 TMP 创建工具会引用 TextMeshPro。

## 新项目应该使用哪种属性模式？

Pure 模式。只有已有模型契约必须保留自动属性时才使用 Mixed。

## 一个 ViewModel 可以显示在多个 View 中吗？

可以。使用 `Route<T>.CreateViewVariant` 或 `OpenWithViewModel`，每个顶层投影仍拥有独立 ViewHandle。

## CLI 是必需的吗？

不是。CLI 属于独立可选 Package。
