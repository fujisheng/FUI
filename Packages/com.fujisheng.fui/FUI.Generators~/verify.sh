#!/usr/bin/env bash
set -euo pipefail

generator_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$generator_dir"

dotnet restore FUI.Generators.sln --locked-mode
dotnet build FUI.Generators.sln -c Release --no-restore
dotnet test Tests/FUI.Generators.Tests.csproj -c Release --no-build --no-restore

generated_dll="bin/Release/netstandard2.0/FUI.Generators.dll"
packaged_dll="../Roslyn~/FUI.Generators.dll"
if [[ "${FUI_VERIFY_PACKAGED_DLL:-1}" == "1" ]]; then
    cmp "$generated_dll" "$packaged_dll"
fi

project_dll="../../../Assets/Scripts/UI/FUI.Generators.dll"
if [[ "${FUI_VERIFY_PACKAGED_DLL:-1}" == "1" && -f "$project_dll" ]]; then
    cmp "$generated_dll" "$project_dll"
fi
