# FUI 1.0 launch kit

This maintainer document turns one technical topic into a Chinese post on Tuesday and an English post on Thursday. Every post should link to one exact documentation page, include one reproducible code sample, and end with a concrete request for feedback rather than a generic request for Stars.

## Release gate

- Gitea `main`, GitHub `main`, and the release tag resolve to the same commit.
- `tools/verify-package.sh com.fujisheng.fui` and `tools/verify-repository.sh` pass.
- The package installs from OpenUPM into a blank Unity 6000 project.
- Settings Sample opens in less than ten minutes without a private asset or external tool.
- GitHub Discussions and issue templates are enabled before the first announcement.

## Eight-week editorial calendar

| Week | Technical story | Primary proof |
|---|---|---|
| 1 | FUI 1.0 and compile-time MVVM | Settings Sample |
| 2 | Source Generator binding | Generated property and command code |
| 3 | Typed navigation | Route and `ViewHandle` example |
| 4 | Asynchronous lifecycle | Open, cover, reveal, close trace |
| 5 | Resource Lease | Cancellation and disposal test |
| 6 | Custom Element | Small Element implementation |
| 7 | Editor validation | Page Wizard and validation output |
| 8 | Extension boundaries and external AI tools | Package graph and installer |

## Channel adaptation

### Chinese

- 知乎：问题背景、设计推导、取舍、完整结果，作为中文母稿。
- 掘金：压缩背景，增加可复制代码和 Unity 操作步骤。
- CSDN、Unity 开发者社区：安装、Sample 与常见问题教程。
- V2EX：只发布一次 1.0 项目帖，重点说明适用人群、限制和反馈入口。
- SegmentFault、博客园：同步最耐久的架构文章并使用 canonical link。
- 开源中国：提交项目页和正式 Release，不重复发布短动态。

### International

- GitHub Release and Discussions: canonical release notes, roadmap, and support threads.
- Unity Discussions Community Showcases: Unity-specific workflow and a short demo.
- DEV Community: the complete English technical article.
- Reddit `r/Unity3D`: a concise technical post with implementation details and limitations.
- Show HN: publish once in week 8 after installation feedback and public usage evidence exist.
- Medium, Hashnode, and LinkedIn: syndicate only the strongest article and link to the canonical version.

## Post checklist

- Title states one technical outcome, not a broad framework claim.
- First screen includes a GIF or screenshot, supported Unity version, MIT license, and install command.
- Example is copied from a tagged version of Settings Sample.
- Limitations and non-goals are explicit.
- Links use the tagged documentation URL after release.
- Discussion prompt asks for one kind of feedback: installation, API shape, or missing integration.
- Record publication URL, date, referrals, install issues, Stars, and discussion replies after 24 hours and 7 days.

## Reusable short release copy

> FUI 1.0 is an MIT-licensed Unity 6000 UI framework built around compile-time MVVM, typed navigation, explicit asynchronous lifecycle, and resource leases. The repository includes a self-contained Settings Sample and package verification scripts. I am looking for feedback on installation reliability and the public API before expanding official integrations.
