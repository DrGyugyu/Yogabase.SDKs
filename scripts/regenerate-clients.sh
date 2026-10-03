#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
if [[ -n "${DOTNET_EXE:-}" ]]; then
    dotnet_exe="$DOTNET_EXE"
elif [[ -x "${HOME}/.dotnet/dotnet" ]]; then
    dotnet_exe="${HOME}/.dotnet/dotnet"
else
    dotnet_exe="dotnet"
fi
tool_manifest="$repo_root/.config/dotnet-tools.json"
application_contract="$repo_root/contracts/yogabase-client-v1.openapi.json"
delivery_contract="$repo_root/contracts/yogabase-delivery-v1.openapi.json"

if ! command -v "$dotnet_exe" >/dev/null 2>&1; then
    echo "A .NET SDK is required. Set DOTNET_EXE to the dotnet executable." >&2
    exit 1
fi

(cd "$repo_root/contracts" && shasum -a 256 -c yogabase-client-v1.openapi.sha256)
(cd "$repo_root/contracts" && shasum -a 256 -c yogabase-builds-v1.openapi.sha256)
(cd "$repo_root/contracts" && shasum -a 256 -c yogabase-delivery-v1.openapi.sha256)
(cd "$repo_root/contracts" && shasum -a 256 -c yogabase-runtime-host-v1.openapi.sha256)
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
cp "$application_contract" "$stage/contracts/yogabase-client-v1.openapi.json"
cp "$delivery_contract" "$stage/contracts/yogabase-delivery-v1.openapi.json"
staged_contract="$stage/contracts/yogabase-client-v1.openapi.json"
staged_delivery_contract="$stage/contracts/yogabase-delivery-v1.openapi.json"

generate_application() {
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

generate_delivery() {
    language="$1"
    namespace="$2"
    output="$3"
    mkdir -p "$output"
    (cd "$repo_root" && "$dotnet_exe" kiota generate \
        --openapi "$staged_delivery_contract" \
        --language "$language" \
        --class-name YogabaseDeliveryApiClient \
        --namespace-name "$namespace" \
        --output "$output" \
        --exclude-backward-compatible \
        --additional-data \
        --clean-output \
        --log-level Warning)
}

generate_application TypeScript Yogabase.Client.TypeScript "$stage/languages/typescript/src"
generate_application Python stackyoga.yogabase "$stage/languages/python/src/stackyoga_yogabase"
generate_application Java io.stackyoga.yogabase "$stage/languages/java/src/main/java/io/stackyoga/yogabase"
generate_application Dart stackyoga_yogabase "$stage/languages/dart/lib/src"
generate_delivery Java io.stackyoga.yogabase.delivery "$stage/languages/java/src/main/java/io/stackyoga/yogabase/delivery"
generate_delivery Dart stackyoga_yogabase_delivery "$stage/languages/dart/lib/src/delivery"
find "$stage" -name .kiota.log -delete

rsync -a --delete \
    --exclude /index.ts \
    --exclude /delivery.ts \
    --exclude /jobs.ts \
    --exclude /storageLogicalKey.ts \
    "$stage/languages/typescript/src/" \
    "$repo_root/languages/typescript/src/"

rsync -a --delete \
    --exclude /__init__.py \
    --exclude /delivery.py \
    --exclude /jobs.py \
    --exclude /storage_logical_key.py \
    "$stage/languages/python/src/stackyoga_yogabase/" \
    "$repo_root/languages/python/src/stackyoga_yogabase/"

rsync -a --delete \
    --exclude /delivery/ \
    --exclude /YogabaseApplicationClient.java \
    --exclude /YogabaseClientOrigin.java \
    --exclude /YogabaseDeliveryBearerTokenProvider.java \
    --exclude /YogabaseDeliveryClient.java \
    --exclude /YogabaseDeliveryRequestOptions.java \
    --exclude /YogabaseDeliverySseEvent.java \
    --exclude /YogabaseDeliverySseParser.java \
    --exclude '/YogabaseJob*.java' \
    --exclude /YogabaseStorageLogicalKey.java \
    "$stage/languages/java/src/main/java/io/stackyoga/yogabase/" \
    "$repo_root/languages/java/src/main/java/io/stackyoga/yogabase/"

rsync -a --delete \
    "$stage/languages/java/src/main/java/io/stackyoga/yogabase/delivery/" \
    "$repo_root/languages/java/src/main/java/io/stackyoga/yogabase/delivery/"

rsync -a --delete \
    --exclude /delivery/ \
    --exclude /yogabase_application_client.dart \
    --exclude /yogabase_delivery_bearer_token_provider.dart \
    --exclude /yogabase_delivery_client.dart \
    --exclude /yogabase_delivery_request_options.dart \
    --exclude /yogabase_delivery_sse.dart \
    --exclude /yogabase_storage_logical_key.dart \
    "$stage/languages/dart/lib/src/" \
    "$repo_root/languages/dart/lib/src/"

rsync -a --delete \
    "$stage/languages/dart/lib/src/delivery/" \
    "$repo_root/languages/dart/lib/src/delivery/"

echo "Regenerated application clients for TypeScript, Python, Java, and Dart plus delivery protocol clients for Java and Dart with Kiota 1.34.1."
