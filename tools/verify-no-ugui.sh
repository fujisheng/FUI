#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
core="$repo_root/Packages/com.fujisheng.fui"
manifest="$core/package.json"
scope="${1:-all}"
case "$scope" in
  runtime) paths=("$core/Runtime") ;;
  editor)  paths=("$core/Editor") ;;
  tests)   paths=("$core/Tests") ;;
  all)     paths=("$core/Runtime" "$core/Editor" "$core/Tests") ;;
  *) echo "Usage: verify-no-ugui.sh [runtime|editor|tests|all]" >&2; exit 2 ;;
esac

if ! command -v jq >/dev/null 2>&1; then
  echo "verify-no-ugui.sh: jq is required on PATH to inspect the package manifest." >&2
  exit 2
fi

if [[ ! -f "$manifest" ]]; then
  echo "verify-no-ugui.sh: missing package manifest: $manifest" >&2
  exit 2
fi

if ! jq -e . "$manifest" >/dev/null 2>&1; then
  echo "verify-no-ugui.sh: package manifest is not valid JSON: $manifest" >&2
  exit 2
fi

if ! jq -e '.dependencies == null or (.dependencies | type) == "object"' "$manifest" >/dev/null 2>&1; then
  echo "verify-no-ugui.sh: package manifest has an invalid dependencies section: $manifest" >&2
  exit 2
fi

for path in "${paths[@]}"; do
  if [[ ! -d "$path" ]]; then
    echo "verify-no-ugui.sh: missing scope directory: $path" >&2
    exit 2
  fi
done

status=0

if jq -e '.dependencies // {} | has("com.unity.ugui")' "$manifest" >/dev/null; then
  echo "Core package must not depend on com.unity.ugui." >&2
  status=1
fi

ref_pattern='FUI\.Rendering\.UGUI'
ref_pattern+='|(^|[^[:alnum:]_])UnityEngine\.UI([^[:alnum:]_]|$)'
ref_pattern+='|(^|[^[:alnum:]_])UnityEditor\.UI([^[:alnum:]_]|$)'
ref_pattern+='|TMPro|Unity\.TextMeshPro|Unity\.ugui'
ref_pattern+='|b8dfc198079bdaf4d9ff09de6b519c0f'
ref_pattern+='|2bafac87e7f4b9b418d9448d219b01ab'
ref_pattern+='|6055be8ebefd69e48b49212b09b47b2f'

grep_status=0
if matches="$(grep -REn "$ref_pattern" "${paths[@]}")"; then
  grep_status=0
else
  grep_status=$?
fi
case "$grep_status" in
  0) ;;
  1) matches="" ;;
  *)
    echo "verify-no-ugui.sh: grep failed while scanning scope '$scope' (exit $grep_status)." >&2
    exit 2
    ;;
esac

if [[ -n "$matches" ]]; then
  ivt_allow='^.*:[0-9]+:[[:space:]]*\[assembly:[[:space:]]*InternalsVisibleTo\("(FUI\.Rendering\.UGUI|FUI\.Rendering\.UGUI\.Editor|FUI\.Rendering\.UGUI\.Tests\.Editor)"\)\][[:space:]]*$'
  if ! matches="$(printf '%s\n' "$matches" | sed -E "/${ivt_allow}/d")"; then
    echo "verify-no-ugui.sh: failed to apply the InternalsVisibleTo allowlist." >&2
    exit 2
  fi
fi

if [[ -n "$matches" ]]; then
  printf '%s\n' "$matches" >&2
  echo "Core package references UGUI/TMP in scope: $scope" >&2
  status=1
fi

exit "$status"
