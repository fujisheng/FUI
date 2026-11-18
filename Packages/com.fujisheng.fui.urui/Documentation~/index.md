# FUI URUI

Install `com.fujisheng.fui` 2.0.0, `com.fujisheng.urui`, and this package.

Create a `UruiStage`, load authored HTML/CSS, and provide explicit `UruiElementBinding` mappings and typed adapter factories to each `UruiView`. A view without mappings is display-only. Ordinary HTML structure is independent of FUI binding names; no private binding attributes or framework-specific tags are needed.

Supply application resource loaders and child-view factories to list, dynamic and slot adapters. Use author CSS for layout and skins, ordinary overflow elements for scrolling, and authored surfaces for composed progress controls. Game models, routes, sounds and visual effects remain outside this package.

Only one URUI document may be active. In URP, execute `Document.RenderCommandBuffer` from the project's renderer feature. Choose the FUI backend before startup; pages retain the same FUI model and presenter contracts. See the package README for composition, initial geometry and callback lifetime requirements.
