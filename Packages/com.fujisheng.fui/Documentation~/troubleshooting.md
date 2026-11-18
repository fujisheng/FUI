# Troubleshooting

## Generated Routes are missing

Confirm the ViewModel assembly has `[ViewModule]`, the ViewModel is `partial`, and the Generator DLL was installed beside the target `.asmdef` with `RoslynAnalyzer` and `SourceGenerator` labels.

## Duplicate generated types

Remove legacy or duplicate generator DLLs from the target assembly folder, then run **Install Generator** again.

## Mixed properties do not update

Confirm the module selects Mixed mode, `com.unity.nuget.mono-cecil` is installed, and `FUI.Editor.ILPostProcessing` is enabled by its version define.

For ordinary `-batchmode -quit` compilation, the first compilation can finish before the editor coordinator subscribes, and Unity can quit before its delayed check runs. FUI also checks the configured target on the official `EditorApplication.quitting` event, without requesting a script reload while exiting. This prepares the DLL for the **next** loaded domain; it cannot update a ViewModel already loaded by a first cold `-executeMethod` action.

Use a compile-and-exit process followed by a separate test/action process for that case. Do not add `-quit` to `-runTests`; allow the Test Runner to finish, and require a fresh XML result with a positive test count and no failures. Zero compiler errors or a valid PDB alone do not prove Mixed notification weaving worked.

The [batch quit regression fixture](../../../tools/fixtures/README.md) and `tools/verify-batch-quit.py` verify actual Mixed and Pure notifications and equal-value deduplication across two starts. A nonzero exit, compiler errors, either known ILPP failure prefix, or a missing success marker fails verification. The script restores its dedicated source fixture, so it is a regression check, not a cache preparation command. It requires an idle isolated fixture project and does not install packages or repair binaries manually.

## A View fails validation

Match every `[Bind]` and `[Command]` element name to exactly one Element in the prefab and verify the target member type. Check nested container boundaries for duplicate ownership.

## Optional package installation fails

Confirm the OpenUPM registry is configured for `com.fujisheng`, the requested package version exists, and no private scoped registry shadows the same scope. The Setup window shows the original Package Manager error.

## Unity compilation result is unclear

Wait until Unity finishes compiling, then inspect fresh Console errors and `Editor.log`. Generator-only `dotnet build` does not replace Unity compilation validation.
