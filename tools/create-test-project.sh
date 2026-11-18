#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package_id="${1:-}"

if [[ -z "$package_id" ]]; then
    echo "Usage: tools/create-test-project.sh <package-id>" >&2
    exit 2
fi

resolve_package_dir() {
    if [[ "$1" == "com.fujisheng.urui" ]]; then
        printf '%s\n' "${FUI_URUI_PACKAGE:-$repo_root/../URUI/Packages/com.fujisheng.urui}"
    elif [[ "$1" == "com.fujisheng.urui.effect" ]]; then
        printf '%s\n' "$repo_root/../URUI/Packages/com.fujisheng.urui.effect"
    else
        printf '%s\n' "$repo_root/Packages/$1"
    fi
}

package_dir="$(resolve_package_dir "$package_id")"
if [[ ! -f "$package_dir/package.json" ]]; then
    echo "Package not found: $package_id" >&2
    exit 1
fi

test_project="$repo_root/Library/FUITestProject"
if [[ -d "$test_project" ]]; then
    rm -rf "$test_project"
fi

mkdir -p "$test_project/Assets" "$test_project/Packages" "$test_project/ProjectSettings"

seen_file="$(mktemp)"
dependencies='{
  "com.unity.test-framework": "1.6.0",
  "com.unity.test-framework.performance": "3.5.0"
}'

visit_package() {
    local current_id="$1"
    local current_dir="$(resolve_package_dir "$current_id")"
    local dependency

    if grep -Fqx "$current_id" "$seen_file"; then
        return
    fi
    if [[ ! -f "$current_dir/package.json" ]]; then
        return
    fi

    printf '%s\n' "$current_id" >> "$seen_file"
    dependencies="$(jq \
        --arg id "$current_id" \
        --arg path "file:$current_dir" \
        '. + {($id): $path}' <<< "$dependencies")"

    while IFS= read -r dependency; do
        if [[ -n "$dependency" && -f "$(resolve_package_dir "$dependency")/package.json" ]]; then
            visit_package "$dependency"
        fi
    done < <(jq -r '.dependencies // {} | keys[]' "$current_dir/package.json")
}

visit_package "$package_id"

# URUI lives in a sibling repository, not under FUI/Packages; inject it for the
# FUI.URUI test project. CI must publish URUI or set FUI_URUI_PACKAGE to a checkout.
if [[ "$package_id" == "com.fujisheng.fui.urui" || "$package_id" == "com.fujisheng.urui.effect" ]]; then
    urui_package="${FUI_URUI_PACKAGE:-$repo_root/../URUI/Packages/com.fujisheng.urui}"
    if [[ ! -f "$urui_package/package.json" ]]; then
        echo "URUI package not found for FUI.URUI tests: $urui_package" >&2
        rm -f "$seen_file"
        exit 1
    fi
    dependencies="$(jq --arg path "file:$urui_package" '. + {"com.fujisheng.urui": $path, "com.unity.render-pipelines.universal": "17.3.0"}' <<< "$dependencies")"
fi

rm -f "$seen_file"

jq -n \
    --argjson dependencies "$dependencies" \
    --arg package_id "$package_id" \
    '{dependencies: $dependencies, testables: ([$package_id] + (if $dependencies["com.fujisheng.urui"] then ["com.fujisheng.urui"] else [] end) | unique)}' \
    > "$test_project/Packages/manifest.json"

printf '%s\n' \
    'm_EditorVersion: 6000.3.22f1' \
    'm_EditorVersionWithRevision: 6000.3.22f1 (f85733094e60)' \
    > "$test_project/ProjectSettings/ProjectVersion.txt"

while IFS=$'\t' read -r sample_name sample_path; do
    if [[ -z "$sample_path" || ! -d "$package_dir/$sample_path" ]]; then
        continue
    fi
    sample_target="$test_project/Assets/Samples/$(jq -r '.displayName' "$package_dir/package.json")/$(jq -r '.version' "$package_dir/package.json")/$sample_name"
    mkdir -p "$(dirname "$sample_target")"
    cp -R "$package_dir/$sample_path" "$sample_target"
done < <(jq -r '.samples[]? | [.displayName, .path] | @tsv' "$package_dir/package.json")

echo "$test_project"
