#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
dotnet_exe="${DOTNET_EXE:-dotnet}"
tool_manifest="$repo_root/.config/dotnet-tools.json"
contract="$repo_root/contracts/yogabase-client-v1.openapi.json"

if ! command -v "$dotnet_exe" >/dev/null 2>&1; then
    user_dotnet="${HOME}/.dotnet/dotnet"
    if [[ -x "$user_dotnet" ]]; then
        dotnet_exe="$user_dotnet"
    else
        echo "A .NET SDK is required. Set DOTNET_EXE to the dotnet executable." >&2
        exit 1
    fi
fi

(cd "$repo_root/contracts" && shasum -a 256 -c yogabase-client-v1.openapi.sha256)
(cd "$repo_root" && "$dotnet_exe" tool restore --tool-manifest "$tool_manifest")

stage="$(mktemp -d "$repo_root/.regeneration.XXXXXX")"
cleanup() {
    case "$stage" in
        "$repo_root"/.regeneration.*) rm -rf -- "$stage" ;;
        *) echo "Refusing to remove unexpected regeneration path: $stage" >&2 ;;
    esac
}
trap cleanup EXIT

mkdir -p "$stage/contracts"
cp "$contract" "$stage/contracts/yogabase-client-v1.openapi.json"
staged_contract="$stage/contracts/yogabase-client-v1.openapi.json"

generate() {
    language="$1"
    namespace="$2"
    output="$3"
    mkdir -p "$output"
    (cd "$repo_root" && "$dotnet_exe" kiota generate \
        --openapi "$staged_contract" \
        --language "$language" \
        --class-name YogabaseApiClient \
        --namespace-name "$namespace" \
        --output "$output" \
        --exclude-backward-compatible \
        --additional-data \
        --clean-output \
        --log-level Warning)
}

generate TypeScript Yogabase.Client.TypeScript "$stage/languages/typescript/src"
generate Python stackyoga.yogabase "$stage/languages/python/src/stackyoga_yogabase"
generate Java io.stackyoga.yogabase "$stage/languages/java/src/main/java/io/stackyoga/yogabase"
generate Dart stackyoga_yogabase "$stage/languages/dart/lib/src"
find "$stage" -name .kiota.log -delete

rsync -a --delete \
    --exclude index.ts \
    --exclude storageLogicalKey.ts \
    "$stage/languages/typescript/src/" \
    "$repo_root/languages/typescript/src/"

rsync -a --delete \
    --exclude __init__.py \
    --exclude storage_logical_key.py \
    "$stage/languages/python/src/stackyoga_yogabase/" \
    "$repo_root/languages/python/src/stackyoga_yogabase/"

rsync -a --delete \
    --exclude YogabaseStorageLogicalKey.java \
    "$stage/languages/java/src/main/java/io/stackyoga/yogabase/" \
    "$repo_root/languages/java/src/main/java/io/stackyoga/yogabase/"

rsync -a --delete \
    --exclude yogabase_storage_logical_key.dart \
    "$stage/languages/dart/lib/src/" \
    "$repo_root/languages/dart/lib/src/"

echo "Regenerated TypeScript, Python, Java, and Dart clients with Kiota 1.34.1."
