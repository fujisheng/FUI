#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package_id="${1:-}"

if [[ -z "$package_id" ]]; then
    echo "Usage: tools/create-test-project.sh <package-id>" >&2
    exit 2
fi

package_dir="$repo_root/Packages/$package_id"
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
    local current_dir="$repo_root/Packages/$current_id"
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
        if [[ -n "$dependency" && -f "$repo_root/Packages/$dependency/package.json" ]]; then
            visit_package "$dependency"
        fi
    done < <(jq -r '.dependencies // {} | keys[]' "$current_dir/package.json")
}

visit_package "$package_id"
rm -f "$seen_file"

jq -n \
    --argjson dependencies "$dependencies" \
    --arg package_id "$package_id" \
    '{dependencies: $dependencies, testables: [$package_id]}' \
    > "$test_project/Packages/manifest.json"

printf '%s\n' \
    'm_EditorVersion: 6000.3.22f1' \
    'm_EditorVersionWithRevision: 6000.3.22f1 (f85733094e60)' \
    > "$test_project/ProjectSettings/ProjectVersion.txt"

sample_dir="$package_dir/Samples~/Settings Sample"
if [[ -d "$sample_dir" ]]; then
    sample_target="$test_project/Assets/Samples/FUI/$(jq -r '.version' "$package_dir/package.json")/Settings Sample"
    mkdir -p "$(dirname "$sample_target")"
    cp -R "$sample_dir" "$sample_target"
fi

echo "$test_project"
