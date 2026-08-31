# Optional packages

FUI Core remains functional without any optional package.

`FUI/Setup` lists the external tools below. The install action stays disabled until the fixed compatible version is published on OpenUPM:

| Package | Repository | Role |
|---|---|---|
| `com.fujisheng.unitycli` | `fujisheng/unitycli` | Unity automation bridge |
| `com.fujisheng.fui.cli` | `fujisheng/fui-cli` | AI-oriented FUI automation |

The tools are maintained in independent repositories. After publication, installing FUI CLI resolves its UnityCli dependency and the availability flag is enabled in a Core patch release.

Future first-party packages that directly implement FUI providers, transitions, Elements, or validators may live in the FUI Monorepo and keep independent SemVer versions.
