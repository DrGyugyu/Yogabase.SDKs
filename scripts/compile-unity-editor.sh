#!/usr/bin/env bash
# Compile-checks the Unity package's Editor scripts in a real Unity Editor compile environment.
#
# Unity itself is not scriptable from this repository, and a hand-rolled reference set would not
# prove much: Editor code compiles against a specific corlib profile plus a specific set of modular
# UnityEngine/UnityEditor assemblies, and getting either wrong turns a real error into a false pass
# or a false failure. So this script reuses a response file Unity's own Bee build already wrote for
# an Editor assembly, keeps only its Editor-installation references, defines and switches, and
# swaps in this package's sources.
#
# Usage:
#   UNITY_RSP=/path/to/Library/Bee/artifacts/<dag>/Assembly-CSharp-Editor.rsp scripts/compile-unity-editor.sh
#
# With no UNITY_RSP, the script looks for one under ~/Desktop/UnityProjects/*/Library/Bee. If it
# finds none it exits 2 and says so, rather than reporting a pass it did not perform.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
package="$repo_root/languages/unity/io.yogabase.runtime-host"
out="$repo_root/.unity-compile"

if [[ -z "${UNITY_RSP:-}" ]]; then
    UNITY_RSP=$(find "$HOME/Desktop/UnityProjects" \
        -path '*/Library/Bee/artifacts/*' -name 'Assembly-CSharp-Editor.rsp' 2>/dev/null \
        | sort | tail -1)
fi
if [[ -z "${UNITY_RSP:-}" || ! -f "$UNITY_RSP" ]]; then
    echo "No Unity Editor response file found, so the Editor scripts were NOT compile-checked." >&2
    echo "Set UNITY_RSP to an Assembly-CSharp-Editor.rsp from a Unity project that has compiled" >&2
    echo "at least once with the Editor version you target." >&2
    exit 2
fi

if [[ -n "${DOTNET_EXE:-}" ]]; then
    dotnet_exe="$DOTNET_EXE"
elif [[ -x "$HOME/.dotnet/dotnet" ]]; then
    dotnet_exe="$HOME/.dotnet/dotnet"
else
    dotnet_exe="dotnet"
fi
sdk=$(ls -d "$HOME"/.dotnet/sdk/*/ 2>/dev/null | sort -V | tail -1)
csc="${sdk}Roslyn/bincore/csc.dll"
if [[ ! -f "$csc" ]]; then
    echo "No Roslyn csc.dll under ${sdk:-<no .NET SDK>}; cannot compile-check." >&2
    exit 2
fi

rm -rf "$out"
mkdir -p "$out"

# Only references inside the Unity installation. The source project's own assemblies and its
# Assets plugins are irrelevant here and would not exist in a consumer's project.
grep -E '^-r:"/Applications/Unity/' "$UNITY_RSP" > "$out/args.rsp"
grep -E '^-define:' "$UNITY_RSP" >> "$out/args.rsp"
grep -E '^-(langversion|nostdlib|nullable|unsafe|deterministic|preferreduilang):' "$UNITY_RSP" \
    >> "$out/args.rsp" || true
{
    echo '-target:library'
    echo '-warnaserror+'
    echo "-out:\"$out/Yogabase.Yogabase.RuntimeHost.Editor.dll\""
} >> "$out/args.rsp"
find "$package/Editor" -name '*.cs' | sort | sed 's/^/"/; s/$/"/' >> "$out/args.rsp"

sources=$(find "$package/Editor" -name '*.cs' | wc -l | tr -d ' ')
references=$(grep -c '^-r:' "$out/args.rsp")
language=$(grep -E '^-langversion:' "$out/args.rsp" | head -1)
echo "Compiling $sources Editor sources against $references Unity reference assemblies ($language)."
echo "Response file: $UNITY_RSP"
"$dotnet_exe" "$csc" -noconfig "@$out/args.rsp"
echo "Editor scripts compiled clean."
