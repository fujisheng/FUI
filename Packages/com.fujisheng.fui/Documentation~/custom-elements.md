# Custom Elements

Derive from `ComponentElement<TComponent>` when adapting a Unity component. Create bindable state in `OnInitialize`, register event cleanup with `TrackCleanup`, and let the base lifetime release tracked objects in reverse order.

```csharp
[RequireComponent(typeof(MyControl))]
public sealed class MyControlElement : ComponentElement<MyControl>
{
    public BindableProperty<float> Value { get; private set; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Value = CreateProperty(Component.Value,
            (_, value) => Component.SetValueWithoutNotify(value));
        Component.ValueChanged += OnValueChanged;
        TrackCleanup(() => Component.ValueChanged -= OnValueChanged);
    }

    void OnValueChanged(float value) => Value.Value = value;
}
```

Initialization must be transactional: if any step fails, all previously registered state is released. Do not manually dispose properties already registered with `Track`.

Implement `IContainerElement` when a custom container owns child projection boundaries and the parent View must not recursively own those children.
