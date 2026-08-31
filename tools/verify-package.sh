#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package_id="${1:-}"
mode="${2:-}"

if [[ -z "$package_id" ]]; then
    echo "Usage: tools/verify-package.sh <package-id> [--structure-only]" >&2
    exit 2
fi
if [[ -n "$mode" && "$mode" != "--structure-only" ]]; then
    echo "Unknown option: $mode" >&2
    exit 2
fi

mapfile_compat() {
    local destination="$1"
    shift
    local line
    eval "$destination=()"
    while IFS= read -r line; do
        eval "$destination+=(\"\$line\")"
    done < <("$@")
}

mapfile_compat package_files find "$repo_root/Packages" -mindepth 2 -maxdepth 2 -name package.json -type f
matches=()
for package_file in "${package_files[@]}"; do
    if [[ "$(jq -r '.name' "$package_file")" == "$package_id" ]]; then
        matches+=("$package_file")
    fi
done

if [[ "${#matches[@]}" -ne 1 ]]; then
    echo "Expected exactly one package.json for $package_id, found ${#matches[@]}." >&2
    exit 1
fi

package_file="${matches[0]}"
package_dir="$(dirname "$package_file")"
directory_name="$(basename "$package_dir")"
version="$(jq -er '.version' "$package_file")"

if [[ "$directory_name" != "$package_id" ]]; then
    echo "Package directory '$directory_name' does not match '$package_id'." >&2
    exit 1
fi
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]]; then
    echo "Invalid SemVer version: $version" >&2
    exit 1
fi

required_paths=(
    README.md
    CHANGELOG.md
    LICENSE.md
    Documentation~/index.md
    Runtime
    Tests
)
for required_path in "${required_paths[@]}"; do
    if [[ ! -e "$package_dir/$required_path" ]]; then
        echo "Missing required package path: $package_id/$required_path" >&2
        exit 1
    fi
done

if ! rg -q "\[$version\]|## +$version|## +\[$version\]" "$package_dir/CHANGELOG.md"; then
    echo "CHANGELOG does not contain package version $version." >&2
    exit 1
fi

if [[ "$package_id" == "com.fujisheng.fui" ]]; then
    required_core_paths=(FUI.Generators~ Roslyn~/FUI.Generators.dll Samples~/"Settings Sample")
    for required_path in "${required_core_paths[@]}"; do
        if [[ ! -e "$package_dir/$required_path" ]]; then
            echo "Missing Core path: $required_path" >&2
            exit 1
        fi
    done
    if jq -e '.dependencies // {} | keys[] | select(startswith("com.fujisheng.fui."))' "$package_file" >/dev/null; then
        echo "Core must not depend on an FUI extension." >&2
        exit 1
    fi
    cmp "$package_dir/Roslyn~/FUI.Generators.dll" \
        "$package_dir/Samples~/Settings Sample/FUI.Generators.dll"
fi

if [[ "$mode" == "--structure-only" ]]; then
    echo "PASS $package_id $version (structure)"
    exit 0
fi

if [[ "$package_id" == "com.fujisheng.fui" ]]; then
    "$package_dir/FUI.Generators~/verify.sh"
fi

unity_path="${FUI_UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$unity_path" ]]; then
    echo "Unity executable not found: $unity_path" >&2
    exit 1
fi

test_project="$("$repo_root/tools/create-test-project.sh" "$package_id")"
report_dir="$repo_root/Library/FUIReports"
mkdir -p "$report_dir"

run_tests() {
    local platform="$1"
    local result_file="$report_dir/$package_id-$platform.xml"
    local log_file="$report_dir/$package_id-$platform.log"

    "$unity_path" \
        -batchmode \
        -nographics \
        -projectPath "$test_project" \
        -runTests \
        -testPlatform "$platform" \
        -testResults "$result_file" \
        -logFile "$log_file"

    if [[ ! -f "$result_file" ]]; then
        echo "Unity did not create $platform test results." >&2
        exit 1
    fi
    if rg -n "error CS[0-9]+|Compilation failed|Scripts have compiler errors|Unhandled Exception" "$log_file"; then
        echo "Unity reported compiler or unhandled errors during $platform tests." >&2
        exit 1
    fi
}

run_tests EditMode
run_tests PlayMode

report_file="$report_dir/$package_id.md"
{
    echo "# Package verification: $package_id"
    echo
    echo "- Version: $version"
    echo "- Unity: $unity_path"
    echo "- EditMode: passed"
    echo "- PlayMode: passed"
    if [[ "$package_id" == "com.fujisheng.fui" ]]; then
        echo "- Generator build, tests and packaged DLL hash: passed"
    fi
} > "$report_file"

echo "PASS $package_id $version"
echo "Report: $report_file"
