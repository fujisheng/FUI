# FUI Source Generator

FUI Framework的Source Generator实现，用于在编译时自动生成数据绑定代码。

## 项目结构

```
FUI.SourceGenerator/
├── Core/                              # 核心接口和工具类
│   ├── IContextInfoGenerator.cs       # 上下文信息生成器接口
│   ├── DiagnosticDescriptors.cs       # 诊断描述符
│   ├── BindingInfo.cs                 # 绑定信息数据结构
│   └── GenerationUtility.cs           # 代码生成工具类
├── Generators/                        # Source Generator实现
│   ├── ObservableObjectGenerator.cs   # 可观察对象生成器
│   └── BindingContextGenerator.cs     # 绑定上下文生成器
└── Analyzers/                         # 代码分析器
    └── ContextInfoByAttributeGenerator.cs # 特性分析器
```

## 主要功能

### 1. 可观察对象生成器 (ObservableObjectGenerator)
- 自动为带有`[Binding]`特性的属性生成PropertyChanged代码
- 生成backing field和事件委托
- 实现`ISynchronizeProperties`接口

### 2. 绑定上下文生成器 (BindingContextGenerator)
- 生成数据绑定逻辑代码
- 支持属性绑定和命令绑定
- 支持值转换器集成
- 生成绑定和解绑方法

### 3. 特性分析器 (ContextInfoByAttributeGenerator)
- 分析`[Binding]`和`[Command]`特性
- 提取绑定配置信息
- 支持多种绑定模式

## 使用方法

### 1. 在Unity项目中引用
将生成的DLL放入Unity项目的`Packages`目录下，并配置Assembly Definition引用。

### 2. 标记绑定属性
```csharp
[Binding("ButtonElement", nameof(ButtonElement.Text))]
public string ButtonText { get; set; }

[Binding("InputElement", nameof(InputElement.Value), bindingMode: BindingMode.TwoWay)]
public string InputValue { get; set; }
```

### 3. 标记命令绑定
```csharp
[Command("SubmitButton", nameof(ButtonElement.OnClick))]
public void OnSubmit() { /* 实现 */ }
```

## 生成的代码

### PropertyChanged代码
```csharp
public partial class MyViewModel : ISynchronizeProperties
{
    public string _ButtonText_BackingField;
    public PropertyChangedHandler<string> _ButtonText_Changed;
    
    void ISynchronizeProperties.Synchronize()
    {
        _ButtonText_Changed?.Invoke(this, this.ButtonText, this.ButtonText);
    }
}
```

### 数据绑定代码
```csharp
public partial class MyViewModel_MyView_Binding_Generated : BindingContext<MyViewModel>
{
    protected override void OnBinding()
    {
        this.vm._ButtonText_Changed += PropertyChanged__ButtonText_ButtonElement_Text;
    }
    
    protected override void OnUnbinding()
    {
        this.vm._ButtonText_Changed -= PropertyChanged__ButtonText_ButtonElement_Text;
    }
    
    private void PropertyChanged__ButtonText_ButtonElement_Text(object sender, string oldValue, string newValue)
    {
        this.View.GetElement<ButtonElement>("ButtonElement").Text = newValue;
    }
}
```

## 编译配置

项目配置为.NET Standard 2.0，确保与Unity兼容。主要依赖：
- Microsoft.CodeAnalysis.CSharp 4.0.1
- Microsoft.CodeAnalysis.Analyzers 3.3.4

## 诊断支持

提供完整的诊断信息：
- FUI001: 无效的绑定配置
- FUI002: 缺少ObservableObject继承
- FUI003: 无效的命令绑定
- FUI101: 属性生成信息

## 性能特性

- 支持增量生成，只在相关代码变更时重新生成
- 编译时代码生成，避免运行时反射
- 类型安全的绑定代码生成

## TODO
- 完善分析器诊断信息
- 支持绑定时目标路径可以为常量表达式
- 优化代码结构