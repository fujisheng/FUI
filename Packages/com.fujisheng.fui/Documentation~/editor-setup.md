# Editor setup

> `FUI/Setup`, Source Generator installation, and Core editor setup ship in Core. The Binding Inspector and uGUI authoring gates require the separately installed [`com.fujisheng.fui.ugui`](../../com.fujisheng.fui.ugui/README.md) package.

## Setup

`FUI/Setup` installs the Source Generator into a target assembly and optionally installs Cecil for Mixed mode. It also exposes optional ecosystem packages when they are available from OpenUPM.

## Binding Inspector

Binding Inspector shows generated View-to-ViewModel connections in Edit Mode, selects the target object, and navigates to source when information is available.

Optional packages are listed in the same window but remain separate UPM dependencies. See [Page Wizard and validation](validation.md) for authoring gates.
