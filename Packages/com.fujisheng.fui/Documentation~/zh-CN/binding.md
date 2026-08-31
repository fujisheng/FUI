# ViewModel 与生成绑定

`[ViewModule]` 定义生成命名空间与属性模式；`[ViewContract]` 将 ViewModel 连接到命名 View；`[Bind]` 连接 ViewModel 属性和 Element 属性；`[Command]` 连接方法和 Element Command。

```csharp
[ObservableProperty]
[Bind("Volume", nameof(SliderElement.Value), BindingMode.TwoWay)]
float volume = 0.5F;

[Command("Close", nameof(ButtonElement.OnClick))]
public void Close() { }
```

Pure 模式使用 `[ObservableProperty]` 字段。Mixed 模式支持自动属性并使用可选 IL Post Processing。新项目默认使用 Pure 模式。

Element 名称、类型、成员、重复 Contract 或 `partial` 声明错误都会产生 Analyzer 诊断。应修复诊断，禁止用运行时反射绕过生成绑定。
