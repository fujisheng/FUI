# Changelog

## 当前预览更新

- 接入 URUI 0.8 编译文档、独立页面及 native render plan。
- URP 从共享 Surface 提交绘制，并处理 GPU 资源退役。

## [0.2.1-preview.1] - 2026-11-10

- 首个显隐租约落实作者初始隐藏状态，避免同值false绑定未实际隐藏。
- 恢复作者display并保持同节点共享租约；要求URUI 0.7.1-preview.1。

## [0.1.0-preview.1] - 2026-11-03

- Added the initial `FUI.Rendering.URUI` backend: single Document stage, layer/page
  scopes, view provider, and text/button/toggle/slider/visibility/color adapters.
- Added `Document.RenderCommandBuffer` support in URUI 0.2 for URP renderer features.
