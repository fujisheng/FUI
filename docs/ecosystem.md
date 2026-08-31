# FUI ecosystem boundaries

FUI is a Monorepo for Core and tightly coupled first-party integrations.

## Packages accepted here

- resource backends implementing `IViewProvider` or asset lease contracts;
- transition integrations implementing `ITransitionProvider`;
- FUI-specific input gates, log sinks, Elements, converters, or validators;
- integrations whose useful behavior is defined primarily by the FUI lifecycle.

## Packages kept independent

- `fui-cli` and `unitycli`;
- general agent tools;
- Unity tools that can serve other UI frameworks without FUI;
- infrastructure with an independent release audience.

Core never depends on an extension. Extensions consume only public Core API. A shared contract moves into Core only when at least two real integrations need it and the contract is smaller than duplicated implementation.

Each package has its own version, changelog, tests, documentation, and OpenUPM submission.
