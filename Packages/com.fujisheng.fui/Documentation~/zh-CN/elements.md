# uGUI Element

> 这些 Element 位于独立安装的 [`com.fujisheng.fui.ugui`](../../../com.fujisheng.fui.ugui/README.md) 包（`FUI.Rendering.UGUI` 程序集）。

Element 将 Unity Component 适配为 FUI 可绑定属性和 Command。Unity Component 保持视觉状态来源，Element 负责绑定状态、事件方向、清理和动态资源 Lease。

内置类型覆盖 Text、Image、Button、Toggle、Slider、Input、Dropdown、EventTrigger、ScrollView、List、Nested View 和 Dynamic View。

双向控件遵循固定方向：

```text
ViewModel → BindableProperty → SetValueWithoutNotify → Unity Component
用户输入 → UnityEvent → BindableProperty 与 Command
```

绑定时优先依赖能表达需求的最通用 Element。例如普通文本使用 `TextElement.Content`，只有使用 TMP 独有能力时才绑定具体 TMP Element。
