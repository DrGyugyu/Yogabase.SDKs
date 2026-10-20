# Toolchains in a Linux cloud environment

Status: recorded 2026-10-20, on a Linux x86-64 container (Ubuntu 24.04) behind an HTTPS proxy
that re-terminates TLS. This repository's own record of how its five language suites are built
and run there. Nothing listed here is vendored into the repository: every tool is installed
outside the checkout, under `/opt/yogabase-sdk-toolchains/<tool>-<version>` or the user's home.

Every download is treated as untrusted data: the version is pinned exactly, the publisher's own
SHA-256 is checked before the archive is unpacked, and the archive is unpacked into its own new
directory. A tool the container image already carries is recorded with the version it reports and
the package it came from, not reinstalled.

## What is installed, and from where

| Tool | Version | Source | Integrity | Used for |
|---|---|---|---|---|
| .NET SDK | 10.0.402 (image), 10.0.400 when `--locked-mode` needs the exact pin | the image's `/usr/local/dotnet` | the image's | the .NET solution, `dotnet tool restore`, Kiota |
| Microsoft Kiota | 1.34.1 (`9f9cfb3b1cb9b5311a214ea6ce0f69943c523005`) | NuGet, through `dotnet tool restore` (`.config/dotnet-tools.json`) | the restored `microsoft.openapi.kiota.1.34.1.nupkg` hashes to `4aa14f12d573d5644eb167b837c4fa10d7d7b7ec4a6634b8bc4026269f9a7671`, the digest `MANIFEST.json` binds | `scripts/regenerate-clients.sh` |
| rsync | 3.2.7 (`3.2.7-1ubuntu1.5`) | `archive.ubuntu.com`, `apt-get install rsync` | the archive's own signed index; the package's SHA-256 is `8f952895697d19a6f1caa71f17c7d4e8c1f1fb485eb824ffe3e4c77dd587b338` | `scripts/regenerate-clients.sh` (the image has none) |
| Node.js | 25.9.0 (npm 11.12.1) | `https://nodejs.org/dist/v25.9.0/node-v25.9.0-linux-x64.tar.xz` | `1d8db7d6e291d167e8c467ae4094be175e1a0b3969c7ae1f8955b9f7824f7b2e`, the line for that file in the release's `SHASUMS256.txt` (fetched from the same release directory; its own SHA-256 was `56b97b0f08b2381a25708ca1f2768c1060ab28c2364ec1eccd5189225974fef1`) | `languages/typescript` (its `engines` names Node 25.9 or later; the image's Node 22 is too old) |
| Dart SDK | 3.13.5 stable (revision `04bcd1036cdc799ac6564988f159ee454d42c822`, built 2026-09-29) | `https://storage.googleapis.com/dart-archive/channels/stable/release/3.13.5/sdk/dartsdk-linux-x64-release.zip` | `ea864bc64df30a6b8bdf30b2e32550f7717d9a890de8f40293aeabb924fe232b`, equal to the archive's published `.sha256sum` | `languages/dart` (its lock is pinned to SDK 3.9 or later; 3.13.5 is the version its earlier runs used) |
| Python | 3.13.16 (`python3.13` `3.13.16-1+noble1`) and 3.12.3 (`python3.12` `3.12.3-1ubuntu0.17`, Ubuntu 24.04's own) | the image (3.12.3 is Ubuntu's archive package, SHA-256 `6745c9463432e619d7402b117ad4ac86c31dcd3999ba20daa9e395f6f9909d86`) | as installed | `languages/python`, on both claimed versions |
| OpenJDK | 21.0.12.1 (`openjdk-21-jdk` `21.0.12.1+1-1~24.04.4`) | the image | as installed | `languages/java`, compiled `--release 17` |
| Apache Maven | 3.9.11 | the image (`/opt/apache-maven-3.9.11`) | as installed | `languages/java`: the jar, the test classes and the dependency classpath |

The Kiota restore prints that 1.36.1 is available; the manifest pins 1.34.1 and regeneration stays
on it until a reviewed upgrade changes `MANIFEST.json`, the tool manifest and every Kiota lock
together.

Node and Dart are added to `PATH` for the commands that need them; the image's own Node 22 stays
where it is:

```sh
export PATH=/opt/yogabase-sdk-toolchains/node-25.9.0/bin:/opt/yogabase-sdk-toolchains/dart-3.13.5/dart-sdk/bin:$PATH
```

## Installing them in a fresh session

The image's proxy settings already carry the CA bundle for curl, npm (`NODE_EXTRA_CA_CERTS`), pip
and the JVM (`JAVA_TOOL_OPTIONS` names a trust store); nothing below disables TLS verification.

