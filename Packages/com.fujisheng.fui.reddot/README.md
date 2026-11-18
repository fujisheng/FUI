# FUI Red Dot

[中文](README.zh-CN.md)

`FUI Red Dot` is an optional FUI extension for strongly typed notification graphs. It keeps business rules behind neutral pull and push ports, batches changes, aggregates a directed acyclic graph, and updates `FUI.RedDot.UGUI.RedDotElement` by exact key subscription.

Domain entry points keep the `RedDot` prefix to avoid collisions with FUI and project types such as `Element`, `Runtime`, and `State`. Concrete algorithms remain concise: `SumAggregator`, `AnyAggregator`, and `MaxAggregator`.

See [Documentation](Documentation~/index.md) for setup and authoring.
