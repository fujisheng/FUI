# FUI Red Dot 0.1

## Setup

Core types are in `FUI.RedDot`; Unity binding types are in `FUI.RedDot.UGUI`.

```csharp
using FUI.RedDot;
using FUI.RedDot.UGUI;
```

1. Add `com.fujisheng.fui.reddot` to the project.
2. Run **FUI > Red Dot > Setup Generator** for each assembly that contains a red dot catalog.
3. Declare a `static partial` catalog and call its generated `Register(center)` method.
4. Configure the Unity bridge with `RedDotRuntime.Configure(center)`.
5. Bind a generated `RedDotKey` to `FUI.RedDot.UGUI.RedDotElement.Key`.

`RedDotElement` only consumes `IRedDotReader`. Without a `RedDotScope` it follows the default Runtime, including late configuration, shutdown and replacement. Under a Scope it always uses the nearest parent Scope; an unconfigured Scope is an explicit isolation boundary and never falls back to the default center.

## Catalog

```csharp
[RedDotCatalog]
public static partial class GameRedDots
{
    [RedDotNode]
    public static readonly RedDotKey Home = RedDotKey.Root("Home");

    [RedDotNode(nameof(Home), Aggregator = typeof(SumAggregator))]
    public static readonly RedDotKey Mail = RedDotKey.Child(Home, "Mail");

    [RedDotFactory(nameof(Home))]
    public static RedDotKey HeroUpgrade(int heroId)
    {
        return RedDotKey.Dynamic(Home, "HeroUpgrade", heroId);
    }
}
```

`Register` performs explicit registration without reflection or assembly scanning. Graph mutation and publishing must occur on Unity's main thread.

## RedDotRuntime ports

- `IRedDotRule` is a pull contribution. Call its assigned invalidator when its dependencies change.
- `IRedDotSource` is a push producer. Publish by key through its assigned publisher.
- Disposing a registration removes only that rule or source's contributions.
- Registration handles follow the Center thread constraint; a wrong-thread `Dispose()` throws without consuming the handle, so the owner thread can retry safely.
- `Tick()` or `Flush()` commits one batch. Reentrant changes are deferred to the next batch.

## Scope and lifetime

```csharp
var defaultCenter = new RedDotCenter();
GameRedDots.Register(defaultCenter);
RedDotRuntime.Configure(defaultCenter);

var accountCenter = new RedDotCenter();
GameRedDots.Register(accountCenter);
scope.Configure(accountCenter); // Scope does not Tick or Dispose it.

// Project composition root shutdown order:
scope.Clear();
RedDotRuntime.Shutdown();
accountCenter.Dispose();
defaultCenter.Dispose();
```

Destroying the nearest Scope makes active descendants resolve the next outer Scope, or the default Runtime when no outer boundary exists. One failed Element subscription is logged and isolated; it never prevents sibling Elements from completing the same context switch.

## Diagnostics

`FUI > Red Dot > Debugger` is read-only. It caches snapshots by `IRedDotDiagnostics.DiagnosticsVersion`, refreshes Scope discovery at a lower frequency, and renders at most 200 matching nodes per page so a 10,000-node graph does not rebuild and draw every row continuously.

The Sample contains seven independent RTS pages and an Overlay tracking terminal, starting from `RedDotQuickStart.unity`. Its typed ViewModels demonstrate the FUI binding form `nameof(RedDotElement.Key)`. The interactive pages use a Sample-only Legacy Text adapter so they run without TMP Essentials; the separate `RedDotBadge.prefab` remains an Inspector reference for native TMP, `visualRoot`, `Auto` and a `99` limit. See [capability coverage](sample-coverage.md) and [Unity verification](sample-verification/README.md).

The framework intentionally has no read, acknowledgement, server-authority, persistence or reset semantics.
