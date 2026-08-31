# FUI.Generators

`FUI.Generators` 是 FUI 3.0 的 Source Generator 与 Analyzer 程序集，目标框架为 `netstandard2.0`。

## 输出

- 可观察属性通知与 `[ObservableProperty]` 字段属性。
- 每个 ViewModel/View 组合的隐藏 BindingContext。
- `${ModuleNamespace}.Routes` 强类型 Route Catalog。
- 同命名空间的 internal `BindingRegistry`。
- Editor 使用的隐藏 `BindingManifest`。

生成器从程序集上的 `ViewModuleAttribute` 读取模块命名空间。未配置时使用 `${AssemblyName}.UI`。每个程序集生成自己的 Catalog，不输出产品名或固定项目语义类型。

## 属性模式

- Pure：只接受 `[ObservableProperty]` 字段模式。
- Mixed：字段模式与自动属性模式同时启用；自动属性通知由可选 IL PostProcessor 注入。
- 两种模式共用同一绑定生成路径和通知委托。

## 构建与测试

```bash
dotnet restore FUI.Generators.sln --locked-mode
dotnet build FUI.Generators.sln -c Release --no-restore
dotnet test Tests/FUI.Generators.Tests.csproj -c Release --no-build --no-restore
```

或直接执行：

```bash
./verify.sh
```

构建产物为：

```text
bin/Release/netstandard2.0/FUI.Generators.dll
```

`verify.sh` 会将其与 `../Roslyn~/FUI.Generators.dll` 比较。安装工具复制预编译 DLL，不要求使用者本机安装 .NET SDK。
