#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mode="${1:-}"

if [[ -n "$mode" && "$mode" != "--structure-only" ]]; then
    echo "Usage: tools/verify-repository.sh [--structure-only]" >&2
    exit 2
fi

if [[ -d "$repo_root/Assets" || -d "$repo_root/ProjectSettings" ]]; then
    echo "Repository root must not be a Unity project." >&2
    exit 1
fi

forbidden="$(
    git -C "$repo_root" ls-files --cached --others --exclude-standard |
        rg '(^|/)(\.DS_Store|Library|Temp|obj|bin)(/|$)' || true
)"
if [[ -n "$forbidden" ]]; then
    echo "Forbidden generated artifacts are present:" >&2
    echo "$forbidden" >&2
    exit 1
fi

package_files=()
while IFS= read -r package_file; do
    package_files+=("$package_file")
done < <(find "$repo_root/Packages" -mindepth 2 -maxdepth 2 -name package.json -type f | sort)

if [[ "${#package_files[@]}" -eq 0 ]]; then
    echo "No UPM packages found." >&2
    exit 1
fi

duplicate_ids="$(for package_file in "${package_files[@]}"; do jq -r '.name' "$package_file"; done | sort | uniq -d)"
if [[ -n "$duplicate_ids" ]]; then
    echo "Duplicate package IDs: $duplicate_ids" >&2
    exit 1
fi

duplicate_assemblies="$({
    find "$repo_root/Packages" -name '*.asmdef' -type f -print0 |
        xargs -0 -n1 jq -r '.name'
} | sort | uniq -d)"
if [[ -n "$duplicate_assemblies" ]]; then
    echo "Duplicate assembly names: $duplicate_assemblies" >&2
    exit 1
fi

if rg -n 'InternalsVisibleTo\("FUI\.Extensions\.' "$repo_root/Packages/com.fujisheng.fui/Runtime"; then
    echo "Core internal APIs must not be exposed to FUI extensions." >&2
    exit 1
fi

python3 - "$repo_root" <<'PY'
import json
import pathlib
import re
import sys
import urllib.parse

root = pathlib.Path(sys.argv[1])
packages = {}
for package_json in sorted((root / "Packages").glob("*/package.json")):
    data = json.loads(package_json.read_text(encoding="utf-8"))
    packages[data["name"]] = data.get("dependencies", {})

visiting = set()
visited = set()

def visit(package_id):
    if package_id in visiting:
        raise SystemExit(f"Package dependency cycle includes {package_id}")
    if package_id in visited:
        return
    visiting.add(package_id)
    for dependency in packages.get(package_id, {}):
        if dependency in packages:
            visit(dependency)
    visiting.remove(package_id)
    visited.add(package_id)

for package_id in packages:
    visit(package_id)

link_pattern = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
errors = []
for markdown in sorted(root.rglob("*.md")):
    if ".git" in markdown.parts or "Library" in markdown.parts:
        continue
    text = markdown.read_text(encoding="utf-8")
    for target in link_pattern.findall(text):
        clean = urllib.parse.unquote(target.split("#", 1)[0])
        if not clean or clean.startswith(("http://", "https://", "mailto:")):
            continue
        resolved = (markdown.parent / clean).resolve()
        if not resolved.exists():
            errors.append(f"{markdown.relative_to(root)} -> {target}")
if errors:
    raise SystemExit("Broken Markdown links:\n" + "\n".join(errors))
PY

for package_file in "${package_files[@]}"; do
    package_id="$(jq -r '.name' "$package_file")"
    if [[ "$mode" == "--structure-only" ]]; then
        "$repo_root/tools/verify-package.sh" "$package_id" --structure-only
    else
        "$repo_root/tools/verify-package.sh" "$package_id"
    fi
done

echo "PASS repository (${#package_files[@]} package(s))"
