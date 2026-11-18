# FUI 纯 .NET 合约测试

这些项目直接编译 `Packages/com.fujisheng.fui/Runtime/Core` 与 `Runtime/Navigation` 的同一份源码，不引用 UnityEngine/UnityEditor，不维护运行时副本。Runtime 目标为 netstandard2.1，测试为 net8.0，使用仓库指定的已安装 SDK。

```sh
dotnet restore tools/dotnet/FUI.Portable.Tests.csproj
dotnet test tools/dotnet/FUI.Portable.Tests.csproj --no-restore
```

本目录的 NuGet.Config 禁用远程源；只使用已经缓存的固定版本依赖。缓存不完整时 restore 明确失败，不自动联网或安装/升级软件。隔离 CLI_HOME 时可显式设置 NUGET_PACKAGES 指向已有缓存。

测试包括原集合/导航/异步队列源码，以及新增真实生成代码执行。生成器通过独立 alias 引入，动态编译只引用实际 Core/Navigation，不使用生成器的 FUIStubs。Pure 模式生成 setter、绑定和 Command；测试在两个投影间共享真实 ViewModel，检查独立解绑和销毁后不响应。

`HeadlessProbe.cs.txt` 是测试用托管元素，不是已发布的通用 Headless 后端。它不实现布局、像素、命中测试、焦点或平台输入。原 uGUI/Mixed/游戏验收仍需受支持的 Unity 环境；纯 .NET 绿灯不能替代 `tools/verify-package.sh`。

原 `TestExecutionContext` 是历史行为测试替身，其 IsMainThread 恒真，不证明真实线程调度。`DeterministicDispatchTests` 使用实际 dispatcher、后台线程和显式消息泵，验证完成、取消、回滚、迟到资源在捕获线程提交。

Settings 测试直接读取仓库原 Sample ViewModel 和 Pure 程序集配置，与真实生成器/Runtime 编译，检查双向绑定、缺失控件、关闭、共享 VM 及 Navigator 缓存换 VM。参数命令夹具直接统计生成方法调用；异步测试等待业务方法实际结束再断言。合作式取消依赖业务方法检查 token。

`MonoDispatcherProbe.cs.txt` 与同份 dispatcher 源文件用 Unity 自带 mcs 编译、独立 mono 执行，覆盖 safe OnCompleted 与 ExecutionContext。它不启动 Unity，也不验证真实 UnitySynchronizationContext、Editor/Player 或 IL2CPP。



## 可复用Headless Testing

FUI.Testing.csproj直接编译Testing~/Headless，引用真实Core，详见[Testing说明](../../Packages/com.fujisheng.fui/Testing~/README.md)。原Probe/Settings夹具保留，已有88项基线不被迁移吞掉。新增GeneratedListBindingTests用实际Generator/Runtime驱动根列表与行Projection，覆盖集合变化、换源、行文本/命令、共享VM、隐藏、真实Navigator缓存重开与失败清理。新Testing程序集有无Unity引用检查；不代表Unity渲染或游戏验收。当前只保留隔离实现，未推广原库。
