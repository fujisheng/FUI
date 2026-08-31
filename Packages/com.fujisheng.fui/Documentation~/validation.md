# Page Wizard and validation

`FUI/Page Wizard` creates a View prefab using FullScreen, Popup, or Tips policy presets. The generated structure is a starting point; the View contract remains the source of binding requirements.

`FUI/Validate Views` checks missing Elements, incompatible types, duplicate names, nested boundaries, and page structure. The same validation runs incrementally after prefab saves and before builds.

Treat validation errors as release blockers. Do not suppress a diagnostic by falling back to a string-based runtime lookup.

Before a release, validate all prefabs, enter Play Mode for the sample, and verify that generated binding diagnostics report no issue.
