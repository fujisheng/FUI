# FUI URUI

Install `com.fujisheng.fui` 2.0.0, `com.fujisheng.urui`, and this package.

Create a `Stage`, load authored HTML/CSS, and provide explicit `ElementBinding` mappings and typed adapter factories to each `View`. A view without mappings is display-only. Ordinary HTML structure can be authored independently of FUI binding names.

Supply application resource loaders and child-view factories to list, dynamic and slot adapters. Use author CSS for layout and skins, ordinary overflow elements for scrolling, and authored surfaces for composed progress controls. Game models, routes, sounds and visual effects remain outside this package.

Each top-level page has its own document and `ViewScope`. In URP, `FUI.Rendering.URUI.URP.RendererFeature` submits the shared surface. Choose the FUI backend before startup. Pages retain the same FUI model and presenter contracts. See the package README for composition, initial geometry and callback lifetime requirements.
