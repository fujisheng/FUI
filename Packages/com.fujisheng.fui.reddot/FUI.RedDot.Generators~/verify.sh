#!/usr/bin/env bash
set -euo pipefail

generator_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package_root="$(cd "$generator_root/.." && pwd)"

dotnet test "$generator_root/Tests/FUI.RedDot.Generators.Tests.csproj" --configuration Release
dotnet build "$generator_root/FUI.RedDot.Generators.csproj" --configuration Release

source_dll="$generator_root/bin/Release/netstandard2.0/FUI.RedDot.Generators.dll"
package_dll="$package_root/Roslyn~/FUI.RedDot.Generators.dll"
cp "$source_dll" "$package_dll"
cp "$source_dll" "$package_root/Samples~/RedDot Sample/FUI.RedDot.Generators.dll"
cp "$package_root/../com.fujisheng.fui/Roslyn~/FUI.Generators.dll" "$package_root/Samples~/RedDot Sample/FUI.Generators.dll"
cmp "$source_dll" "$package_dll"
cmp "$source_dll" "$package_root/Samples~/RedDot Sample/FUI.RedDot.Generators.dll"
cmp "$package_root/../com.fujisheng.fui/Roslyn~/FUI.Generators.dll" "$package_root/Samples~/RedDot Sample/FUI.Generators.dll"

echo "PASS FUI.RedDot.Generators"
