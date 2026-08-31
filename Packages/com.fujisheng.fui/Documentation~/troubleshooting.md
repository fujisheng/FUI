# Troubleshooting

## Generated Routes are missing

Confirm the ViewModel assembly has `[ViewModule]`, the ViewModel is `partial`, and the Generator DLL was installed beside the target `.asmdef` with `RoslynAnalyzer` and `SourceGenerator` labels.

## Duplicate generated types

Remove legacy or duplicate generator DLLs from the target assembly folder, then run **Install Generator** again.

## Mixed properties do not update

Confirm the module selects Mixed mode, `com.unity.nuget.mono-cecil` is installed, and `FUI.Editor.ILPostProcessing` is enabled by its version define.

## A View fails validation

Match every `[Bind]` and `[Command]` element name to exactly one Element in the prefab and verify the target member type. Check nested container boundaries for duplicate ownership.

## Optional package installation fails

Confirm the OpenUPM registry is configured for `com.fujisheng`, the requested package version exists, and no private scoped registry shadows the same scope. The Setup window shows the original Package Manager error.

## Unity compilation result is unclear

Wait until Unity finishes compiling, then inspect fresh Console errors and `Editor.log`. Generator-only `dotnet build` does not replace Unity compilation validation.
