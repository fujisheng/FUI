# UGUI Element 扩展指南

FUI Element 采用适配器/包装器结构。Unity Component 是视觉状态来源；Element 只负责绑定、生命周期、事件方向和资源所有权。Element 不继承 `Image`、`Button` 等 Unity 组件。

```text
Element
└── RectTransformElement
    ├── ComponentElement<TComponent>
    ├── GraphicElement
    │   ├── GraphicElement<TGraphic>
    │   └── TextElement
    │       ├── LegacyTextElement
    │       └── TMPTextElement
    └── SelectableElement
        ├── SelectableElement<TSelectable>
        ├── InputFieldElement
        │   ├── LegacyInputFieldElement
        │   └── TMPInputFieldElement
        └── DropdownElement
            ├── LegacyDropdownElement
            └── TMPDropdownElement
```

## 生命周期

Element 由 View 初始化和释放。自定义 Element 在 `OnInitialize()` 中调用 `CreateProperty<T>()`、`Track()` 和 `TrackCleanup()` 注册资源。框架会按注册顺序的逆序统一清理；初始化失败也执行相同回滚，重复释放不会再次清理。

```csharp
[RequireComponent(typeof(MyComponent))]
[AddComponentMenu("FUI/Elements/My Component")]
public sealed class MyComponentElement : ComponentElement<MyComponent>
{
    public BindableProperty<float> Value { get; private set; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Value = CreateProperty(Component.Value, (_, value) => Component.SetValueWithoutNotify(value));
        Component.ValueChanged += OnValueChanged;
        TrackCleanup(() => Component.ValueChanged -= OnValueChanged);
    }

    void OnValueChanged(float value)
    {
        Value.Value = value;
    }
}
```

不要依赖派生类手工 `Dispose()` Property、Command 或 Lease，也不要在运行时错误信息中使用本地化文本。框架日志和异常统一使用英语。

## Legacy 与 TMP

公共绑定只依赖抽象 Element：

```csharp
[Bind("Title", nameof(TextElement.Content))]
public string Title { get; set; }
```

View 会用基类查询匹配 Prefab 中的 `LegacyTextElement`、`TMPTextElement` 或自定义派生类。只有需要后端独有能力时才使用具体类型：

```csharp
[Bind("Title", nameof(TMPTextElement.AutoSize))]
public bool AutoSize { get; set; }
```

Text、InputField 和 Dropdown 的具体 Element 与 Unity Component 必须预置在 Prefab 中。框架不会在运行时创建 Adapter 或后端组件。

自定义文本组件继承 `TextElement<TText>`，实现 `TextValue`、`FontSizeValue`、`AlignmentValue` 和 `RichTextValue`。如果自定义实现直接依赖 TMP 功能，可以继承 `TMPTextElement`，例如项目的本地化文本扩展。

## 事件方向

双向控件必须遵守固定方向：

```text
ViewModel -> BindableProperty -> SetValueWithoutNotify -> Unity Component
用户输入 -> Unity Event -> BindableProperty + Command
```

ViewModel 写入不得触发业务 Command。Unity Listener 必须通过 `TrackCleanup()` 移除，Command 使用 `Track(new Command<T>())` 注册。

## 资源切换

Sprite、Texture、Material 和动态 View 使用事务式 Lease 切换：先加载候选 Lease，验证资源，更新组件，再替换当前 Lease，最后释放旧 Lease。加载失败必须保留旧资源、旧 Source 和旧 Lease；直接设置 Unity 对象或将 Source 清空时释放已有 Lease。

## 嵌套投影

- `NestedViewElement` 只持有投影生命周期，静态 GameObject 由父 View 持有。
- `DynamicViewElement` 独占实例 Lease 与投影；Source 和 ViewModel 的赋值顺序不影响最终结果。
- `ProjectionTemplate.Create<TViewModel>()` 返回受限的 `ProjectionHandle<TViewModel>`，不会暴露 View、Element、BindingContext 或 ViewInstance。
- 自定义容器实现 `IContainerElement`，避免父 View 递归扫描并重复拥有子投影 Element。

## 列表

所有列表统一使用 `Items`、`SelectedIndex`、`SelectedItem` 和 `TryGetBoundItemViewModel()`。`Items = null` 等价于 Reset。

- `RecyclingListElement`：按历史最大数量创建并复用实例。
- `SlotListElement`：绑定已有固定子节点，超出容量的项目隐藏。
- `AnchoredListElement`：将实例放入固定 Anchor，容量规则与 Slot 相同。
- `ScrollListElement`：组合 Recycling List 与 ScrollRect 行为。
- `ScrollViewElement`：只包装普通 ScrollRect，不承担列表职责。

本版本不提供虚拟化。未来的 `VirtualizedListElement` 会沿用相同 Items 和选择 API。
