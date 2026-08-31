# FUI 3.0 迁移指南

FUI 3.0 是一次性主版本切换，不提供旧 API 别名。

## 程序集与命名空间

- `FUI.Bindable` 改为 `FUI.Binding`。
- `FUI.Manager` 改为 `FUI.Navigation`。
- `FUI.UGUI` 改为 `FUI.Rendering.UGUI`。
- UGUI 内置 Element 位于 `FUI.Rendering.UGUI.Elements`。
- `FUI.Core` 不再引用 Unity 或无关基础设施。

## 导航

| 旧名称 | 3.0 名称 |
|---|---|
| `UIManager` | `Navigator` |
| `UIRoute<T>` | `Route<T>` |
| `UIHandle` | `ViewHandle` |
| `UIRouteSettings` | `RoutePolicy` |
| `UIOpenOptions` | `OpenOptions` |
| `UIInstanceState` | `ViewState` |

`FullScreen`、`Unstackable`、`AllowMultiple` 复合标志已删除，改用正交配置：

```csharp
[RoutePolicy(
    CoverageMode = CoverageMode.Hide,
    HistoryMode = HistoryMode.Transient,
    AllowMultiple = true)]
```

## 绑定

| 旧名称 | 3.0 名称 |
|---|---|
| `[ViewBinding]` | `[ViewContract]` |
| `[Binding]` | `[Bind]` |
| `[PresenterViewModel]` | `[PresenterFor]` |
| `[PropertyChanged]` | `[OnChanged]` |
| `[ObservablePropertyIgnore]` | `[IgnoreObservable]` |

为每个包含 ViewModel 的程序集添加：

```csharp
[assembly: FUI.Binding.ViewModule("Sample.UI")]
```

生成入口由固定的项目语义名称改为 `${ModuleNamespace}.Routes`；绑定注册由 `Routes.Initialize()` 内部完成。

## 渲染扩展

| 旧名称 | 3.0 名称 |
|---|---|
| `UIElement` | `RectTransformElement` |
| `UIElement<T>` | `ComponentElement<T>` |
| `ViewElement` | `NestedViewElement` |
| `PrototypeElement` | `ViewPrototypeElement` |
| `PositionConversionElement` | `ScreenPositionElement` |
| `ListViewElement` | `ListElement` |
| `StaticListViewElement` | `RecyclingListElement` |
| `FixedListViewElement` | `SlotListElement` |
| `FixedPositionListViewElement` | `AnchoredListElement` |
| `ScrollRectElement` | `ScrollListElement` |
| `EventTriggerElement` | `InteractionElement` |
| `FormatString` | `FormattedText` |

成员同步改名：

| 旧成员 | 3.0 成员 |
|---|---|
| `TextElement.TextObject` | `TextElement.Content` |
| `TextElement.FormatText` | `TextElement.FormattedText` |
| `NestedViewElement.Data` | `NestedViewElement.ViewModel` |
| `ViewPrototypeElement.Prototype` | `ViewPrototypeElement.Template` |
| `ListViewElement.List` | `ListElement.Items` |

`ButtonElement` 不再代理子文本和图片。请把相关绑定直接指向对应的 `TextElement` 或 `ImageElement`。

文本控件不再使用 `IText`、`LegacyTextAdapter` 或 `TMPTextAdapter`。FUI 3.0 的项目资产已经完成以下一次性迁移：

- `TextElement + Text` → `LegacyTextElement`
- `TextElement + TextMeshProUGUI` → `TMPTextElement`
- `InputFieldElement + InputField` → `LegacyInputFieldElement`
- `DropdownElement + Dropdown` → `LegacyDropdownElement`
- 删除旧 Text Adapter

一次性迁移代码已从正式包删除，不作为长期兼容层保留。公共绑定继续使用 `nameof(TextElement.Content)`；只有访问独有属性时才使用 `LegacyTextElement` 或 `TMPTextElement`。

业务层不再接触实体或 BindingContext。手动嵌套投影改用 `ProjectionHandle<TViewModel>`，它只暴露 ViewModel、存活状态、激活、停用和释放。

## 属性模式

- Pure：使用 `[ObservableProperty]` 字段，由 Generator 生成属性；绑定自动属性会报告 `FUI0013`。
- Mixed：字段与自动属性并存；自动属性由可选 IL PostProcessor 注入通知。

主包不再直接依赖 Cecil。Mixed 模式通过 `Tools/FUI/Setup` 按需安装 Unity 官方 Cecil 包。

## 验收

1. 运行 Generator 的 `verify.sh`。
2. 执行 `Tools/FUI/Validate Views`。
3. 确认所有 Prefab 通过 Element、类型、重名与嵌套边界检查。
4. 等待 Unity Refresh 与 Domain Reload 完成。
5. 确认 Console 与 `Editor.log` 没有新的编译错误。
