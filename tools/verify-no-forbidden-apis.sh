#!/usr/bin/env bash
# Anti-cheat compliance gate for iRadar.
#
# Fails the build if any source file references Win32 APIs commonly used for
# process injection, memory tampering, or kernel-level hooking. These APIs are
# the line between "external overlay" (allowed by iRacing) and "cheat" (ban).
#
# Allowed APIs: shared memory (CreateFileMapping/MapViewOfFile), window styles,
# RegisterHotKey, BroadcastSystemMessage. Anything that touches the iRacing
# process directly is forbidden.
#
# Exit codes:
#   0 - clean
#   1 - forbidden API detected
#   2 - usage error
set -euo pipefail

ROOT="${1:-src}"

if [ ! -d "$ROOT" ]; then
  echo "error: directory not found: $ROOT" >&2
  echo "usage: $0 [source-root]" >&2
  exit 2
fi

# Patterns: P/Invoke imports + literal API names.
# Add new ones here as the threat surface evolves.
FORBIDDEN=(
  "ReadProcessMemory"
  "WriteProcessMemory"
  "VirtualAllocEx"
  "VirtualProtectEx"
  "CreateRemoteThread"
  "NtCreateThreadEx"
  "RtlCreateUserThread"
  "QueueUserAPC"
  "SetWindowsHookEx"
  "SetWindowsHookExA"
  "SetWindowsHookExW"
  "LoadLibraryA"
  "LoadLibraryW"
  "LdrLoadDll"
  "OpenProcess"
  "DuplicateHandle"
  "WriteFile.*iRacingSim"
  "ZwUnmapViewOfSection"
  "MiniDumpWriteDump"
  "EasyHook"
  "Detours"
  "MinHook"
)

# Files to skip: this script itself and any allow-listed places.
EXCLUDE_PATHS=(
  -path "*/bin" -prune
  -o -path "*/obj" -prune
  -o -path "*/.git" -prune
  -o -name "verify-no-forbidden-apis.sh" -prune
)

found=0
echo "Scanning $ROOT for forbidden APIs..."

# Build single regex
pattern="$(IFS='|'; echo "${FORBIDDEN[*]}")"

while IFS= read -r -d '' file; do
  if grep -nE "$pattern" "$file" > /dev/null 2>&1; then
    echo ""
    echo "FORBIDDEN API DETECTED in: $file"
    grep -nE "$pattern" "$file" || true
    found=1
  fi
done < <(find "$ROOT" \( "${EXCLUDE_PATHS[@]}" \) -o \( -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.xml" -o -name "*.json" \) -print0 \))

if [ "$found" -ne 0 ]; then
  echo ""
  echo "FAIL: anti-cheat compliance gate detected forbidden Win32 APIs."
  echo "These APIs are associated with process injection, memory tampering,"
  echo "or hooking — all of which violate iRacing's anti-cheat policy."
  echo "If you have a legitimate need, add an explicit allow-list entry"
  echo "in tools/verify-no-forbidden-apis.sh and justify in the commit message."
  exit 1
fi

echo "OK: no forbidden APIs found."
exit 0