```sh
set -euo pipefail
root=/opt/yogabase-sdk-toolchains
mkdir -p "$root/_downloads/node" "$root/_downloads/dart"

# Node 25.9.0, checked against the release's SHASUMS256.txt
cd "$root/_downloads/node"
curl -sSfLO https://nodejs.org/dist/v25.9.0/SHASUMS256.txt
curl -sSfLO https://nodejs.org/dist/v25.9.0/node-v25.9.0-linux-x64.tar.xz
grep ' node-v25.9.0-linux-x64.tar.xz$' SHASUMS256.txt | sha256sum -c -
echo '1d8db7d6e291d167e8c467ae4094be175e1a0b3969c7ae1f8955b9f7824f7b2e  node-v25.9.0-linux-x64.tar.xz' | sha256sum -c -
tar -xJf node-v25.9.0-linux-x64.tar.xz -C "$root" && mv "$root/node-v25.9.0-linux-x64" "$root/node-25.9.0"

# Dart 3.13.5, checked against its published .sha256sum
cd "$root/_downloads/dart"
base=https://storage.googleapis.com/dart-archive/channels/stable/release/3.13.5/sdk
curl -sSfLO "$base/dartsdk-linux-x64-release.zip"
curl -sSfLO "$base/dartsdk-linux-x64-release.zip.sha256sum"
echo "$(cut -d' ' -f1 dartsdk-linux-x64-release.zip.sha256sum)  dartsdk-linux-x64-release.zip" | sha256sum -c -
echo 'ea864bc64df30a6b8bdf30b2e32550f7717d9a890de8f40293aeabb924fe232b  dartsdk-linux-x64-release.zip' | sha256sum -c -
mkdir -p "$root/dart-3.13.5" && unzip -q dartsdk-linux-x64-release.zip -d "$root/dart-3.13.5"

# rsync, which the regeneration script needs
apt-get update -qq && apt-get install -y -qq rsync

# Kiota 1.34.1, from the repository's tool manifest
cd <checkout> && dotnet tool restore
```

If `dotnet restore --locked-mode` refuses on the image's 10.0.402 SDK, install the exact
10.0.400 that `global.json` names beside it and point `DOTNET_EXE` (for the regeneration script)
or `PATH` at it; on 2026-10-20 the locked restore passed on 10.0.402.

## Running the suites

These are the README's "Local verification" commands as they run here. Python environments are
made outside the checkout so the two interpreters do not share one `.venv`.

```sh
# .NET
dotnet restore Yogabase.SDKs.slnx --locked-mode
dotnet build Yogabase.SDKs.slnx -c Release --no-restore
dotnet test Yogabase.SDKs.slnx -c Release --no-restore --no-build

# Regeneration (must leave the tree unchanged)
./scripts/regenerate-clients.sh && git status --short

# TypeScript
cd languages/typescript && npm ci && npm test

# Python, on each claimed interpreter
for v in 3.13 3.12; do
  python$v -m venv /tmp/yogabase-py$v
  /tmp/yogabase-py$v/bin/python -m pip install --require-hashes -r requirements.lock -r build-requirements.lock
  PYTHONPATH=src /tmp/yogabase-py$v/bin/python -X dev -W error::ResourceWarning -m unittest discover -s tests
done

# Java: Maven compiles the jar and the check programs and resolves the classpath; the checks are
# main-method programs, so `mvn test` runs none of them
cd languages/java
mvn -B -q test-compile
mvn -B -q org.apache.maven.plugins:maven-dependency-plugin:3.8.1:build-classpath -Dmdep.outputFile=target/cp.txt
for check in YogabaseStorageLogicalKeyTest YogabaseGeneratedSurfaceTest YogabaseDeliveryClientTest \
             YogabaseJobsClientTest YogabaseSchedulesClientTest YogabaseSignInTest; do
  java -cp "target/classes:target/test-classes:$(cat target/cp.txt)" io.yogabase.client.$check
done

# Dart
cd languages/dart
dart pub get --enforce-lockfile
dart analyze --fatal-infos --fatal-warnings
for check in delivery jobs schedules sign_in storage; do dart run test/yogabase_${check}_source_test.dart; done
```

`JAVA_TOOL_OPTIONS` makes every JVM print a "Picked up" line; it is harmless, and setting the
variable empty for the `java` commands keeps the output clean when no proxy is needed (the checks
run on loopback only).

## What does not run here

- The Unity Editor compile check (`scripts/compile-unity-editor.sh`) needs a response file a real
  Unity Editor wrote; there is no Unity Editor in the container, and the script says so and exits 2.
  The Unity builds of the .NET sources (`src/unity/*`) do build here as part of the solution.
- No live installation is reachable from the container, so each client's live smoke test skips
  without its `YOGABASE_SMOKE_*` variables.
