# ViewModel and generated binding

`[ViewModule]` defines the generated namespace and property mode. `[ViewContract]` connects one ViewModel to one named View. `[Bind]` connects a ViewModel property to an Element bindable property, while `[Command]` connects a method to an Element command.

```csharp
[ObservableProperty]
[Bind("Volume", nameof(SliderElement.Value), BindingMode.TwoWay)]
float volume = 0.5F;

[Command("Close", nameof(ButtonElement.OnClick))]
public void Close() { }
```

Pure mode requires `[ObservableProperty]` fields. Mixed mode permits automatic properties and uses optional IL post-processing. Prefer Pure mode unless an existing public model contract requires automatic properties.

Analyzer diagnostics are build errors for invalid element names, incompatible types, unsupported members, duplicate contracts, or missing partial declarations. Fix diagnostics instead of bypassing generated binding with reflection.
