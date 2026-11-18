# uGUI Elements

> These Elements ship in the separately installed [`com.fujisheng.fui.ugui`](../../com.fujisheng.fui.ugui/README.md) package (`FUI.Rendering.UGUI` assembly).

Elements adapt Unity components to FUI bindable properties and commands. The Unity component remains the visual source of truth; the Element owns binding-facing state, event direction, cleanup, and dynamic resource leases.

Built-in groups include text, image, button, toggle, slider, input, dropdown, event trigger, scroll views, lists, nested views, and dynamic views.

Two-way controls follow one direction rule:

```text
ViewModel → BindableProperty → SetValueWithoutNotify → Unity component
User input → UnityEvent → BindableProperty and Command
```

Bind to the most general Element type that exposes the required member. For example, bind text through `TextElement.Content` unless an application specifically requires a TMP-only feature.
