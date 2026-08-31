# FUI 1.0 推广执行手册

每周只生产一个技术主题：周二发布中文母稿，周四发布英文改写。每篇内容只链接一个明确的文档页，包含一个可复现示例，并以具体反馈问题结尾，不单纯索要 Star。

## 发布门禁

- Gitea `main`、GitHub `main` 与正式标签指向同一提交。
- `tools/verify-package.sh com.fujisheng.fui` 与 `tools/verify-repository.sh` 全部通过。
- 空白 Unity 6000 工程能够从 OpenUPM 安装 Package。
- Settings Sample 不依赖私有素材和外部工具，十分钟内可以运行。
- 首次公告前启用 GitHub Discussions 与 Issue Template。

## 八周内容日历

| 周次 | 技术主题 | 主要证据 |
|---|---|---|
| 1 | FUI 1.0 与编译期 MVVM | Settings Sample |
| 2 | Source Generator 绑定 | 生成属性与 Command 代码 |
| 3 | Typed Navigation | Route 与 `ViewHandle` 示例 |
| 4 | 异步生命周期 | Open、Cover、Reveal、Close 跟踪 |
| 5 | Resource Lease | 取消与释放测试 |
| 6 | 自定义 Element | 最小 Element 实现 |
| 7 | Editor 验证工具 | Page Wizard 与验证输出 |
| 8 | 扩展边界与独立 AI 工具链 | Package 依赖图与安装器 |

## 国内渠道

- 知乎：问题背景、设计推导、取舍和完整结果，作为中文母稿。
- 掘金：压缩背景，增加可复制代码与 Unity 操作步骤。
- CSDN、Unity 开发者社区：安装、Sample 与常见问题教程。
- V2EX：只发布一次 1.0 项目帖，说明适用人群、限制和反馈入口。
- SegmentFault、博客园：同步最耐久的架构文章并标注原文链接。
- 开源中国：提交项目页与正式 Release，不重复刷短动态。

## 海外渠道

- GitHub Release 与 Discussions：正式发布说明、Roadmap 与支持讨论。
- Unity Discussions Community Showcases：Unity 工作流和短演示。
- DEV Community：完整英文技术文章。
- Reddit `r/Unity3D`：包含实现细节和限制的简洁技术帖。
- Show HN：第 8 周、已有试装反馈和公开使用证据后发布一次。
- Medium、Hashnode、LinkedIn：只转载表现最好的文章并指向原文。

## 单篇检查清单

- 标题说明一个技术结果，避免宽泛框架口号。
- 首屏包含截图或 GIF、支持的 Unity 版本、MIT License 和安装命令。
- 示例来自已发布标签中的 Settings Sample。
- 明确限制与非目标。
- 发布后全部链接改为带标签的稳定文档 URL。
- 每篇只询问一种反馈：安装、API 形状或缺失集成。
- 记录发布 URL、日期、24 小时和 7 天后的访问来源、安装问题、Star 与讨论回复。

## 可复用发布短文

> FUI 1.0 是面向 Unity 6000 的 MIT 开源 UI 框架，核心包括编译期 MVVM、Typed Navigation、显式异步生命周期和 Resource Lease。仓库包含独立 Settings Sample 与 Package 验证脚本。现阶段重点收集安装可靠性和公共 API 反馈，再决定第一方扩展方向。
